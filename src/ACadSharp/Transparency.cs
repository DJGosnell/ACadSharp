using System;

namespace ACadSharp
{
	/// <summary>
	/// Represents the transparency for the graphical objects.
	/// </summary>
	public struct Transparency
	{
		/// <summary>
		/// Gets the ByLayer transparency.
		/// </summary>
		public static Transparency ByLayer { get { return new Transparency(-1); } }

		/// <summary>
		/// Gets the ByBlock transparency.
		/// </summary>
		public static Transparency ByBlock { get { return new Transparency(100); } }

		/// <summary>
		/// Gets the Opaque transparency.
		/// </summary>
		public static Transparency Opaque { get { return new Transparency(0); } }

		/// <summary>
		/// Defines if the transparency is defined by layer.
		/// </summary>
		public bool IsByLayer
		{
			get { return _value == -1; }
		}

		/// <summary>
		/// Defines if the transparency is defined by block.
		/// </summary>
		public bool IsByBlock
		{
			get { return _value == 100; }
		}

		/// <summary>
		/// Gets or sets the transparency value.
		/// </summary>
		/// <remarks>
		/// Transparency values must be in range from 0 (opaque) to 90 (transparent), the reserved values -1 and 100 represents ByLayer and ByBlock.
		/// </remarks>
		public short Value
		{
			get { return _value; }
			set
			{
				if (value == -1)
				{
					_value = value;
					return;
				}

				if (value == 100)
				{
					_value = value;
					return;
				}

				if (value < 0 || value > 90)
					throw new ArgumentOutOfRangeException(nameof(value), value, "Transparency must be in range from 0 to 90.");

				_value = value;
			}
		}

		private short _value;

		/// <summary>
		/// Initializes a new instance of the Transparency struct.
		/// </summary>
		/// <param name="value">Alpha value range from 0 to 90.</param>
		/// <remarks>
		/// Transparency values must be in range from 0 (opaque) to 90 (transparent), the reserved values -1 and 100 represents ByLayer and ByBlock.
		/// </remarks>
		public Transparency(short value)
		{
			_value = -1;
			this.Value = value;
		}

		/// <summary>
		/// Gets the alpha value of a transperency.
		/// </summary>
		/// <param name="transparency">The transparency to encode.</param>
		/// <returns>The packed value described in <see cref="FromAlphaValue(int)"/>.</returns>
		public static int ToAlphaValue(Transparency transparency)
		{
			if (transparency.IsByLayer)
			{
				return BitConverter.ToInt32(new byte[] { 0, 0, 0, 0 }, 0);
			}

			if (transparency.IsByBlock)
			{
				return BitConverter.ToInt32(new byte[] { 0, 0, 0, 1 }, 0);
			}

			//Only computed on this arm: for a ByLayer transparency the expression is
			//(byte)(255 * 101 / 100.0), an out of range double to byte conversion the language
			//leaves unspecified.
			byte alpha = (byte)(255 * (100 - transparency.Value) / 100.0);
			return BitConverter.ToInt32(new byte[] { alpha, 0, 0, 2 }, 0);
		}

		/// <summary>
		/// Gets the transparency from a transparency value
		/// </summary>
		/// <param name="value">A transparency value</param>
		/// <returns>A <see cref="Transparency"></see></returns>
		/// <remarks>
		/// The packed form is little endian <c>{ alpha, 0, 0, type }</c>. The <b>type</b> byte is
		/// what says which of the three states the value is in: 0 is BYLAYER, 1 is BYBLOCK, and
		/// anything else means the percentage is carried by the alpha byte. The alpha byte cannot
		/// say it on its own - 0 there is both "BYLAYER, no alpha" and "present and fully
		/// transparent", which are opposite ends of the range.
		/// </remarks>
		public static Transparency FromAlphaValue(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);

			if (bytes[3] == 0)
			{
				return ByLayer;
			}

			if (bytes[3] == 1)
			{
				return ByBlock;
			}

			//Rounded, not truncated: 101 transparencies are packed onto 256 alpha values, so the
			//inverse is lossy and a truncating one always loses in the same direction. AutoCAD
			//writes 59% as alpha 105, and 100 - 105 * 100 / 255 is 58.82.
			//Multiplying before dividing also keeps the numerator exact where dividing first does
			//not.
			short alpha = (short)Math.Round(100.0 - bytes[0] * 100.0 / 255.0);

			//bytes[0] is a byte, so alpha is closed over 0..100 and only the top needs clamping.
			//It needs it: alpha 0 is the most transparent value the encoding can express
			//and decodes to 100, which is both outside the range Value accepts and above AutoCAD's
			//own cap of 90.
			return new Transparency(alpha > 90 ? (short)90 : alpha);
		}
	}
}
