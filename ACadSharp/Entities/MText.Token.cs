using ACadSharp.Attributes;
using System.Collections.Generic;

namespace ACadSharp.Entities
{
    public partial class MText
    {
        public abstract class MTextToken
        {
            public MText.Format Format { get; internal set; }

            public MTextToken(MText.Format format)
            {
                Format = format;
            }
        }
    }
}
