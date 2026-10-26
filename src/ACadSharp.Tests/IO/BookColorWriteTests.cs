using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO;

/// <summary>
/// A writer never records a color the swatch did not name.
/// </summary>
/// <remarks>
/// <para>
/// A DBCOLOR is only obliged to carry its name. One written with group code 430 and neither 62 nor
/// 420 leaves <see cref="BookColor.Color"/> at its default, which is index 0 - ByBlock - and
/// <c>Color.GetRgb()</c> answers that from the index table's dummy row {0,0,0} rather than throwing.
/// Both writers used to copy those three zeros into a true-color field, so the saved drawing
/// positively named black and a reader had nothing left to tell it from a book color someone chose
/// to be black.
/// </para>
/// <para>
/// The two containers are asserted differently because they can do different things. DXF writes the
/// swatch back the way it arrived - 430 and no 62 or 420 - and loses nothing. DWG has no such
/// encoding: the flag byte <c>writeBookColor</c> emits is fixed at 0b11000010, the true-color flag,
/// so it drops the reference and reports it rather than substituting a color.
/// </para>
/// </remarks>
public class BookColorWriteTests : IOTestsBase
{
	public BookColorWriteTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>The entity's own color, chosen to be nothing like the dummy row's black.</summary>
	private static readonly Color OwnColor = new Color(226, 144, 0);

	public static TheoryData<short> SentinelIndices { get; } = new TheoryData<short> { 0, 256, 257 };

	public static TheoryData<short> NamedIndices { get; } = new TheoryData<short> { 1, 42, 255 };

	[Theory]
	[MemberData(nameof(SentinelIndices))]
	public void ASentinelNamesNoColor(short index)
	{
		BookColor color = new BookColor("RAL CLASSIC$RAL 1006") { Color = new Color(index) };

		Assert.False(color.NamesAColor);
	}

	[Theory]
	[MemberData(nameof(NamedIndices))]
	public void AnAciIndexNamesAColor(short index)
	{
		BookColor color = new BookColor("RAL CLASSIC$RAL 1006") { Color = new Color(index) };

		Assert.True(color.NamesAColor);
	}

	[Fact]
	public void ATrueColorNamesAColor()
	{
		BookColor color = new BookColor("RAL CLASSIC$RAL 1006") { Color = OwnColor };

		Assert.True(color.NamesAColor);
	}

	/// <summary>
	/// The default a DBCOLOR carrying only group code 430 leaves behind.
	/// </summary>
	/// <remarks>
	/// Stated as its own case because it is the one that actually occurs: the sentinel is not
	/// something a caller sets, it is what the reader leaves when the file said nothing.
	/// </remarks>
	[Fact]
	public void ASwatchLeftAtItsDefaultNamesNoColor()
	{
		Assert.False(new BookColor("RAL CLASSIC$RAL 1006").NamesAColor);
	}

	[Fact]
	public void ADwgWrite_DropsAReferenceToASwatchThatNamesNoColor()
	{
		List<string> messages = new List<string>();

		CadDocument read = this.dwgRoundTrip(this.drawing(namesAColor: false), messages, out ulong handle);
		Circle circle = read.GetCadObject<Circle>(handle);

		Assert.NotNull(circle);
		Assert.Null(circle.BookColor);
		Assert.Contains(messages, m => m.Contains("names no color"));
	}

	/// <summary>
	/// The entity keeps the color it had, which is what it resolved to anyway.
	/// </summary>
	/// <remarks>
	/// Separate from the reference claim because a writer that dropped the reference and then also
	/// wrote the entity as ByBlock would satisfy the one above while still losing the drawing's
	/// color.
	/// </remarks>
	[Fact]
	public void ADwgWrite_LeavesTheEntityItsOwnColor()
	{
		CadDocument read = this.dwgRoundTrip(this.drawing(namesAColor: false), null, out ulong handle);
		Circle circle = read.GetCadObject<Circle>(handle);

		Assert.True(circle.Color.IsTrueColor);
		Assert.Equal(OwnColor.R, circle.Color.R);
		Assert.Equal(OwnColor.G, circle.Color.G);
		Assert.Equal(OwnColor.B, circle.Color.B);
	}

	/// <summary>
	/// The control: a swatch that names a color is still written and still referenced.
	/// </summary>
	/// <remarks>
	/// Without it, a writer that simply stopped emitting book colors altogether would pass every
	/// other case in this class.
	/// </remarks>
	[Fact]
	public void ADwgWrite_KeepsAReferenceToASwatchThatNamesAColor()
	{
		List<string> messages = new List<string>();

		CadDocument read = this.dwgRoundTrip(this.drawing(namesAColor: true), messages, out ulong handle);
		Circle circle = read.GetCadObject<Circle>(handle);

		Assert.NotNull(circle.BookColor);
		Assert.True(circle.BookColor.NamesAColor);
		Assert.DoesNotContain(messages, m => m.Contains("names no color"));
	}

	/// <summary>
	/// The swatch object itself is not written to a DWG, because there is no way to write it
	/// truthfully.
	/// </summary>
	/// <remarks>
	/// Dropping the entity's reference is only half of it. writeBookColor builds its true-color
	/// BitLong from the swatch's R, G and B, which for a sentinel are the index table's dummy row,
	/// and readDbColor rebuilds a true color from that BitLong without ever inspecting the flag
	/// byte - so a written swatch comes back with NamesAColor true, black, and no reader able to
	/// tell it from a book color someone chose to be black. That is the confusion this whole class
	/// exists to prevent, displaced from the entity onto the swatch.
	/// </remarks>
	[Fact]
	public void ADwgWrite_DoesNotWriteASwatchThatNamesNoColor()
	{
		CadDocument read = this.dwgRoundTrip(this.drawing(namesAColor: false), null, out _);

		Assert.Empty(read.Colors);
	}

	/// <summary>
	/// The control: a swatch that names a color is still written, and still names it.
	/// </summary>
	[Fact]
	public void ADwgWrite_WritesASwatchThatNamesAColor()
	{
		CadDocument read = this.dwgRoundTrip(this.drawing(namesAColor: true), null, out _);

		BookColor swatch = Assert.Single(read.Colors);
		Assert.True(swatch.NamesAColor);
	}

	/// <summary>
	/// DXF says what the file said: 430 and no 62 or 420, so the swatch comes back naming nothing.
	/// </summary>
	/// <remarks>
	/// Asked of the document's color table rather than of the entity's reference on purpose. Whether
	/// that reference resolves is a separate defect on a separate branch
	/// (<c>fix/dxf-dbcolor-name-written-twice</c>: the objects writer appends the book name to a
	/// Name that already contains it, so the swatch is registered under a name no entity's own 430
	/// matches). Asserting through the reference here would make this branch red on its own test
	/// file for a reason it does not fix.
	/// </remarks>
	[Fact]
	public void ADxfWrite_KeepsTheSwatchSayingItNamesNoColor()
	{
		CadDocument read = this.dxfRoundTrip(this.drawing(namesAColor: false), out _);

		BookColor swatch = Assert.Single(read.Colors);
		Assert.False(swatch.NamesAColor);
	}

	/// <summary>
	/// And the entity keeps its own color rather than the swatch's non-color.
	/// </summary>
	/// <remarks>
	/// A book color normally replaces the entity's own 62 and 420, which is correct - the swatch is
	/// the entity's color. It stops being correct when the swatch names nothing, because the value
	/// written would then be the dummy row.
	/// </remarks>
	[Fact]
	public void ADxfWrite_LeavesTheEntityItsOwnColor()
	{
		CadDocument read = this.dxfRoundTrip(this.drawing(namesAColor: false), out ulong handle);
		Circle circle = read.GetCadObject<Circle>(handle);

		Assert.True(circle.Color.IsTrueColor);
		Assert.Equal(OwnColor.R, circle.Color.R);
		Assert.Equal(OwnColor.G, circle.Color.G);
		Assert.Equal(OwnColor.B, circle.Color.B);
	}

	/// <summary>
	/// The control on the same terms: a swatch that names a color still comes back naming it.
	/// </summary>
	[Fact]
	public void ADxfWrite_KeepsASwatchThatNamesAColor()
	{
		CadDocument read = this.dxfRoundTrip(this.drawing(namesAColor: true), out _);

		BookColor swatch = Assert.Single(read.Colors);
		Assert.True(swatch.NamesAColor);
	}

	/// <summary>
	/// One swatch shared by many entities is one loss, and is reported once.
	/// </summary>
	/// <remarks>
	/// The count is the claim, so it is asserted rather than the mere presence of a message: a
	/// writer that reported per entity passes an Assert.Contains and fills a caller's drop list with
	/// N copies of one fact.
	/// </remarks>
	[Fact]
	public void ADwgWrite_ReportsAColorlessSwatchOncePerSwatchNotOncePerEntity()
	{
		List<string> messages = new List<string>();
		CadDocument doc = this.drawing(namesAColor: false);

		BookColor shared = doc.Colors.First();
		for (int i = 0; i < 4; i++)
		{
			Circle extra = new Circle { Center = new XYZ(i, i, 0), Radius = 1, BookColor = shared };
			doc.Entities.Add(extra);
		}

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: (sender, args) => messages.Add(args.Message));

		Assert.Single(messages, m => m.Contains("names no color"));
	}

	/// <summary>
	/// Before R2004 nothing is refused and nothing is reported, because nothing is wrong there.
	/// </summary>
	/// <remarks>
	/// The whole argument for refusing the swatch is that writeBookColor would name it black - and
	/// writeBookColor emits no color field at all below R2004, because the entire RGB block sits
	/// inside the same version check. So there is nothing to refuse, and the swatch is still
	/// written. WriteEnColor likewise ignores its isBookColor argument below R2004 and the hard
	/// pointer is only emitted from AC1018, so dropping the reference or keeping it produces
	/// identical bytes and there is nothing to announce either.
	/// <para>
	/// Asserted rather than assumed because a guard written one version too wide fails exactly
	/// here, silently, and in the direction of deleting user data.
	/// </para>
	/// </remarks>
	[Fact]
	public void ADwgWrite_BeforeR2004_StillWritesTheSwatchAndReportsNothing()
	{
		List<string> messages = new List<string>();
		CadDocument doc = this.drawing(namesAColor: false);
		doc.Header.Version = ACadVersion.AC1015;

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: (sender, args) => messages.Add(args.Message));
		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);

		Assert.Single(read.Colors);
		Assert.DoesNotContain(messages, m => m.Contains("names no color"));
	}

	/// <summary>
	/// A colorless swatch no entity references is still reported, not removed in silence.
	/// </summary>
	/// <remarks>
	/// The refusal lives on the object pass and the reference drop on the entity pass. A swatch
	/// nobody points at never reaches the second, so if only that one reported, the drawing would
	/// come back a color book short with nothing said - the silent substitution this whole change
	/// exists to replace, in its purest form.
	/// </remarks>
	[Fact]
	public void ADwgWrite_ReportsAColorlessSwatchNoEntityReferences()
	{
		List<string> messages = new List<string>();
		CadDocument doc = this.drawing(namesAColor: false);
		foreach (Entity entity in doc.Entities)
		{
			entity.BookColor = null;
		}

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: (sender, args) => messages.Add(args.Message));
		CadDocument read = DwgReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);

		Assert.Empty(read.Colors);
		Assert.Single(messages, m => m.Contains("names no color"));
	}

	private CadDocument drawing(bool namesAColor)
	{
		CadDocument doc = new CadDocument(ACadVersion.AC1024);

		Layer layer = new Layer("book-color-layer") { Color = new Color(0, 0, 255) };
		doc.Layers.Add(layer);

		Circle circle = new Circle
		{
			Center = new XYZ(10, 20, 0),
			Radius = 3,
			Layer = layer,
			Color = OwnColor,
		};

		BookColor book = new BookColor("RAL CLASSIC$RAL 1006");
		if (namesAColor)
		{
			book.Color = new Color(0, 128, 64);
		}

		doc.Colors.Add(book);
		circle.BookColor = book;

		doc.Entities.Add(circle);
		return doc;
	}

	private CadDocument dwgRoundTrip(CadDocument doc, List<string> messages, out ulong handle)
	{
		handle = onlyEntityHandle(doc);

		MemoryStream stream = new MemoryStream();
		DwgWriter.Write(stream, doc, notification: (sender, args) =>
		{
			if (messages != null)
			{
				messages.Add(args.Message);
			}
		});

		return DwgReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);
	}

	private CadDocument dxfRoundTrip(CadDocument doc, out ulong handle)
	{
		handle = onlyEntityHandle(doc);

		MemoryStream stream = new MemoryStream();
		DxfWriter.Write(stream, doc, false, notification: this.onNotification);

		return DxfReader.Read(new MemoryStream(stream.ToArray()), this.onNotification);
	}

	private static ulong onlyEntityHandle(CadDocument doc)
	{
		ulong handle = 0;
		foreach (Entity entity in doc.Entities)
		{
			Assert.Equal(0ul, handle);
			handle = entity.Handle;
		}

		Assert.NotEqual(0ul, handle);
		return handle;
	}
}
