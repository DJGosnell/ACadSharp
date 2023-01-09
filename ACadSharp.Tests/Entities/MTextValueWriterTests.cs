﻿using System;
using ACadSharp.Entities;
using CSMath;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.IO;

namespace ACadSharp.Tests.Entities
{
    public class MTextValueWriterTests
    {

        [Theory, MemberData(nameof(MTextValueTestData.EscapesData), MemberType = typeof(MTextValueTestData))]
        public void Escapes(MTextValueTestData.TextData data)
        {
            TestTextData(data);
        }
        
        [Theory, MemberData(nameof(MTextValueTestData.ReadsTextData), MemberType = typeof(MTextValueTestData))]
        public void ReadsText(MTextValueTestData.TextData data)
        {
            TestTextData(data);
        }

        [Theory, MemberData(nameof(MTextValueTestData.FormatsData), MemberType = typeof(MTextValueTestData))]
        public void Formats(MTextValueTestData.FormatData data)
        {
            TestFormatData(data);
        }

        [Theory, MemberData(nameof(MTextValueTestData.FontsData), MemberType = typeof(MTextValueTestData))]
        public void Fonts(MTextValueTestData.FormatData data)
        {
            TestFormatData(data);
        }

        [Theory, MemberData(nameof(MTextValueTestData.ColorsData), MemberType = typeof(MTextValueTestData))]
        public void Colors(MTextValueTestData.FormatData data)
        {
            TestFormatData(data);
        }

        [Theory, MemberData(nameof(MTextValueTestData.FractionsData), MemberType = typeof(MTextValueTestData))]
        public void Fractions(MTextValueTestData.FormatData data)
        {
            // Skip the null decodes
            if (data.Decoded == null)
                return;
            TestFormatData(data);
        }

        private void TestTextData(MTextValueTestData.TextData data)
        {
            var reader = new MText.ValueWriter();
            var parts = reader.Seralize(new[] { new MText.TokenValue(data.Decoded) });
            var concatParts = string.Concat(parts);
            for (int i = 0; i < data.Encoded.Length; i++)
            {
                if (concatParts == data.Encoded[i])
                {
                    // Passes
                    return;
                }
            }

            Assert.Equal(data.Encoded.FirstOrDefault(), concatParts);
        }

        private void TestFormatData(MTextValueTestData.FormatData data)
        {
            var reader = new MText.ValueWriter();
            var parts = reader.Seralize(data.Decoded);

            var concatParts = string.Concat(parts);
            for (int i = 0; i < data.Encoded.Length; i++)
            {
                if (concatParts == data.Encoded[i])
                {
                    // Passes
                    return;
                }
            }

            Assert.Equal(data.Encoded.FirstOrDefault(), concatParts);
        }
    }
}
