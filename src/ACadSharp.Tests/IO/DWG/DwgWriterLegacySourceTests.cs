using ACadSharp.Entities;
using ACadSharp.IO;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DWG;

/// <summary>
/// The end-to-end case: a document that arrived in a pre-R13 container, written as a DWG and read
/// back.
/// </summary>
/// <remarks>
/// This needs all three of the fixes it sits on top of at once — the classes section's empty case
/// (<see cref="DwgClassesSectionTests"/>), the SEQEND drop-list arm
/// (<see cref="DwgWriterSeqendTests"/>) and the unmeasurable-dimension validity rule
/// (<see cref="DwgWriterUnmeasurableDimensionTests"/>) — so unlike those three it belongs to no
/// single fix, and is deliberately not carried by any of their upstream branches.
/// </remarks>
public class DwgWriterLegacySourceTests : IOTestsBase
{
	public DwgWriterLegacySourceTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>
	/// The R12 fixture, saved as an AC1024 DWG and read back.
	/// </summary>
	/// <remarks>
	/// The two <c>CreateDefaults</c> calls are setup, not part of what is under test: an R12 file has
	/// no OBJECTS section, so its MLINESTYLE dictionary reaches the writer without its
	/// <c>Standard</c> entry and its LTYPE table has no <c>ByBlock</c>, and the DWG header
	/// dereferences both unconditionally. Consumers of this library make those two calls themselves.
	/// </remarks>
	[Theory]
	[InlineData("sample_AC1009_ascii.dxf")]
	[InlineData("sample_AC1009_binary.dxf")]
	public void LegacySourceWritesADwgThatReadsBack(string fixture)
	{
		string source = Path.Combine(TestVariables.SamplesFolder, fixture);
		if (!File.Exists(source))
		{
			return;
		}

		CadDocument doc = DxfReader.Read(source);
		Assert.Equal(ACadVersion.AC1009, doc.Header.Version);
		Assert.Empty(doc.Classes);

		doc.MLineStyles.CreateDefaults();
		doc.LineTypes.CreateDefaultEntries();
		doc.Header.Version = ACadVersion.AC1024;

		int expected = doc.ModelSpace.Entities.Count
			- doc.ModelSpace.Entities.OfType<Seqend>().Count()
			- doc.ModelSpace.Entities.OfType<DimensionAngular2Line>().Count(d => !d.IsMeasurable);

		MemoryStream stream = new MemoryStream();
		using (DwgWriter writer = new DwgWriter(stream, doc))
		{
			// SHAPE is off by default and the fixture carries one. Turned on so the only entities this
			// save drops are the two the count above accounts for.
			writer.Configuration.WriteShapes = true;
			writer.Write();
		}

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()));

		Assert.Equal(ACadVersion.AC1024, read.Header.Version);
		Assert.Empty(read.Classes);
		Assert.Equal(expected, read.ModelSpace.Entities.Count);
	}
}
