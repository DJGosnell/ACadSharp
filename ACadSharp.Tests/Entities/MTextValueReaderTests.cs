using System;
using ACadSharp.Entities;
using CSMath;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.IO;

namespace ACadSharp.Tests.Entities
{
    public class MTextValueReaderTests
    {

        public static IEnumerable<object[]> EscapesData = new List<object[]>()
        {
            new[] { @"\\", @"\" },
            new[] { @"\\\\\\", @"\\\" },
            new[] { @"\", @"\" },
            new[] { @"\{", @"{" },
            new[] { @"\}", @"}" },
            new[] { @"\\P", @"\P" },
            new[] { @"\\~", @"\~" },
        };

        [Theory, MemberData(nameof(EscapesData))]
        public void Escapes(string input, string expected)
        {
            var reader = new MText.ValueReader();
            var parts = reader.Parse(input);

            if (parts[0] is MText.TokenValue value1)
                Assert.Equal(expected, value1.CombinedValues);
        }

        public static IEnumerable<object[]> ReadsTextData = new List<object[]>()
        {
        new [] { "0", "0" },
        new [] { "a", "a" },
        new [] { "0123456789", "0123456789" },
        new [] { "abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz" },
        new [] { @"\P", "\n" },
        new [] { @"\", @"\" },
        new [] { @"\~", "\u00A0" },
    };

        [Theory, MemberData(nameof(ReadsTextData))]
        public void ReadsText(string input, string expected)
        {
            var reader = new MText.ValueReader();
            var parts = reader.Parse(input);

            if (parts[0] is MText.TokenValue value1)
                Assert.Equal(expected, value1.CombinedValues);
        }

        public static IEnumerable<object[]> FormatsData = new List<object[]>()
        {
            new[]
            {
                new MTextFormatsTestData(@"\A0;BOTTOM",
                    new MText.TokenValue(new() { Align = MText.Format.Alignment.Bottom }, "BOTTOM"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\A1;CENTER",
                    new MText.TokenValue(new() { Align = MText.Format.Alignment.Center }, "CENTER"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\A2;TOP",
                    new MText.TokenValue(new() { Align = MText.Format.Alignment.Top }, "TOP"))
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE{\OFORMATTED}AFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("O"), "FORMATTED"),
                        new MText.TokenValue("AFTER")
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE\OFORMATTED\oAFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("O"), "FORMATTED"),
                        new MText.TokenValue(new("o"), "AFTER")
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE{\LFORMATTED}AFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("L"), "FORMATTED"),
                        new MText.TokenValue("AFTER")
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE\LFORMATTED\lAFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("L"), "FORMATTED"),
                        new MText.TokenValue(new("l"), "AFTER")
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE{\KFORMATTED}AFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("K"), "FORMATTED"),
                        new MText.TokenValue("AFTER"),
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE\KFORMATTED\kAFTER",
                    new[]
                    {
                        new MText.TokenValue("BEFORE"),
                        new MText.TokenValue(new("K"), "FORMATTED"),
                        new MText.TokenValue(new("k"), "AFTER"),
                    })
            },
            new[]
            {
                new MTextFormatsTestData(@"BEFORE\T2;FORMATTED",
                    new[] { new MText.TokenValue("BEFORE"), new MText.TokenValue(new() { Tracking = 2 }, "FORMATTED") })
            },
            new[]
            {
                new MTextFormatsTestData(@"\H2.64x;FORMATTED",
                    new[] { new MText.TokenValue(new() { Height = 2.64f }, "FORMATTED"), })
            },
            new[]
            {
                new MTextFormatsTestData(@"\H2.64;FORMATTED",
                    new MText.TokenValue(new() { Height = 2.64f }, "FORMATTED"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\T1.8;FORMATTED",
                    new MText.TokenValue(new() { Tracking = 1.8f }, "FORMATTED"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\Q14.84;FORMATTED",
                    new MText.TokenValue(new() { Obliquing = 14.84f }, "FORMATTED"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\pt128.09,405.62,526.60;FORMATTED",
                    new MText.TokenValue(new()
                    {
                        Paragraph = new[] { "t128.09".AsMemory(), "405.62".AsMemory(), "526.60".AsMemory() }
                    }, "FORMATTED"))
            },
            new[]
            {
                new MTextFormatsTestData(@"\pi-70.76154,l70.76154,t70.76154;FORMATTED",
                    new MText.TokenValue(new()
                    {
                        Paragraph = new[] { "i-70.76154".AsMemory(), "l70.76154".AsMemory(), "t70.76154".AsMemory() }
                    }, "FORMATTED"))
            },
        };

        [Theory, MemberData(nameof(FormatsData))]
        public void Formats(MTextFormatsTestData data)
        {
            TestFormatData(data);
        }

        public static IEnumerable<object[]> FontsData = new List<object[]>()
        {
            new[]
            {
                new MTextFormatsTestData(@"{\fArial|b0|i1|c22|p123;FORMAT}",
                    new[]
                    {
                        new MText.TokenValue(new MText.Format()
                        {
                            Font =
                            {
                                FontFamily = "Arial".AsMemory(),
                                IsBold = false,
                                IsItalic = true,
                                CodePage = 22,
                                Pitch = 123
                            }
                        }, "FORMAT"),
                    })
            },
        };

        [Theory, MemberData(nameof(FontsData))]
        public void Fonts(MTextFormatsTestData data)
        {
            TestFormatData(data);
        }

        public static IEnumerable<object[]> ColorsData = new List<object[]>()
        {
            new[] { new MTextFormatsTestData(@"{\C1;1}", new MText.TokenValue(new() { Color = new Color(1) }, "1")) },
            new[]
            {
                new MTextFormatsTestData(@"{\C184;1}NORMAL",
                    new[]
                    {
                        new MText.TokenValue(new() { Color = new Color(184) }, "1"),
                        new MText.TokenValue("NORMAL")
                    })
            },

            // True Colors
            new[] { new MTextFormatsTestData(@"{\c245612;1}", new MText.TokenValue(new() { Color = Color.FromTrueColor(245612) }, "1")) },
            new[] { new MTextFormatsTestData(@"{\c0;1}", new MText.TokenValue(new() { Color = Color.FromTrueColor(0) }, "1")) },
        };

        [Theory, MemberData(nameof(ColorsData))]
        public void Colors(MTextFormatsTestData data)
        {
            TestFormatData(data);
        }

        public static IEnumerable<object[]> FractionsData = new List<object[]>()
        {
            new[]
            {
                new MTextFormatsTestData(@"\S1^2;",
                    new MText.MTextTokenFraction("1", "2", MText.MTextTokenFraction.Divider.Stacked))
            },
            new[]
            {
                new MTextFormatsTestData(@"\S1/2;",
                    new MText.MTextTokenFraction("1", "2", MText.MTextTokenFraction.Divider.FractionBar))
            },
            new[]
            {
                new MTextFormatsTestData(@"\S1#2;",
                    new MText.MTextTokenFraction("1", "2", MText.MTextTokenFraction.Divider.Condensed))
            },
            new[]
            {
                new MTextFormatsTestData(@"\SNUM^DEN;",
                    new MText.MTextTokenFraction("NUM", "DEN", MText.MTextTokenFraction.Divider.Stacked))
            },
            new[]
            {
                new MTextFormatsTestData(@"\SNUM/DEN;",
                    new MText.MTextTokenFraction("NUM", "DEN", MText.MTextTokenFraction.Divider.FractionBar))
            },
            new[]
            {
                new MTextFormatsTestData(@"\SNUM#DEN;",
                    new MText.MTextTokenFraction("NUM", "DEN", MText.MTextTokenFraction.Divider.Condensed))
            },

            // Escapes
            new[]
            {
                new MTextFormatsTestData(@"\SNUM#DEN\;;",
                    new MText.MTextTokenFraction("NUM", "DEN;", MText.MTextTokenFraction.Divider.Condensed))
            },
            new[]
            {
                new MTextFormatsTestData(@"\SNUM\##DEN\;;",
                    new MText.MTextTokenFraction("NUM#", "DEN;", MText.MTextTokenFraction.Divider.Condensed))
            },

            // Unexpected end to string.
            new[] { new MTextFormatsTestData(@"\SNUMDEN\;;", (MText.MTextToken?)null) },
            new[] { new MTextFormatsTestData(@"\SNUMDEN", (MText.MTextToken?)null) },
        };

        [Theory, MemberData(nameof(FractionsData))]
        public void Fractions(MTextFormatsTestData data)
        {
            TestFormatData(data);
        }

        private void TestFormatData(MTextFormatsTestData data)
        {
            var reader = new MText.ValueReader();
            var parts = reader.Parse(data.Input);

            if (data.Expected == null)
            {
                Assert.Empty(parts);
                return;
            }

            Assert.Equal(data.Expected!.Length, parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                Assert.Equal(data.Expected[i].Format, parts[i].Format);
                Assert.Equal(data.Expected[i], parts[i]);
            }
        }
    }
}
