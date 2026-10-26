using ACadSharp.Entities;
using ACadSharp.Exceptions;
using ACadSharp.Header;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tests.Common;
using CSMath;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DWG;

public class DwgWriterTests : IOTestsBase
{
	public DwgWriterTests(ITestOutputHelper output) : base(output)
	{
	}

	[Theory]
	[MemberData(nameof(Versions))]
	public void WriteEmptyTest(ACadVersion version)
	{
		string path = Path.Combine(TestVariables.OutputSamplesFolder, $"out_empty_sample_{version}.dwg");
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		using (var wr = new DwgWriter(path, doc))
		{
			if (this.isSupportedVersion(version))
			{
				wr.Write();
			}
			else
			{
				Assert.Throws<CadNotSupportedException>(() => wr.Write());
				return;
			}
		}

		using (var re = new DwgReader(path, this.onNotification))
		{
			CadDocument readed = re.Read();
		}
	}

	[Theory]
	[MemberData(nameof(Versions))]
	public void WriteHeaderTest(ACadVersion version)
	{
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		MemoryStream stream = new MemoryStream();

		using (var wr = new DwgWriter(stream, doc))
		{
			if (this.isSupportedVersion(version))
			{
				wr.Write();
			}
			else
			{
				Assert.Throws<CadNotSupportedException>(() => wr.Write());
				return;
			}
		}

		stream = new MemoryStream(stream.ToArray());

		using (var re = new DwgReader(stream, this.onNotification))
		{
			CadHeader header = re.ReadHeader();
		}
	}

	[Theory]
	[MemberData(nameof(Versions))]
	public void WritePreview(ACadVersion version)
	{
		if (!TestVariables.SavePreview)
		{
			return;
		}

		string image = Path.Combine(TestVariables.SamplesFolder, $"preview.png");
		string path = Path.Combine(TestVariables.OutputSamplesFolder, $"prview_{version}.dwg");
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		using (var wr = new DwgWriter(path, doc))
		{
			if (this.isSupportedVersion(version))
			{
				wr.Preview = new DwgPreview(DwgPreview.PreviewType.Png, new byte[80], File.ReadAllBytes(image));
				wr.Write();
			}
			else
			{
				Assert.Throws<CadNotSupportedException>(() => wr.Write());
				return;
			}
		}

		using (var re = new DwgReader(path, this.onNotification))
		{
			CadDocument readed = re.Read();
		}
	}

	[Theory]
	[MemberData(nameof(Versions))]
	public void WriteSummaryTest(ACadVersion version)
	{
		if (version <= ACadVersion.AC1015)
			return;

		CadDocument doc = new CadDocument();
		doc.Header.Version = version;
		doc.SummaryInfo = new CadSummaryInfo
		{
			Title = "This is a random title",
			Subject = "This is a subject",
			Author = "ACadSharp",
			Keywords = "My Keyworks",
			Comments = "This is my comment"
		};

		MemoryStream stream = new MemoryStream();

		using (var wr = new DwgWriter(stream, doc))
		{
			if (this.isSupportedVersion(version))
			{
				wr.Write();
			}
			else
			{
				Assert.Throws<CadNotSupportedException>(() => wr.Write());
				return;
			}
		}

		stream = new MemoryStream(stream.ToArray());

		using (var re = new DwgReader(stream, this.onNotification))
		{
			CadSummaryInfo info = re.ReadSummaryInfo();

			Assert.Equal(doc.SummaryInfo.Title, info.Title);
			Assert.Equal(doc.SummaryInfo.Subject, info.Subject);
			Assert.Equal(doc.SummaryInfo.Author, info.Author);
			Assert.Equal(doc.SummaryInfo.Keywords, info.Keywords);
			Assert.Equal(doc.SummaryInfo.Comments, info.Comments);
		}
	}

	[Theory]
	[MemberData(nameof(Versions))]
	public void WriteTest(ACadVersion version)
	{
		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		this.addEntities(doc);

		string path = Path.Combine(TestVariables.OutputSamplesFolder, $"out_sample_{version}.dwg");

		using (var wr = new DwgWriter(path, doc))
		{
			wr.OnNotification += this.onNotification;
			if (this.isSupportedVersion(version))
			{
				wr.Write();
			}
			else
			{
				Assert.Throws<CadNotSupportedException>(() => wr.Write());
				return;
			}
		}

		using (var re = new DwgReader(path, this.onNotification))
		{
			CadDocument readed = re.Read();
		}
	}

	/// <summary>
	/// An entity can carry a book color and a true color at the same time: a dxf read takes the
	/// true color from group code 420 and the book color from 430. The book color's rgb lives on
	/// the AcDbColor object the entity points at, so the entity color must not also write it
	/// inline - <c>ReadEnColor</c> does not consume it, and the displaced bits make the whole
	/// entity unreadable.
	/// </summary>
	[Theory]
	[MemberData(nameof(Versions))]
	public void WriteEntityWithBookColorAndTrueColor(ACadVersion version)
	{
		//Book colors are an R2004+ feature.
		if (!this.isSupportedVersion(version) || version < ACadVersion.AC1018)
		{
			return;
		}

		CadDocument doc = new CadDocument();
		doc.Header.Version = version;

		BookColor book = new BookColor("TEST BOOK$MY COLOR");
		book.Color = new Color(226, 156, 0);
		doc.Colors.Add(book);

		Circle circle = new Circle();
		circle.Center = new XYZ(10, 20, 0);
		circle.Radius = 3;
		circle.Color = Color.FromTrueColor(14848000);
		circle.BookColor = book;
		doc.Entities.Add(circle);

		ulong handle = circle.Handle;

		MemoryStream stream = new MemoryStream();

		using (var wr = new DwgWriter(stream, doc))
		{
			wr.OnNotification += this.onNotification;
			wr.Write();
		}

		stream = new MemoryStream(stream.ToArray());

		using (var re = new DwgReader(stream, this.onNotification))
		{
			CadDocument readed = re.Read();

			Circle result = readed.GetCadObject<Circle>(handle);
			Assert.NotNull(result);
			Assert.Equal(3, result.Radius);
			Assert.NotNull(result.BookColor);
		}
	}

	private void addEntities(CadDocument doc)
	{
		doc.Entities.Add(EntityFactory.Create<Point>());
		doc.Entities.Add(EntityFactory.Create<Line>());
	}
}