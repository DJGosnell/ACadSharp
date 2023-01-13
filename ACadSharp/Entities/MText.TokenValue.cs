﻿#nullable enable
using System;
using System.Collections.Generic;

namespace ACadSharp.Entities
{
	public partial class MText
	{
		/// <summary>
		/// Contains a formatted value.
		/// </summary>
		public class TokenValue : Token
		{
			/// <summary>
			/// Contains all the token values.  Will normally be multiple splices of memory.
			/// </summary>
			/// <remarks>Does not allocate.</remarks>
			public IReadOnlyList<ReadOnlyMemory<char>>? Values { get; internal set; }

			/// <summary>
			/// Helper method which will combine all the <see cref="Values"/> into a single new string.
			/// </summary>
			/// <remarks>Allocates.</remarks>
			public string CombinedValues => this.Values == null ? string.Empty : string.Concat(this.Values);

			/// <summary>
			/// Creates a blank token value for holding a list of memory.
			/// </summary>
			public TokenValue()
			{
			}

			/// <summary>
			/// Creates a token value with the passed parameters for it's starting state.
			/// </summary>
			/// <param name="format">Current Format of the value.</param>
			/// <param name="value">Single value this token contains.</param>
			public TokenValue(Format format, ReadOnlyMemory<char> value)
				: base(format)
			{
				this.Values = new[] { value };
			}

			/// <summary>
			/// Creates a token value with the passed parameters for it's starting state.
			/// </summary>
			/// <param name="format">Current Format of the value.</param>
			/// <param name="values">Multiple values this token contains.</param>
			public TokenValue(Format format, ReadOnlyMemory<char>[] values)
				: base(format)
			{
				this.Values = values;
			}

			/// <summary>
			/// Creates a token value with the passed parameters for it's starting state.  Used for testing.
			/// </summary>
			/// <param name="format">Current Format of the value.</param>
			/// <param name="value">String value this token contains.</param>
			internal TokenValue(Format format, string value)
				: base(format)
			{
				this.Values = new[] { value.AsMemory() };
			}

			/// <summary>
			/// Creates a token value with the passed parameters for it's starting state.  Used for testing.
			/// </summary>
			/// <param name="value">String value this token contains.</param>
			internal TokenValue(string value)
				: base(new Format())
			{
				this.Values = new[] { value.AsMemory() };
			}


			public override string ToString()
			{
				return this.CombinedValues;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != this.GetType())
				{
					return false;
				}

				return ((TokenValue)obj).CombinedValues == this.CombinedValues;
			}

			public override int GetHashCode()
			{
#if NETFRAMEWORK
                return base.GetHashCode();
#else
				return HashCode.Combine(this.Format, this.Values);
#endif
			}
		}
	}
}