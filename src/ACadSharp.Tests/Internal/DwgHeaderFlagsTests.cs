using ACadSharp.Header;
using ACadSharp.IO.DWG;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.Internal;

/// <summary>
/// The R2000+ header section packs eight variables into one BL "Flags" value. These pin each field
/// that is not CELWEIGHT through a real write and read.
/// </summary>
/// <remarks>
/// Two of them are stored inverted - the field list writes LWDISPLAY and XEDIT with a leading "!" -
/// and three of them sit above bit 0, so a decode that forgets either detail still produces a value
/// and never throws. The values below are chosen so that a forgotten shift is visible: EndCaps 2
/// occupies 0x40 and JoinStyle 3 occupies 0x180, so reading the mask without shifting it down
/// answers 64 and 384 rather than 2 and 3, and PlotStyleMode 1 answers 8192 rather than 1.
/// </remarks>
public class DwgHeaderFlagsTests : DwgSectionWriterTestBase
{
	/// <summary>
	/// The Flags value exists only from R2000 onwards; before that the reader never reaches it.
	/// </summary>
	public static TheoryData<ACadVersion> R2000PlusVersions { get; } = new TheoryData<ACadVersion>
	{
		ACadVersion.AC1015,
		ACadVersion.AC1018,
		ACadVersion.AC1021,
		ACadVersion.AC1024,
		ACadVersion.AC1027,
		ACadVersion.AC1032,
	};

	public DwgHeaderFlagsTests(ITestOutputHelper output) : base(output)
	{
	}

	private static CadHeader roundTrip(ACadVersion version, System.Action<CadHeader> set)
	{
		CadDocument document = new CadDocument();
		document.Header.Version = version;
		set(document.Header);

		Stream stream = new MemoryStream();
		DwgHeaderWriter writer = new DwgHeaderWriter(stream, document, Encoding.Default);
		writer.Write();

		IDwgStreamReader sreader = DwgStreamReaderBase.GetStreamHandler(version, stream, resetPositon: true);
		CadHeader read = new CadHeader { Version = version };
		DwgHeaderReader reader = new DwgHeaderReader(version, sreader, read);
		reader.Read(read.MaintenanceVersion, out _);

		return read;
	}

	[Theory]
	[MemberData(nameof(R2000PlusVersions))]
	public void FlagsSetOnSurviveTheRoundTrip(ACadVersion version)
	{
		CadHeader read = roundTrip(version, h =>
		{
			h.DisplayLineWeight = true;
			h.XEdit = true;
			h.ExtendedNames = true;
			h.LoadOLEObject = true;
			h.EndCaps = 2;
			h.JoinStyle = 3;
			h.PlotStyleMode = 1;
		});

		Assert.True(read.DisplayLineWeight);
		Assert.True(read.XEdit);
		Assert.True(read.ExtendedNames);
		Assert.True(read.LoadOLEObject);
		Assert.Equal((short)2, read.EndCaps);
		Assert.Equal((short)3, read.JoinStyle);
		Assert.Equal((short)1, read.PlotStyleMode);
	}

	[Theory]
	[MemberData(nameof(R2000PlusVersions))]
	public void FlagsSetOffSurviveTheRoundTrip(ACadVersion version)
	{
		CadHeader read = roundTrip(version, h =>
		{
			h.DisplayLineWeight = false;
			h.XEdit = false;
			h.ExtendedNames = false;
			h.LoadOLEObject = false;
			h.EndCaps = 0;
			h.JoinStyle = 0;
			h.PlotStyleMode = 0;
		});

		Assert.False(read.DisplayLineWeight);
		Assert.False(read.XEdit);
		Assert.False(read.ExtendedNames);
		Assert.False(read.LoadOLEObject);
		Assert.Equal((short)0, read.EndCaps);
		Assert.Equal((short)0, read.JoinStyle);
		Assert.Equal((short)0, read.PlotStyleMode);
	}

	/// <summary>
	/// EndCaps and JoinStyle each have four values, and only the two middle ones separate a correct
	/// shift from a mask that happens to agree at the ends.
	/// </summary>
	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	[InlineData(3, 3)]
	public void EveryEndCapsAndJoinStyleValueSurvivesTheRoundTrip(short endCaps, short joinStyle)
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h =>
		{
			h.EndCaps = endCaps;
			h.JoinStyle = joinStyle;
		});

		Assert.Equal(endCaps, read.EndCaps);
		Assert.Equal(joinStyle, read.JoinStyle);
	}

	/// <summary>
	/// The one combination that tells an inverted field from a non-inverted one: LWDISPLAY and
	/// EXTNAMES sit two bits apart and are stored with opposite polarity, so a decode that treats
	/// them alike gets exactly one of these two assertions wrong whichever way it guesses.
	/// </summary>
	[Fact]
	public void AnInvertedFlagAndAPlainFlagAreNotDecodedAlike()
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h =>
		{
			h.DisplayLineWeight = true;
			h.ExtendedNames = false;
		});

		Assert.True(read.DisplayLineWeight);
		Assert.False(read.ExtendedNames);
	}
}
