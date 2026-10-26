using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tests.Common;
using CSMath;
using CSMath.Extensions;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class DimensionAngular2LineTests : CommonDimensionTests<DimensionAngular2Line>
{
	public override DimensionType Type => DimensionType.Angular;

	[Fact]
	public void CenterTest()
	{
		var dim = this.createDim();

		AssertUtils.AreEqual(XYZ.Zero, dim.Center);
	}

	public override void GetBoundingBoxTest()
	{
		var dim = this.createDim();
		BoundingBox b = dim.GetBoundingBox();

		Assert.Equal(new BoundingBox(XYZ.Zero, XYZ.AxisX), b);
	}

	[Fact]
	public void MeasurementTest()
	{
		var dim = this.createDim();

		Assert.Equal(MathHelper.HalfPI, dim.Measurement);
	}

	[Fact]
	public void MeasurementTest_DimensionArc_BetweenVectors()
	{
		var dim = this.createDim();
		dim.DefinitionPoint = XYZ.AxisX; // 0°
		dim.SecondPoint = new XYZ(1, 1, 0).Normalize(); // 45°
		dim.DimensionArc = new XYZ(1, 0.5f, 0); // somewhere between

		Assert.Equal(MathHelper.HalfPI * 0.5f, dim.Measurement);
	}

	[Fact]
	public void MeasurementTest_DimensionArc_After2ndVector()
	{
		var dim = this.createDim();
		dim.DefinitionPoint = XYZ.AxisX; // 0°
		dim.SecondPoint = new XYZ(1, 1, 0).Normalize(); // 45°
		dim.DimensionArc = new XYZ(0, 1, 0).Normalize(); //After 2nd vector

		Assert.Equal(MathHelper.HalfPI * 1.5f, dim.Measurement);
	}

	[Fact]
	public void MeasurementTest_DimensionArc_BetweenMirroredVector()
	{
		var dim = this.createDim();
		dim.DefinitionPoint = XYZ.AxisX; // 0°
		dim.SecondPoint = new XYZ(1, 1, 0).Normalize(); // 45°
		dim.DimensionArc = new XYZ(-1, -0.5f, 0); // somewhere between

		Assert.Equal(MathHelper.HalfPI * 0.5f, dim.Measurement);
	}

	[Fact]
	public void MeasurementTest_DimensionArc_BeforeFirstVector()
	{
		var dim = this.createDim();
		dim.DefinitionPoint = XYZ.AxisX; // 0°
		dim.SecondPoint = new XYZ(1, 1, 0).Normalize(); // 45°
		dim.DimensionArc = new XYZ(0, -1, 0).Normalize(); //After 2nd vector

		Assert.Equal(MathHelper.HalfPI * 1.5f, dim.Measurement);
	}

	/// <summary>
	/// A dimension whose defining vectors are zero answers 0.0 rather than throwing.
	/// </summary>
	/// <remarks>
	/// Which is what <see cref="DimensionAngular3Pt"/> has always done for its own degenerate
	/// case. A property getter that throws is not a failure anything can route around: it makes
	/// the whole document unreadable to a property grid, a serializer or a debugger.
	/// </remarks>
	[Fact]
	public void MeasurementTest_Degenerate_AnswersZero()
	{
		DimensionAngular2Line dim = new DimensionAngular2Line();

		Assert.False(dim.IsMeasurable);
		Assert.Equal(0.0, dim.Measurement);
	}

	/// <summary>
	/// Answering 0.0 does not make the dimension writable to a DWG.
	/// </summary>
	/// <remarks>
	/// The half that stops the guard above from quietly undoing the validity rule. A DWG stores
	/// the measurement as a field of its own, so "there is no angle to measure" and "the angle is
	/// zero" are not the same thing to write - and <see cref="DimensionAngular2Line.IsMeasurable"/>
	/// is what tells them apart, which is why the rule was never keyed on the throw.
	/// </remarks>
	[Fact]
	public void ADegenerateDimensionMeasuringZeroIsStillInvalidForDwg()
	{
		DimensionAngular2Line dim = new DimensionAngular2Line();

		Assert.Equal(0.0, dim.Measurement);
		Assert.False(dim.IsValid(CadFileFormat.DWG, ACadVersion.AC1032));
	}

	/// <summary>
	/// A default dimension of this type is valid everywhere except a DWG.
	/// </summary>
	/// <remarks>
	/// <see cref="CommonEntityTests{T}.ValidEntityTest"/> asserts that every entity is valid on
	/// creation, which this one deliberately is not: a default-constructed angular dimension has
	/// no angle, and a DWG cannot store that. The base case is replaced rather than relaxed, so
	/// the exception is stated here instead of being a hole in the general rule - and so the two
	/// DXF cases it also covers keep being asserted.
	/// </remarks>
	public override void ValidEntityTest(CadFileFormat format, ACadVersion version)
	{
		//A DWG stores a dimension's measurement as a field of its own, so an angular dimension
		//with no angle to measure cannot be written to one. Every other container can hold it.
		Assert.Equal(format != CadFileFormat.DWG, new DimensionAngular2Line().IsValid(format, version));

		//And one that can be measured is valid everywhere - without this the rule above would be
		//satisfied by an IsValid that answered false for every angular dimension.
		Assert.True(this.createDim().IsValid(format, version));
	}

	public override void UpdateBlockTests()
	{
	}

	protected override DimensionAngular2Line createDim()
	{
		DimensionAngular2Line angular = new DimensionAngular2Line();
		angular.FirstPoint = XYZ.Zero;
		angular.SecondPoint = XYZ.AxisX;

		angular.AngleVertex = XYZ.Zero;
		angular.DefinitionPoint = XYZ.AxisY;

		return angular;
	}
}