using CSMath;
using System;

namespace ACadSharp
{
	public static class MathUtils
    {
		/// <summary>
		/// Factor for converting radians to degrees.
		/// </summary>
        public const double RadToDeg = (180 / Math.PI);

		/// <summary>
		/// Factor for converting degrees to radians.
		/// </summary>
        public const double DegToRad = (Math.PI / 180);

        public static XY GetCenter(XY start, XY end, double bulge)
		{
			return GetCenter(start, end, bulge, out _);
		}

		public static XY GetCenter(XY start, XY end, double bulge, out double radius)
		{
			double theta = 4 * Math.Atan(Math.Abs(bulge));
			double c = start.DistanceFrom(end) / 2.0;
			radius = c / Math.Sin(theta / 2.0);

			double gamma = (Math.PI - theta) / 2;
			double phi = XY.GetAngle(start, end) + Math.Sign(bulge) * gamma;
			return new XY(start.X + radius * Math.Cos(phi), start.Y + radius * Math.Sin(phi));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="bulge"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static T GetCenter<T>(T start, T end, double bulge)
			where T : IVector<T>, new()
		{
			//Needs a plane of reference in case is in 3D
			throw new NotImplementedException();
		}


        /// <summary>
        /// Gets a vector between origin vector (1, 0, 0)
        /// </summary>
        /// <param name="vector">Vector to calculate the angle from.</param>
        /// <returns>Angle in radians.</returns>
        public static double GetAngleFromOriginVector(XYZ vector)
        {
            return Math.Atan2(vector.Y, vector.X);
        }

        /// <summary>
        /// Calculates a normalized vector from an angle (in radians)
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Normalized directional vector in 2d.</returns>
        public static XYZ GetOriginVectorFromAngle(double angle)
        {
            return new XYZ(Math.Cos(angle), Math.Sin(angle), 0);
        }
    }
}
