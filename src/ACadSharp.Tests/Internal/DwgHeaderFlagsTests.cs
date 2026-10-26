using ACadSharp.Header;
using ACadSharp.IO.DWG;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.Internal;

/// <summary>
/// The R2000+ header section packs eight variables into one BL "Flags" value. These pin each of
/// them through a real write and read.
/// </summary>
/// <remarks>
/// Two are stored inverted - the field list writes LWDISPLAY and XEDIT with a leading "!" - three
/// sit above bit 0, and CELWEIGHT is a 5-bit index into the lineweight table rather than the
/// LineWeightType value itself. A decode that forgets any of those still produces a value and never
/// throws. The values below are chosen so that a forgotten shift is visible: EndCaps 2 occupies
/// 0x40 and JoinStyle 3 occupies 0x180, so reading the mask without shifting it down answers 64 and
/// 384 rather than 2 and 3, and PlotStyleMode 1 answers 8192 rather than 1.
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
	[InlineData(1, 2)]
	[InlineData(3, 1)]
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

	/// <summary>
	/// CELWEIGHT occupies the low five bits, and it holds a lineweight <em>index</em>.
	/// </summary>
	/// <remarks>
	/// LineWeightType's members are hundredths of a millimetre - W50 is 50 - so the value itself
	/// does not fit in five bits, and masking it produces a different, legal weight: 50 becomes 18,
	/// 100 becomes 4 and 211 becomes 19. CadUtils.ToIndex and CadUtils.ToValue are the converters
	/// the DWG object and table readers already use for an entity's and a layer's lineweight, and
	/// they carry the three sentinels (29 ByLayer, 30 ByBlock, 31 Default) as well.
	/// </remarks>
	[Theory]
	[InlineData(LineWeightType.ByLayer)]
	[InlineData(LineWeightType.ByBlock)]
	[InlineData(LineWeightType.Default)]
	[InlineData(LineWeightType.W0)]
	[InlineData(LineWeightType.W25)]
	[InlineData(LineWeightType.W50)]
	[InlineData(LineWeightType.W100)]
	[InlineData(LineWeightType.W211)]
	public void EveryCurrentEntityLineWeightSurvivesTheRoundTrip(LineWeightType weight)
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h => h.CurrentEntityLineWeight = weight);

		Assert.Equal(weight, read.CurrentEntityLineWeight);
	}

	/// <summary>
	/// A weight the index table does not contain becomes Default, and takes no bit outside the
	/// five the field owns.
	/// </summary>
	/// <remarks>
	/// CadUtils.ToIndex answers 255 for a value it cannot place - Array.IndexOf returns -1 and the
	/// cast to byte wraps - so the field has to be masked on the way in. Unmasked, those bits would
	/// land on ENDCAPS, JOINSTYLE and LWDISPLAY, which is why this asserts them as well as the
	/// weight. Masked, 255 becomes 31, and 31 is the index ToValue already reads as Default.
	/// </remarks>
	[Fact]
	public void AWeightOutsideTheIndexTableBecomesDefaultAndDisturbsNothingElse()
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h =>
		{
			h.CurrentEntityLineWeight = (LineWeightType)999;
			h.EndCaps = 0;
			h.JoinStyle = 0;
			h.DisplayLineWeight = true;
		});

		Assert.Equal(LineWeightType.Default, read.CurrentEntityLineWeight);
		Assert.Equal((short)0, read.EndCaps);
		Assert.Equal((short)0, read.JoinStyle);
		Assert.True(read.DisplayLineWeight);
	}

	/// <summary>
	/// A value outside a field's own bit width stays inside it, and its neighbours are unaffected.
	/// </summary>
	/// <remarks>
	/// ENDCAPS owns two bits at 0x0060 and JOINSTYLE two at 0x0180, and the object model holds both
	/// as a plain short that any reader can fill from a file - DXF carries them in group code 280
	/// with no range check. Shifted without a mask, 4 becomes 0x80 for ENDCAPS and 0x200 for
	/// JOINSTYLE, and 0x200 is LWDISPLAY's bit, so writing a JOINSTYLE of 4 would silently invert a
	/// variable that has nothing to do with it.
	/// <para>
	/// Every neighbour is asserted against the value it was written with, not against its legal
	/// range. A mask one bit too wide keeps the overflow inside the pair - ENDCAPS 4 under a 0x7
	/// mask lands on 0x80, which is JOINSTYLE's low bit - so it produces a neighbour that is still
	/// in range and merely wrong, and a range assertion cannot see it.
	/// </para>
	/// </remarks>
	[Theory]
	[InlineData(4, 0)]
	[InlineData(16, 0)]
	[InlineData(0, 4)]
	[InlineData(0, 16)]
	[InlineData(4, 2)]
	[InlineData(2, 4)]
	public void AnOutOfRangeCapOrJoinValueDisturbsNoNeighbouringField(short endCaps, short joinStyle)
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h =>
		{
			h.EndCaps = endCaps;
			h.JoinStyle = joinStyle;
			h.DisplayLineWeight = true;
			h.XEdit = true;
			h.ExtendedNames = false;
			h.LoadOLEObject = false;
			h.CurrentEntityLineWeight = LineWeightType.W211;
		});

		Assert.True(read.DisplayLineWeight);
		Assert.True(read.XEdit);
		Assert.False(read.ExtendedNames);
		Assert.False(read.LoadOLEObject);
		Assert.Equal(LineWeightType.W211, read.CurrentEntityLineWeight);

		// Each field keeps its own two bits and nothing else, so the neighbour is exactly what it
		// was written with even when the other field overflowed.
		Assert.Equal((short)(endCaps & 0x3), read.EndCaps);
		Assert.Equal((short)(joinStyle & 0x3), read.JoinStyle);
	}

	/// <summary>
	/// The single case that names the collision directly, because it is the one that matters: a
	/// JOINSTYLE of 4 lands exactly on LWDISPLAY's bit.
	/// </summary>
	[Fact]
	public void AnOutOfRangeJoinStyleDoesNotInvertLwDisplay()
	{
		CadHeader spilled = roundTrip(ACadVersion.AC1032, h =>
		{
			h.JoinStyle = 4;
			h.DisplayLineWeight = true;
		});
		CadHeader control = roundTrip(ACadVersion.AC1032, h =>
		{
			h.JoinStyle = 0;
			h.DisplayLineWeight = true;
		});

		Assert.True(control.DisplayLineWeight);
		Assert.True(spilled.DisplayLineWeight);
	}

	/// <summary>
	/// CELWEIGHT shares its bit long with the seven fields above, so a decode that is right on its
	/// own can still be wrong beside them.
	/// </summary>
	[Fact]
	public void CelWeightIsDecodedBesideTheOtherFields()
	{
		CadHeader read = roundTrip(ACadVersion.AC1032, h =>
		{
			h.CurrentEntityLineWeight = LineWeightType.W211;
			h.EndCaps = 3;
			h.JoinStyle = 2;
			h.PlotStyleMode = 1;
			h.DisplayLineWeight = true;
		});

		Assert.Equal(LineWeightType.W211, read.CurrentEntityLineWeight);
		Assert.Equal((short)3, read.EndCaps);
		Assert.Equal((short)2, read.JoinStyle);
		Assert.Equal((short)1, read.PlotStyleMode);
		Assert.True(read.DisplayLineWeight);
	}
}
