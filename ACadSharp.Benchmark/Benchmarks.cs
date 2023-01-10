using System;
using ACadSharp.Entities;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace ACadSharp.Benchmark;

[Config(typeof(FastConfig))] //comment out for marginally more accurate results
[MemoryDiagnoser]
public class Benchmarks
{
    private readonly ReadOnlyMemory<char> _parseValue;
    private readonly MText.ValueReader _reader;
    private readonly MText.Token[] _deserializedValues;
    private readonly MText.ValueWriter _writer;

    public Benchmarks()
    {
        _reader = new MText.ValueReader();
        _writer = new MText.ValueWriter();
        _parseValue =
            @"\A1;\P{\C24;\OOVERSTRIKE}{\pt128.09,405.62,526.60;FORMATTED}{\H4;safsf{\H2.64x;FORMATTED}}{BEFORE\T4.252;FORMATTED}\P{\Q14.84;FORMATTED}{\c2452;\LUNDERLINE}\P{STRIKE-THROUGH}\P{\fArial|b1|i0|c0|p34;BOLD}\P{\fArial|b0|i1|c0|p34;ITALIC\P\H0.7x;\SSUPERSET^;\H1.42857x;\P\H0.7x;\S^SUBSET;\H1.42857x;\P\H0.7x;\S1/2;\H1.42857x;\P\fArial|b0|i0|c0|p34;\P\pi-21.34894,l21.34894,t21.34894;\fSymbol|b0|i0|c2|p18;·	\fArial|b0|i0|c0|p34;BULLET 1\P\fSymbol|b0|i0|c2|p18;·	\fArial|b0|i0|c0|p34;BULLET 2\P\fSymbol|b0|i0|c2|p18;·	\fArial|b0|i0|c0|p34;BULLET 3\P\pi0,l0,tz;\P\pi-21.34894,l21.34894,t21.34894;a.	LOWERCASE LETTER A\Pb.	LOWERCASE LETTER B\Pc.	LOWERCASE LETTER C\P\pi0,l0,tz;\P\pi-21.34894,l21.34894,t21.34894;A.	UPPERCASE LETTER A\PB.	UPPERCASE LETTER B\PC.	UPPERCASE LETTER C\P\pi0,l0,tz;\P\pi-21.34894,l21.34894,t21.34894;1.	NUMBERED 1\P2.	NUMBERED 2\P3.	NUMBERED 3\P\pi0,l0,tz;\P\{TEST\}\P\PNOTE 1:	NOTE TEXT 1"" 2"" 3' 4'\P\\A1;\P\\P\PNOTE 2:	NOTE TEXT.\P}NESTED FORMATTING {\fArial|b1|i0|c0|p34;BOLD \fArial|b1|i1|c0|p34;ITALICS \LUNDERLINE \H2.24835x;FONT \fBaby Kruffy|b1|i1|c0|p2;DIFFERENT\fArial|b1|i1|c0|p34;\H0.44477x; \OOVERLINE \H0.7x;\SSUPER-TEXT^;\H1.42857x; \H0.7x;\S^SUB-TEXT;}"
                .AsMemory();
        _deserializedValues = _reader.Deserialize(_parseValue, null);
    }

    [Benchmark]
    public void ReaderDeserializeWalker()
    {
        _reader.DeserializeWalker(visit => { }, _parseValue);
    }

    [Benchmark]
    public void ReaderDeserialize()
    {
        _reader.Deserialize(_parseValue, null);
    }

    [Benchmark]
    public void WriterSerializeWalker()
    {
        _writer.SerializeWalker((in ReadOnlyMemory<char> walk) =>
        {

        }, _deserializedValues);
    }


    [Benchmark]
    public void WriterSerialize()
    {
        _writer.Serialize(_deserializedValues);
    }
}

