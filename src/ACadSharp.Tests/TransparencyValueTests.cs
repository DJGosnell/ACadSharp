using Xunit;

namespace ACadSharp.Tests;

/// <summary>
/// The packed 32-bit transparency form and the two methods that convert it.
/// </summary>
/// <remarks>
/// The packed value is little-endian <c>{ alpha, 0, 0, type }</c>. The <b>type</b> byte is what says
/// which of the three states a transparency is in - 0 BYLAYER, 1 BYBLOCK, otherwise the percentage
/// is carried by the alpha byte. The alpha byte cannot say it: 0 there is both "BYLAYER, no alpha"
/// and "present and fully transparent", which are opposite ends of the range.
/// </remarks>
public class TransparencyValueTests
{
	/// <summary>
	/// Type 0 is BYLAYER whatever the alpha byte says. <c>0x0000004C</c> has an alpha byte that
	/// decodes to a plausible 70 %, and it is still BYLAYER, because the type byte is the
	/// discriminator and the alpha byte is not.
	/// </summary>
	[Theory]
	[InlineData(0x00000000)]
	[InlineData(0x0000004C)]
	[InlineData(0x000000FF)]
	public void TypeZeroIsByLayer(int packed)
	{
		Assert.True(Transparency.FromAlphaValue(packed).IsByLayer);
	}

	/// <summary>
	/// Type 1 is BYBLOCK, again regardless of the alpha byte. <c>ToAlphaValue</c> writes
	/// <c>0x01000000</c>, so the alpha byte agrees there; the other two cases prove the answer
	/// comes from the type.
	/// </summary>
	[Theory]
	[InlineData(0x01000000)]
	[InlineData(0x0100004C)]
	[InlineData(0x010000FF)]
	public void TypeOneIsByBlock(int packed)
	{
		Assert.True(Transparency.FromAlphaValue(packed).IsByBlock);
	}

	/// <summary>
	/// Any other type byte means the percentage is in the alpha byte. <c>ToAlphaValue</c> writes 2;
	/// the comment in <c>DwgStreamReaderAC18.ReadEnColor</c> and in both
	/// <c>DwgStreamWriterAC18.WriteEnColor</c> overloads says 3. Both decode, because the rule is
	/// "not one of the two symbolic types" rather than a match on a single number.
	/// </summary>
	[Theory]
	[InlineData(0x0200004C)]
	[InlineData(0x0300004C)]
	[InlineData(0x7F00004C)]
	public void AnyOtherTypeCarriesTheValueInTheAlphaByte(int packed)
	{
		Transparency transparency = Transparency.FromAlphaValue(packed);

		Assert.False(transparency.IsByLayer);
		Assert.False(transparency.IsByBlock);
		Assert.Equal((short)70, transparency.Value);
	}

	/// <summary>
	/// The two ends of the alpha byte, and the boundary the clamp actually sits on. 255 is fully
	/// opaque; 0 is the most transparent value the encoding can express and decodes to 100, which
	/// is both outside the 0..90 range <see cref="Transparency.Value"/> accepts and outside
	/// AutoCAD's own cap.
	/// </summary>
	/// <remarks>
	/// Alpha 24 is the case that pins the bound rather than something ten steps past it: it is the
	/// largest alpha byte that decodes above the cap (90.588, rounding to 91), so it is the first
	/// value a clamp written one too high would let through - and letting it through is not a
	/// wrong number but an <see cref="System.ArgumentOutOfRangeException"/> from the
	/// <see cref="Transparency.Value"/> setter. The round trip below cannot reach it, because
	/// <see cref="Transparency.ToAlphaValue"/> never emits an alpha byte below 25.
	/// </remarks>
	[Theory]
	[InlineData(0x020000FF, 0)]
	[InlineData(0x02000000, 90)]
	[InlineData(0x02000018, 90)]
	public void TheAlphaByteEndpointsClampIntoTheDocumentedRange(int packed, short expected)
	{
		Assert.Equal(expected, Transparency.FromAlphaValue(packed).Value);
	}

	/// <summary>
	/// The percentage is rounded, not truncated. 101 transparencies map onto 256 alpha bytes, so
	/// the inverse is lossy and a truncating one always loses in the same direction: AutoCAD writes
	/// 59 % as alpha 105, and <c>100 - 105 * 100 / 255</c> is 58.82, which truncates to 58.
	/// </summary>
	[Theory]
	[InlineData(0x02000069, 59)]
	[InlineData(0x02000080, 50)]
	[InlineData(0x020000B3, 30)]
	[InlineData(0x02000034, 80)]
	public void ThePercentageIsRoundedRatherThanTruncated(int packed, short expected)
	{
		Assert.Equal(expected, Transparency.FromAlphaValue(packed).Value);
	}

	/// <summary>
	/// The encoding each of the three states is written as. ByLayer had none at all before: it fell
	/// into the by-value arm and evaluated <c>(byte)(255 * 101 / 100.0)</c>, an out-of-range
	/// double-to-byte conversion the language leaves unspecified.
	/// </summary>
	[Fact]
	public void ToAlphaValueEncodesTheTypeByte()
	{
		Assert.Equal(0x00000000, Transparency.ToAlphaValue(Transparency.ByLayer));
		Assert.Equal(0x01000000, Transparency.ToAlphaValue(Transparency.ByBlock));
		Assert.Equal(0x020000FF, Transparency.ToAlphaValue(Transparency.Opaque));
		Assert.Equal(0x0200004C, Transparency.ToAlphaValue(new Transparency(70)));
		Assert.Equal(0x02000019, Transparency.ToAlphaValue(new Transparency(90)));
	}

	/// <summary>
	/// The pair is a bijection over every state a <see cref="Transparency"/> can hold - the two
	/// symbolic ones and all 91 percentages.
	/// </summary>
	[Fact]
	public void EveryStateSurvivesAnEncodeDecodeCycle()
	{
		for (short value = -1; value <= 100; value++)
		{
			if (value > 90 && value < 100)
			{
				//Not a state Transparency can hold; the setter throws.
				continue;
			}

			Transparency original = new Transparency(value);
			Transparency read = Transparency.FromAlphaValue(Transparency.ToAlphaValue(original));

			Assert.Equal(original.Value, read.Value);
		}
	}
}
