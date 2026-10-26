using ACadSharp.Classes;
using ACadSharp.IO.DWG;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.IO.DWG;

/// <summary>
/// The DWG classes section, written and read back on its own.
/// </summary>
/// <remarks>
/// Driven at the section rather than through a whole document because the case that matters cannot
/// be built from one: a <see cref="CadDocument"/> always carries a model layout, LAYOUT defines a
/// DXF class, and <c>UpdateDxfClasses</c> rebuilds the table from the document's own objects before
/// every write. An empty class table is reachable only from a pre-R13 container, which has no
/// OBJECTS section at all - which is why the section's empty case stayed latent.
/// </remarks>
public class DwgClassesSectionTests
{
	public static TheoryData<ACadVersion> WritableVersions { get; } = new()
	{
		ACadVersion.AC1015,
		ACadVersion.AC1018,
		ACadVersion.AC1024,
		ACadVersion.AC1027,
		ACadVersion.AC1032,
	};

	/// <summary>
	/// A section holding no classes reads back holding no classes.
	/// </summary>
	/// <remarks>
	/// The regression this file exists for. At R2004+ the writer emits
	/// <c>BS maxClassNumber</c> / <c>RC 0</c> / <c>RC 0</c> / <c>B true</c>, and the reader used to
	/// decode that as <c>BL</c> / <c>B</c> at R2007+ and only match it at exactly AC1018. The two
	/// shapes are both 35 bits when the bit-short takes its 16-bit form, which every class number
	/// from 500 up does - so every file anyone had read decoded correctly. A maximum class number of
	/// <b>0</b>, which only an empty table produces, is the bit-short's two-bit zero code: the reader
	/// consumed 3 bits where the writer wrote 19, read the remaining padding as the start of a class,
	/// and threw <c>OverflowException</c> out of <c>ReadVariableText</c> on the length it found.
	/// </remarks>
	[Theory]
	[MemberData(nameof(WritableVersions))]
	public void EmptySectionReadsBackEmpty(ACadVersion version)
	{
		DxfClassCollection read = roundTrip(version);

		Assert.Empty(read);
	}

	/// <summary>
	/// A section holding classes reads every one of them back, with its number.
	/// </summary>
	/// <remarks>
	/// The control for <see cref="EmptySectionReadsBackEmpty"/>: the numbers are asserted, not just
	/// the count, because a preamble read at the wrong width shifts the whole list rather than
	/// dropping it.
	/// </remarks>
	[Theory]
	[MemberData(nameof(WritableVersions))]
	public void PopulatedSectionReadsBackWithItsClassNumbers(ACadVersion version)
	{
		DxfClassCollection read = roundTrip(version, sampleClasses());

		Assert.Equal(3, read.Count);
		Assert.Equal(
			new[] { (short)500, (short)501, (short)502 },
			read.OrderBy(c => c.ClassNumber).Select(c => c.ClassNumber));
		Assert.Equal(
			new[] { "ACDBPLACEHOLDER", "LAYOUT", "LWPOLYLINE" },
			read.Select(c => c.DxfName).OrderBy(n => n, System.StringComparer.Ordinal));
	}

	/// <summary>
	/// A single class is the boundary the empty case sits next to, and it has its own maximum class
	/// number of 500 - the smallest value that still takes the bit-short's 16-bit form.
	/// </summary>
	[Theory]
	[MemberData(nameof(WritableVersions))]
	public void SingleClassSectionReadsBack(ACadVersion version)
	{
		DxfClassCollection read = roundTrip(version, sampleClasses().Take(1).ToArray());

		DxfClass only = Assert.Single(read);
		Assert.Equal("LWPOLYLINE", only.DxfName);
		Assert.Equal(500, only.ClassNumber);
	}

	private static IList<DxfClass> sampleClasses() => new List<DxfClass>
	{
		new DxfClass
		{
			CppClassName = "AcDbPolyline",
			DxfName = "LWPOLYLINE",
			ClassNumber = 500,
			ItemClassId = 498,
			DwgVersion = ACadVersion.AC1015,
		},
		new DxfClass
		{
			CppClassName = "AcDbLayout",
			DxfName = "LAYOUT",
			ClassNumber = 501,
			ItemClassId = 499,
			DwgVersion = ACadVersion.AC1015,
		},
		new DxfClass
		{
			CppClassName = "AcDbPlaceHolder",
			DxfName = "ACDBPLACEHOLDER",
			ClassNumber = 502,
			ItemClassId = 499,
			DwgVersion = ACadVersion.AC1015,
		},
	};

	private static DxfClassCollection roundTrip(ACadVersion version, IList<DxfClass> classes = null)
	{
		CadDocument source = new CadDocument(version);
		source.Classes.Clear();
		foreach (DxfClass item in classes ?? new List<DxfClass>())
		{
			source.Classes.Add(item);
		}

		MemoryStream stream = new MemoryStream();
		new DwgClassesWriter(stream, source, Encoding.UTF8).Write();

		stream.Position = 0;

		// The section carries an extra raw long at AC1024/AC1027 when the maintenance version is above
		// 3, and the writer reads that off the document while the reader reads it off the file header.
		// A real file header carries the document's value; this one has to be given it, or the two
		// disagree about the section's shape and the reader finds no classes at all.
		DwgFileHeader fileHeader = DwgFileHeader.CreateFileHeader(version);
		fileHeader.AcadMaintenanceVersion = source.Header.MaintenanceVersion;

		DxfClassCollection read = new DxfClassCollection(new CadDocument(version));
		new DwgClassesReader(
			version,
			DwgStreamReaderBase.GetStreamHandler(version, stream, Encoding.UTF8, resetPositon: true),
			fileHeader,
			read)
			.Read();

		return read;
	}
}
