using System;
using System.Collections.Generic;
using System.Text;
using ACadSharp.Entities;

namespace ACadSharp.Tests.Entities
{
    public class MTextFormatsTestData
    {
        public MTextFormatsTestData(string input, MText.MTextToken? expected)
            : this(input, expected == null ? null : new[] { expected })
        {
        }

        public MTextFormatsTestData(string Input, MText.MTextToken[]? Expected)
        {
            this.Input = Input;
            this.Expected = Expected;
        }

        public string Input { get; set; }
        public MText.MTextToken[]? Expected { get; set; }

        public virtual bool Equals(MTextFormatsTestData? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Input == other.Input
                   && Expected?.Equals(other.Expected) == true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Input, Expected);
        }
    }
}
