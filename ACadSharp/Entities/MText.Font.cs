using System;

namespace ACadSharp.Entities
{
    public partial class MText
    {
        public class Font
        {
            public ReadOnlyMemory<char>? FontFamily { get; set; } = null;
            public bool IsBold { get; set; } = false;
            public bool IsItalic { get; set; } = false;
            public int CodePage { get; set; } = 0;
            public int Pitch { get; set; } = 0;

            public Font()
            {
                
            }

            public Font(Font original)
            {
                FontFamily = original.FontFamily;
                IsBold = original.IsBold;
                IsItalic = original.IsItalic;
                CodePage = original.CodePage;
                Pitch = original.Pitch;
            }

            internal Font(string formats)
            {
                // Used only for testing
                if (formats.Contains("B"))
                    IsBold = true;

                if (formats.Contains("I"))
                    IsItalic = true;

                if (formats.Contains("b"))
                    IsBold = false;

                if (formats.Contains("i"))
                    IsItalic = false;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Font);
            }

            public bool Equals(Font other)
            {
                var fontComparison = other.FontFamily.HasValue
                    ? other.FontFamily.Value.Span
                    : ReadOnlySpan<char>.Empty;

                var nullEqual = Nullable.Equals(FontFamily, other.FontFamily);
                var comparisonEqual = FontFamily?.Span.SequenceEqual(fontComparison);

                return
                    (nullEqual || comparisonEqual == true)
                    && IsBold == other.IsBold
                    && IsItalic == other.IsItalic
                    && CodePage == other.CodePage
                    && Pitch == other.Pitch;
            }

            public override string ToString()
            {
                return
                    $"F:{FontFamily}; B:{IsBold}; I:{IsItalic}; C:{CodePage}; P:{Pitch};";
            }
        }
    }
}
