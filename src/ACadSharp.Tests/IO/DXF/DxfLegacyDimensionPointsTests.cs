using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DXF;

/// <summary>
/// The points a pre-R13 DIMENSION declares on its own subtype, rather than on <c>AcDbDimension</c>.
/// </summary>
/// <remarks>
/// <para>
/// A pre-R13 DXF carries no <c>100</c> subclass markers, so <c>DxfSectionReaderBase</c> fixes
/// <c>currentSubclass</c> from the first value that reaches its default arm. For a DIMENSION that
/// value is group code 10, which arrives <em>before</em> the group code 70 saying which subtype the
/// record is — so the marker is always <c>AcDbDimension</c>, the placeholder's, and every 13/14/15/16
/// and 40 was looked up in a map that does not hold it and silently dropped. Every R12 dimension
/// loaded with its subtype points at the origin.
/// </para>
/// <para>
/// Moving <c>currentSubclass</c> when code 70 is read does not fix it. A linear dimension declares
/// its two points on <c>AcDbAlignedDimension</c> and its rotation on <c>AcDbRotatedDimension</c>, so
/// no single marker covers it whichever of the two is chosen. What makes a search well-defined
/// instead is that the codes do not overlap — pinned below by
/// <see cref="NoDxfCodeIsDeclaredOnTwoSubclassesOfOneDimension"/>, which is the premise the reader
/// now rests on.
/// </para>
/// </remarks>
public class DxfLegacyDimensionPointsTests : IOTestsBase
{
	public DxfLegacyDimensionPointsTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>The R12 containers in the sample corpus — the same drawing, ASCII and binary.</summary>
	public static TheoryData<string> R12Samples { get; } = new()
	{
		"sample_AC1009_ascii.dxf",
		"sample_AC1009_binary.dxf",
	};

	/// <summary>
	/// Every dimension in the fixture that declares subtype points, by handle.
	/// </summary>
	/// <remarks>
	/// Values, not "not zero". A non-zero assertion passes for a reader that puts the definition
	/// point in every slot, which is close to what the defect looked like from the outside:
	/// <c>DimensionRadius.Measurement</c> answered 570.258 — the raw magnitude of its definition
	/// point — rather than the 11.399 radius it now measures.
	/// </remarks>
	public static IEnumerable<object[]> ExpectedPoints()
	{
		// handle, first point, second point, third point (angle vertex / none)
		yield return [1300UL, new XYZ(330.2890594765795, 2.941179455067986, 0), new XYZ(364.4872644138525, 37.13938439234094, 0), XYZ.Zero];
		yield return [1301UL, new XYZ(455.6824775799136, 8.640880277946811, 0), new XYZ(444.283075934156, 37.13938439234094, 0), new XYZ(438.5833751112772, 14.34058110082563, 0)];
		yield return [1302UL, new XYZ(495.5803833400654, 14.34058110082563, 0), new XYZ(501.2800841629442, 37.13938439234094, 0), new XYZ(495.5803833400654, 14.34058110082563, 0)];
		yield return [1317UL, new XYZ(660.8717072035513, 8.640880277946811, 0), new XYZ(700.7696129637031, 14.34058110082563, 0), XYZ.Zero];
		yield return [1318UL, new XYZ(666.5714080264308, 2.941179455067988, 0), new XYZ(677.9708096721885, 42.8390852152198, 0), XYZ.Zero];
		yield return [1319UL, new XYZ(381.5863668824889, 8.640880277946811, 0), new XYZ(415.7845718197619, 42.83908521521976, 0), XYZ.Zero];
		yield return [2510UL, new XYZ(35.99986995979099, -123.0390274605079, 0), new XYZ(40.99986995979099, -118.0390274605079, 0), XYZ.Zero];
		yield return [3222UL, new XYZ(313.7733248453183, -18.69321059672529, 0), new XYZ(348.8966563915915, -18.69321059672529, 0), XYZ.Zero];
		yield return [3236UL, new XYZ(313.9043063788445, -26.77161094058147, 0), new XYZ(348.8496376033149, -26.77161094058147, 0), XYZ.Zero];
	}

	/// <summary>Cross product of every R12 container with every expected dimension.</summary>
	public static IEnumerable<object[]> SamplesAndExpectedPoints()
		=> from file in new[] { "sample_AC1009_ascii.dxf", "sample_AC1009_binary.dxf" }
		   from row in ExpectedPoints()
		   select new[] { file }.Concat(row).ToArray();

	private static Dimension ByHandle(CadDocument doc, ulong handle)
		=> Assert.Single(doc.Entities.OfType<Dimension>(), d => d.Handle == handle);

	/// <summary>
	/// The two defining points, and the angle vertex where the subtype has one.
	/// </summary>
	[Theory]
	[MemberData(nameof(SamplesAndExpectedPoints))]
	public void ALegacyDimensionCarriesTheSubtypePointsItsRecordHolds(
		string fileName, ulong handle, XYZ first, XYZ second, XYZ vertex)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));
		Dimension dim = ByHandle(doc, handle);

		(XYZ actualFirst, XYZ actualSecond, XYZ actualVertex) = dim switch
		{
			DimensionOrdinate o => (o.FeatureLocation, o.LeaderEndpoint, XYZ.Zero),
			DimensionAngular2Line a2 => (a2.FirstPoint, a2.SecondPoint, a2.AngleVertex),
			DimensionAngular3Pt a3 => (a3.FirstPoint, a3.SecondPoint, a3.AngleVertex),
			DimensionAligned al => (al.FirstPoint, al.SecondPoint, XYZ.Zero),
			_ => throw new InvalidOperationException($"unexpected subtype {dim.GetType().Name}"),
		};

		AssertPoint(first, actualFirst, "first");
		AssertPoint(second, actualSecond, "second");
		AssertPoint(vertex, actualVertex, "vertex");
	}

	/// <summary>
	/// The rotation of a rotated linear dimension, which lives on a second subclass map again.
	/// </summary>
	/// <remarks>
	/// Handle 1300 is the fixture's one dimension with a rotation of its own — 30°, declared on
	/// <c>AcDbRotatedDimension</c> while its points are on <c>AcDbAlignedDimension</c>. It is the
	/// case no single <c>currentSubclass</c> can cover, and its measurement is the product of both:
	/// the distance between the two points, projected onto the rotation.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void ARotatedLinearDimensionCarriesBothItsPointsAndItsRotation(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));
		DimensionLinear dim = Assert.IsType<DimensionLinear>(ByHandle(doc, 1300UL));

		Assert.Equal(MathHelper.DegToRad(30.0), dim.Rotation, 9);
		Assert.Equal(46.715617, dim.Measurement, 5);
	}

	/// <summary>
	/// What each dimension measures, which is the reason the points are worth reading at all.
	/// </summary>
	/// <remarks>
	/// The two angular kinds agreeing is the drawing's own symmetry — it draws the same angle twice,
	/// once as a three-point dimension and once as a two-line one. Before this, one of the two
	/// answered 0 and the other threw.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void TheMeasurementsAreTheOnesTheGeometryGives(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));

		Assert.Equal(1.647572, ByHandle(doc, 1301UL).Measurement, 5);
		Assert.Equal(1.647572, ByHandle(doc, 1302UL).Measurement, 5);
		Assert.Equal(11.399402, ByHandle(doc, 1311UL).Measurement, 5);
		Assert.Equal(48.3635652, ByHandle(doc, 1319UL).Measurement, 6);
		Assert.Equal(35.123332, ByHandle(doc, 3222UL).Measurement, 5);
	}

	/// <summary>
	/// A radial dimension's angle vertex, which is its only subtype point.
	/// </summary>
	/// <remarks>
	/// Its measurement is the distance from the definition point to it — the radius. Reading it as
	/// the origin is what made <c>Measurement</c> answer the definition point's own magnitude, a
	/// number large enough to look like a measurement and be wrong by fifty times.
	/// </remarks>
	[Theory]
	[MemberData(nameof(R12Samples))]
	public void ARadialDimensionCarriesItsAngleVertex(string fileName)
	{
		CadDocument doc = DxfReader.Read(Path.Combine(TestVariables.SamplesFolder, fileName));
		DimensionRadius dim = Assert.IsType<DimensionRadius>(ByHandle(doc, 1311UL));

		AssertPoint(new XYZ(577.7370882426746, 33.80057695176783, 0), dim.AngleVertex, "vertex");
		Assert.Equal(0.0, dim.LeaderLength);
	}

	/// <summary>
	/// The premise the pre-R13 search rests on: within one dimension type, a DXF code belongs to
	/// exactly one subclass map.
	/// </summary>
	/// <remarks>
	/// The reader searches the subtype's maps for a code rather than naming the map it should be in,
	/// because naming one cannot cover a linear dimension. A search is only well-defined while the
	/// maps are disjoint, and nothing else in the library says they are — so it is asserted here. A
	/// subtype that redeclared a code its base already owns would make the search order significant,
	/// and would fail this before it reached a file.
	/// </remarks>
	[Fact]
	public void NoDxfCodeIsDeclaredOnTwoSubclassesOfOneDimension()
	{
		DxfMap[] maps =
		[
			DxfMap.Create<DimensionLinear>(),
			DxfMap.Create<DimensionAligned>(),
			DxfMap.Create<DimensionAngular2Line>(),
			DxfMap.Create<DimensionAngular3Pt>(),
			DxfMap.Create<DimensionDiameter>(),
			DxfMap.Create<DimensionRadius>(),
			DxfMap.Create<DimensionOrdinate>(),
		];

		List<string> collisions = [];
		foreach (DxfMap map in maps)
		{
			foreach (var group in map.SubClasses
				.SelectMany(sc => sc.Value.DxfProperties.Keys.Select(code => (code, sc.Key)))
				.GroupBy(pair => pair.code)
				.Where(g => g.Count() > 1))
			{
				collisions.Add($"{map.Name} code {group.Key} in {string.Join(" and ", group.Select(p => p.Key))}");
			}
		}

		Assert.Empty(collisions);

		// And the maps are not vacuously disjoint: each type does declare subtype codes.
		Assert.All(maps, map => Assert.True(map.SubClasses.Count >= 3, $"{map.Name} has {map.SubClasses.Count} subclasses"));
	}

	private static void AssertPoint(XYZ expected, XYZ actual, string name)
	{
		Assert.Equal(expected.X, actual.X, 6);
		Assert.Equal(expected.Y, actual.Y, 6);
		Assert.Equal(expected.Z, actual.Z, 6);
	}
}
