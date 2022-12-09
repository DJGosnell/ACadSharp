using CSMath;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.Common
{
    public class MathUtilsTests
    {
        [Fact]
        public void GetAngleFromOriginVector()
        {
            var random = new Random();
            for (int i = 0; i < 50; i++)
            {
                var x = random.NextDouble();
                var y = random.NextDouble();
                var vector = new XYZ(x, y, 0).Normalize();
                var angle = MathUtils.GetAngleFromOriginVector(vector);
                var calculatedVector = MathUtils.GetOriginVectorFromAngle(angle);

                Assert.Equal(vector.X, calculatedVector.X, 10);
                Assert.Equal(vector.Y, calculatedVector.Y, 10);
            }
        }
    }
}
