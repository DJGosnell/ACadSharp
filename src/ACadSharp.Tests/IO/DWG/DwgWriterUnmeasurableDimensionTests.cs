using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DWG;

/// <summary>
/// An angular dimension whose measurement cannot be computed, written to each container.
/// </summary>
/// <remarks>
/// A DWG stores the actual measurement as a field of its own (group 42), so a dimension that cannot
/// supply one cannot be written to that container. DXF stores no such field and is unaffected —
/// that asymmetry is the whole of the rule, and both halves are asserted here.
/// </remarks>
public class DwgWriterUnmeasurableDimensionTests : IOTestsBase
{
	public DwgWriterUnmeasurableDimensionTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>
	/// An angular dimension whose defining vectors are zero is declined by the DWG writer, and the
	/// save completes.
	/// </summary>
	/// <remarks>
	/// <see cref="DimensionAngular2Line.Measurement"/> throws rather than answering when the vectors
	/// it measures between are zero — which is how every pre-R13 angular dimension loads. Before
	/// this, that one entity took the whole save with it.
	/// </remarks>
	[Fact]
	public void UnmeasurableAngularDimensionIsDeclinedAndReported()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);
		doc.Entities.Add(new Line(XYZ.Zero, XYZ.AxisX));
		doc.Entities.Add(new DimensionAngular2Line());

		Assert.False(doc.Entities.OfType<DimensionAngular2Line>().Single().IsMeasurable);

		List<string> notifications = new();
		// The writer closes the stream it was given when it is disposed, so the bytes are taken
		// out of the MemoryStream rather than read through it.
		MemoryStream stream = new MemoryStream();
		using (DwgWriter writer = new DwgWriter(stream, doc))
		{
			writer.OnNotification += (s, e) => notifications.Add($"{e.NotificationType}: {e.Message}");
			writer.Write();
		}

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()));

		Assert.Single(read.Entities.OfType<Line>());
		Assert.Empty(read.Entities.OfType<DimensionAngular2Line>());
		Assert.Contains(notifications, n => n.Contains(nameof(DimensionAngular2Line)));
	}

	/// <summary>
	/// An angular dimension that <em>can</em> be measured is written as it always was.
	/// </summary>
	/// <remarks>
	/// The counter-test the validity rule needs. <see cref="DimensionAngular2Line.IsMeasurable"/> is
	/// the predicate the rule turns on, so a rule that answered false for every angular dimension
	/// would satisfy the test above and lose every one of them.
	/// </remarks>
	[Fact]
	public void AMeasurableAngularDimensionIsStillWrittenToDwg()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);
		DimensionAngular2Line dimension = new DimensionAngular2Line
		{
			FirstPoint = XYZ.Zero,
			SecondPoint = XYZ.AxisX,
			AngleVertex = XYZ.Zero,
			DefinitionPoint = XYZ.AxisY,
		};
		doc.Entities.Add(dimension);

		Assert.True(dimension.IsMeasurable);

		MemoryStream stream = new MemoryStream();
		using (DwgWriter writer = new DwgWriter(stream, doc))
		{
			writer.Write();
		}

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()));

		Assert.Single(read.Entities.OfType<DimensionAngular2Line>());
	}

	/// <summary>
	/// The unmeasurable dimension is still written to DXF, which carries no measurement field.
	/// </summary>
	/// <remarks>
	/// The validity rule is scoped to <see cref="CadFileFormat.DWG"/> on purpose: group 42 is
	/// read-only in DXF and no DXF writer asks for <c>Measurement</c>, so a pre-R13 drawing saved
	/// back as DXF must keep every dimension it arrived with.
	/// </remarks>
	[Fact]
	public void UnmeasurableAngularDimensionIsStillWrittenToDxf()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1032);
		doc.Entities.Add(new DimensionAngular2Line());

		MemoryStream stream = new MemoryStream();
		using (DxfWriter writer = new DxfWriter(stream, doc, false))
		{
			writer.Write();
		}

		CadDocument read = DxfReader.Read(new MemoryStream(stream.ToArray()));

		Assert.Single(read.Entities.OfType<DimensionAngular2Line>());
	}
}
