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
/// A SEQEND reaching the DWG writer on its own.
/// </summary>
/// <remarks>
/// A SEQEND is written as part of the POLYLINE or INSERT that owns it, so one arriving in an entity
/// list of its own is a duplicate rather than something to carry. Pre-R13 files are how one gets
/// there: R12 keeps SEQEND in the entity list.
/// </remarks>
public class DwgWriterSeqendTests : IOTestsBase
{
	public DwgWriterSeqendTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>
	/// A SEQEND sitting in the entity list is declined rather than thrown on.
	/// </summary>
	/// <remarks>
	/// It is declined silently, with no notification, because nothing is lost — which is what the
	/// assertion on <c>notifications</c> pins. Before this it fell through to <c>writeEntity</c>'s
	/// throwing default arm and took the whole save with it.
	/// </remarks>
	[Fact]
	public void StandaloneSeqendIsDeclinedWithoutThrowing()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);
		doc.Entities.Add(new Line(XYZ.Zero, XYZ.AxisX));
		doc.Entities.Add(new Seqend());

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
		Assert.Empty(read.Entities.OfType<Seqend>());
		Assert.DoesNotContain(notifications, n => n.Contains(nameof(Seqend)));
	}

	/// <summary>
	/// The SEQEND an owner brings with it is still written, and is still there on the way back.
	/// </summary>
	/// <remarks>
	/// The counter-test the drop-list arm needs. An owner's SEQEND never passes through
	/// <c>isEntitySupported</c> — <c>writeChildEntities</c> emits it directly — so declining the
	/// standalone case must not touch it. Without this, a drop-list arm that reached the owned
	/// SEQEND as well would look identical from the test above.
	/// </remarks>
	[Fact]
	public void AnOwnedSeqendIsStillWritten()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);
		Polyline2D polyline = new Polyline2D();
		polyline.Vertices.Add(new Vertex2D(new XYZ(0, 0, 0)));
		polyline.Vertices.Add(new Vertex2D(new XYZ(1, 0, 0)));
		doc.Entities.Add(polyline);

		Assert.NotNull(polyline.Vertices.Seqend);

		MemoryStream stream = new MemoryStream();
		using (DwgWriter writer = new DwgWriter(stream, doc))
		{
			writer.Write();
		}

		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()));

		Polyline2D readPolyline = Assert.Single(read.Entities.OfType<Polyline2D>());
		Assert.Equal(2, readPolyline.Vertices.Count());
		Assert.NotNull(readPolyline.Vertices.Seqend);
	}
}
