using ACadSharp.Entities;
using ACadSharp.IO;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DXF;

/// <summary>
/// Which subtype a pre-R13 DIMENSION becomes, given the flag word that is the only thing saying so.
/// </summary>
/// <remarks>
/// <para>
/// A pre-R13 DXF carries no <c>100</c> subclass markers, so <c>DxfSectionReaderBase.readDimension</c>
/// reads group code 70 to decide what to build. <see cref="DimensionType"/> is a flag enum, but only
/// three of its members are flags: <c>BlockReference</c> (32), <c>OrdinateTypeX</c> (64) and
/// <c>TextUserDefinedLocation</c> (128). <c>Linear</c> (0) through <c>Ordinate</c> (6) are an
/// enumeration packed into the low three bits of the same word.
/// </para>
/// <para>
/// Switching on the whole word therefore matched nothing for any record carrying a modifier. The
/// template kept its <c>DimensionPlaceholder</c>, and the reader discards a placeholder outright —
/// so the dimension was not merely read wrong, it was <b>lost</b>, behind a
/// <c>NotificationType.Warning</c> nobody reads.
/// </para>
/// </remarks>
public class DxfLegacyDimensionTypeTests : IOTestsBase
{
	public DxfLegacyDimensionTypeTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>The R12 containers in the sample corpus — the same drawing, ASCII and binary.</summary>
	public static TheoryData<string> R12Samples { get; } = new()
	{
		"sample_AC1009_ascii.dxf",
		"sample_AC1009_binary.dxf",
	};

	/// <summary>
	/// Every DIMENSION record in the file becomes a dimension in the document.
	/// </summary>
	/// <remarks>
	/// The fixture holds 11 DIMENSION records. It used to load 10: the one whose group code 70 is
	/// <c>131</c> — <c>Diameter | TextUserDefinedLocation</c> — matched no arm of the subtype switch.
	/// A count is the assertion that catches a loss, because a dimension read as the wrong subtype is
	/// still present and a dimension with no subtype is not.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void EveryLegacyDimensionRecordBecomesADimension(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));

		Assert.Equal(11, doc.Entities.OfType<Dimension>().Count());
	}

	/// <summary>
	/// A modifier bit does not change which subtype the record is.
	/// </summary>
	/// <remarks>
	/// The counter-assertion to the count above, and the one that says the record was read
	/// <em>correctly</em> rather than merely read. <c>70 = 131</c> is a diameter dimension whose text
	/// was placed by hand; the 128 says where the text is and nothing about the subtype, so it is a
	/// <see cref="DimensionDiameter"/> — and it still reports the modifier it carried.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void AModifierFlagDoesNotChangeTheSubtype(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));

		Dimension modified = Assert.Single(
			doc.Entities.OfType<Dimension>(),
			d => d.Flags.HasFlag(DimensionType.TextUserDefinedLocation));

		Assert.IsType<DimensionDiameter>(modified);
		Assert.Equal(131, (int)modified.Flags);
		Assert.True(modified.IsTextUserDefinedLocation);
	}

	/// <summary>
	/// The subtypes the fixture's eleven records name, as a set.
	/// </summary>
	/// <remarks>
	/// Pins the mapping from flag word to type across every arm the fixture exercises — 0 Linear,
	/// 1 Aligned, 2 Angular (two-line), 4 Radius, 5 Angular (three-point), 6 and 70 Ordinate, and
	/// 131 Diameter. A mask applied to the wrong width would keep the count above correct while
	/// building the wrong types.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void TheSubtypesAreTheOnesTheFlagWordsName(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));

		string census = string.Join(", ", doc.Entities.OfType<Dimension>()
			.GroupBy(d => d.GetType().Name)
			.OrderBy(g => g.Key)
			.Select(g => $"{g.Key}:{g.Count()}"));

		Assert.Equal(
			"DimensionAligned:2, DimensionAngular2Line:1, DimensionAngular3Pt:1, "
			+ "DimensionDiameter:1, DimensionLinear:3, DimensionOrdinate:2, DimensionRadius:1",
			census);
	}
}
