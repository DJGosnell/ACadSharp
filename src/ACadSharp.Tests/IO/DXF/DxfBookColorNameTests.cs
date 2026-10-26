using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DXF;

/// <summary>
/// A DBCOLOR written to DXF comes back under the name it was written with.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BookColor.Name"/> already returns <c>BookName$ColorName</c>, which is the form
/// AutoCAD writes in group code 430 and the form an entity's own 430 carries. The objects-section
/// writer appended the book name to it a second time, producing
/// <c>BookName$ColorName$BookName</c>; the <c>Name</c> setter splits on '$' and keeps the first and
/// last tokens, so the swatch was rebuilt as <c>BookName$BookName</c>.
/// </para>
/// <para>
/// Nothing threw. The entity's 430 still said <c>BookName$ColorName</c>, the color book entry was
/// registered under a different string, and <c>CadEntityTemplate</c> resolves the reference by
/// looking that string up - so every color book reference in a DXF this library wrote came back
/// null, and the entity lost its swatch without a word.
/// </para>
/// </remarks>
public class DxfBookColorNameTests : IOTestsBase
{
	public DxfBookColorNameTests(ITestOutputHelper output) : base(output)
	{
	}

	[Fact]
	public void ADbColorKeepsItsNameThroughADxfRoundTrip()
	{
		CadDocument read = this.roundTrip();

		Assert.Equal("RAL CLASSIC$RAL 1006", read.Colors.Single().Name);
	}

	/// <summary>
	/// And the entity finds it again, which is the only reason the name matters.
	/// </summary>
	/// <remarks>
	/// Asserted separately because the two can come apart: a lookup keyed on something other than
	/// the name would satisfy this while the name was still mangled, and a name that round-trips
	/// while the entity's own 430 is written differently would satisfy the case above.
	/// </remarks>
	[Fact]
	public void AnEntityResolvesItsColorBookReferenceAfterADxfRoundTrip()
	{
		CadDocument read = this.roundTrip();
		Circle circle = read.Entities.OfType<Circle>().Single();

		Assert.NotNull(circle.BookColor);
		Assert.Equal("RAL CLASSIC$RAL 1006", circle.BookColor.Name);
		Assert.Equal("RAL CLASSIC", circle.BookColor.BookName);
		Assert.Equal("RAL 1006", circle.BookColor.ColorName);
	}

	private CadDocument roundTrip()
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);

		BookColor book = new BookColor("RAL CLASSIC$RAL 1006") { Color = new Color(226, 144, 0) };
		doc.Colors.Add(book);

		Circle circle = new Circle { Center = new XYZ(10, 20, 0), Radius = 3, BookColor = book };
		doc.Entities.Add(circle);

		MemoryStream stream = new MemoryStream();
		DxfWriter.Write(stream, doc, false, notification: this.onNotification);

		return DxfReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);
	}
}
