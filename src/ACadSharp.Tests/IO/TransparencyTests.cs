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
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		Point point = new Point
		{
			Color = Color.ByBlock,
			Transparency = Transparency.ByLayer,
		};
		doc.Entities.Add(point);

		ulong handle = point.Handle;

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: this.onNotification);

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);

		Point readPoint = read.GetCadObject<Point>(handle);

		Assert.NotNull(readPoint);
		Assert.True(readPoint.Color.IsByBlock);
		Assert.True(readPoint.Transparency.IsByLayer);
	}
}
