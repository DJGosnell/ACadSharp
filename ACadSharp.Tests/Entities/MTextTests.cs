using ACadSharp.Entities;
using CSMath;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Entities
{
	public class MTextTests
    {
		private List<Line> _lines;

		private Arc _arc = new Arc
		{
			Radius = 0.5,
			Center = new XYZ(1, 0.5, 0)
		};

		[Fact]
        public void SettingRotationUpdatesAlignmentPoint()
        {
            var random = new Random();
            var mText = new MText();
            for (int i = 0; i < 50; i++)
            {
                mText.Rotation = random.NextDouble();
                Assert.Equal(mText.Rotation, MathUtils.GetAngleFromOriginVector(mText.AlignmentPoint), 10);
            }
        }

        [Fact]
        public void SettingAlignmentPointUpdatesRotation()
        {
            var random = new Random();
            var mText = new MText();
            for (int i = 0; i < 50; i++)
            {
                var vector = new XYZ(random.NextDouble(), random.NextDouble(), 0).Normalize();
                mText.AlignmentPoint = vector;
                var calculatedVector = MathUtils.GetOriginVectorFromAngle(mText.Rotation);
                Assert.Equal(vector.X, calculatedVector.X, 10);
                Assert.Equal(vector.Y, calculatedVector.Y, 10);
            }
        }

    }
}
