using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tests.TestModels;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO;

public class TransparencyTests : IOTestsBase
{
	/// <summary>
	/// The POINT in the sample drawings whose color, linetype and lineweight are all ByBlock and
	/// whose transparency is ByLayer.
	/// </summary>
	private const ulong ByBlockPointHandle = 0x298;

	public static TheoryData<FileModel> TransparencySamplesFilePaths { get; } = new();

	static TransparencyTests()
	{
		loadSamples("./", "dwg", TransparencySamplesFilePaths);
		loadSamples("./", "dxf", TransparencySamplesFilePaths);
	}

	public TransparencyTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>
	/// The R2004+ DWG entity color field is a bit short of flags plus an ACI index, and flag 0x2000
	/// says a transparency BL follows it. An entity whose color is ByBlock (index 0) and whose
	/// transparency is ByLayer sets no flag and contributes no index, so the whole field is 0 - and
	/// a 0 carries no transparency, exactly like any other field with 0x2000 clear.
	/// </summary>
	/// <remarks>
	/// The same drawing is in the samples folder in both containers, and the DXF copies carry no
	/// group 440 on this entity, which is ByLayer. A reader that gives it any other transparency
	/// from the DWG copy disagrees with AutoCAD about a state AutoCAD wrote twice.
	/// </remarks>
	[Theory]
	[MemberData(nameof(TransparencySamplesFilePaths))]
	public void ByBlockColorKeepsByLayerTransparency(FileModel test)
	{
		CadDocument doc = this.readDocument(test);

		//R13-R15 DWG does not carry transparency in the entity color field at all.
		if (doc.Header.Version < ACadVersion.AC1018)
		{
			return;
		}

		Point point = doc.GetCadObject<Point>(ByBlockPointHandle);

		Assert.NotNull(point);
		Assert.True(point.Color.IsByBlock, "sample precondition: this entity's color is ByBlock");
		Assert.True(point.Transparency.IsByLayer);
	}

	/// <summary>
	/// The same pair written and read back by this library, so the claim does not rest on the
	/// sample files alone. The versions are the ones DwgWriter supports at R2004 and above -
	/// AC1021 is not among them, same as in <see cref="DWG.DwgWriterSingleObjectTests"/>.
	/// </summary>
	[Theory]
	[InlineData(ACadVersion.AC1018)]
	[InlineData(ACadVersion.AC1024)]
	[InlineData(ACadVersion.AC1027)]
	[InlineData(ACadVersion.AC1032)]
	public void ByBlockColorKeepsByLayerTransparencyThroughDwg(ACadVersion version)
	{
		Point readPoint = this.roundTripPoint(version, Color.ByBlock, Transparency.ByLayer);

		Assert.True(readPoint.Color.IsByBlock);
		Assert.True(readPoint.Transparency.IsByLayer);
	}

	/// <summary>
	/// The boundary the ByLayer rule stops at: a ByBlock colour does not by itself imply ByLayer
	/// transparency. An explicit value sets flag 0x2000 and writes a transparency BL, so the field
	/// is not 0 and the entity keeps the value it was given - including 0, which is a genuinely
	/// opaque entity and not the absence of a transparency.
	/// </summary>
	[Theory]
	[InlineData(ACadVersion.AC1018, (short)0)]
	[InlineData(ACadVersion.AC1018, (short)50)]
	[InlineData(ACadVersion.AC1024, (short)0)]
	[InlineData(ACadVersion.AC1024, (short)50)]
	[InlineData(ACadVersion.AC1027, (short)90)]
	[InlineData(ACadVersion.AC1032, (short)0)]
	[InlineData(ACadVersion.AC1032, (short)50)]
	public void ByBlockColorKeepsAnExplicitTransparencyThroughDwg(ACadVersion version, short value)
	{
		Point readPoint = this.roundTripPoint(version, Color.ByBlock, new Transparency(value));

		Assert.True(readPoint.Color.IsByBlock);
		Assert.False(readPoint.Transparency.IsByLayer);
		Assert.False(readPoint.Transparency.IsByBlock);
		Assert.Equal(value, readPoint.Transparency.Value);
	}

	/// <summary>
	/// ByBlock transparency is its own state as well: it sets 0x2000 and writes the BL with type
	/// byte 1, so it is neither the 0 field nor an explicit percentage.
	/// </summary>
	[Theory]
	[InlineData(ACadVersion.AC1018)]
	[InlineData(ACadVersion.AC1024)]
	[InlineData(ACadVersion.AC1027)]
	[InlineData(ACadVersion.AC1032)]
	public void ByBlockColorKeepsByBlockTransparencyThroughDwg(ACadVersion version)
	{
		Point readPoint = this.roundTripPoint(version, Color.ByBlock, Transparency.ByBlock);

		Assert.True(readPoint.Color.IsByBlock);
		Assert.True(readPoint.Transparency.IsByBlock);
	}

	private Point roundTripPoint(ACadVersion version, Color color, Transparency transparency)
	{
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		Point point = new Point
		{
			Color = color,
			Transparency = transparency,
		};
		doc.Entities.Add(point);

		ulong handle = point.Handle;

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: this.onNotification);

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);

		Point readPoint = read.GetCadObject<Point>(handle);
		Assert.NotNull(readPoint);

		return readPoint;
	}
}
