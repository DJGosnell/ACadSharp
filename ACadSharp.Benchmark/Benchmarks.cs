using System;
using System.Collections.Generic;
using System.IO;
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
	private readonly Random _random;

	public Benchmarks()
	{
		this._random = new Random();
		_reader = new MText.ValueReader();
		_writer = new MText.ValueWriter();
		_parseValue =
			@"\L\O\T0.47148516;\Q-17.30884;\W0.056774848;{\f0pbn0wlq.il2|b1|i0|c69|p5;tffjykoq.3vj}\L\O\K\T2.1639878;\W2.966034;{\f5zyhppjm.lyj|b1|i0|c145|p2;\pyoieryjj.vy3,ucw0gfv3.jk2;c2oc5l4m.lar}\L\O\W3.8645608;{\C29;\fm4q4izvz.dzt|b0|i1|c1|p3;\pb1yiojvz.d5x,mmz5kyr4.mwa,r3qw0hmm.mti,5eedlr2x.jc1;lwyxq1r5.3zv}\O\T2.0711844;\W3.6625656;{\c8297353;\fyytuegai.yrd|b0|i1|c106|p5;\pqsfh2yuz.jbm,tfj0aulv.smc,gbjol2k1.5uk;g1krozx1.dit}\A0;\L\T0.76273472;\W0.9606444;{\C234;\fqswjvgz2.w5r|b0|i1|c168|p7;iixavz1b.vgd}\A1;\L\K\T2.1740312;\Q-13.136555;\W0.37002184;{\flfkv1lt5.wew|b0|i1|c25|p5;v15dpsav.ul0}\A1;\L\O\K\T4.4622392;\W2.3836738;{\c12339152;\ftnlshmti.iiw|b1|i0|c153|p0;\pbuc0rojp.5va;nqlwfhaa.pyl}\A1;\L\T3.7612856;\Q6.0669524;\W2.4327518;{\fxe0ah50g.xda|b0|i0|c239|p3;\S^ukgflnof.p2c;}\A2;\L\W0.6718468;{\ftz1sjwx0.3rf|b0|i0|c80|p7;\piqspxqwr.e2k,nm4nq31h.x41,hg042hq4.kzu;ml0cg3x4.xjh}\Q10.513465;\W3.1398124;{\C228;\ftogppkdo.gpf|b0|i0|c36|p0;\pm5obojrn.j0n,vsgc5nme.034;lhrp2qz1.2fm}\A1;\L\O\K\T3.2897856;\W0.9084316;{\c4750841;\f1rt53jqr.hu0|b0|i1|c8|p3;\peu0a2r33.s51;\S^;}\A0;\O\Q-14.249514;\W0.62547724;{\fuaxiipqt.2gr|b1|i1|c107|p9;\pwcl1ejo3.bnx,suran3nj.ggx,g5mvwrfs.n1r;islumdwg.syn}\L\Q17.493798;\W0.7451228;{\fvggzfh3d.y4o|b0|i0|c87|p7;ok1lteyh.kec}\O\Q11.67564;\W3.3643368;{\C214;\fkg5krwyh.5oc|b0|i1|c195|p3;\po1s0ptod.g2u,b1ohpzoj.wuq,aud34o2l.z13,2lmymsft.s3l;1ldhcems.ifq}\T0.54979176;\W0.024929548;{\c9305785;\fmxx2nmbv.hmy|b1|i0|c15|p9;\psu2jf0vx.g1k,c3ae1x4p.hfr,fqypa5pk.klv;0ovhvtsi.dpf}\L\O\T0.63778952;\Q18.83349;\W3.1190276;{\c2821072;\fgxvvii5r.utu|b0|i1|c139|p2;\pwavlp0j4.ggz,emw0p2dd.vmc,10nkpmd1.tnr,qx532a0e.bbb;zf2femf0.ohy}\A0;\O\Q-2.5752104;\W2.665533;{\C234;\faernzkh3.ref|b0|i0|c137|p1;\p0iwlwhjc.sm5;tsb2fs1f.tar}\A2;\W2.7948864;{\C96;\fkwl54bzl.j4r|b1|i0|c4|p0;\pf1ffhjb0.dp3,j5qsmyye.jkn,lehbfdk2.ekk;l2cln0es.qnu}\A0;\O\T4.781638;\Q8.3356256;\W1.3533862;{\fhgn4osll.24g|b0|i0|c109|p9;\pkginc3iw.x2s,y344sdzj.n4i;1pzhhpo0.mqb}\A0;\Q-12.95829;\W0.21253602;{\fal1poe0h.sgw|b0|i1|c195|p6;\p0hphkzoq.i1l;\S#;}\O\W1.641143;{\C64;\ffo41hnkt.pg3|b1|i1|c181|p5;3hxvs1zd.4er}\L\O\K\Q-18.334218;\W2.0711144;{\c8809988;\f54k234m3.5jw|b1|i0|c198|p7;\pjni441t2.4hk;\S3unrckyc.raj^nfvxl2gv.cy0;}\A2;\L\T2.9288962;\Q14.036089;\W3.5237184;{\fevtw0sil.wjs|b1|i1|c21|p2;\ppfggjxnb.qoy;2vqoeciq.o44}\L\O\W3.9082776;{\c11767601;\f2oskec4l.30j|b0|i1|c213|p8;qcwedcr5.5qb}\A1;\L\O\K\T4.1668336;\W3.646406;{\fmvoj2blk.s12|b1|i1|c100|p4;\pvvoemw50.v5v;\S^hkateo2j.hlq;}\O\T0.002721157;\W2.4186584;{\C194;\fcduq5hih.fo4|b1|i1|c31|p9;\pvwyjbvxo.mjo,gxdksify.0nc,ihvr5h2b.otn;\S/1qhtbwfm.bqm;}\L\O\T3.5226576;\Q8.7487264;\W3.5484204;{\C104;\flwea1fnq.fju|b1|i0|c164|p8;\ppgjuwblx.cjw,te5v1mwn.4fs,jeb2bmdi.bqf,u3ahynwk.vid;g1tincai.kda}\T3.0420662;\Q19.987852;\W0.85444176;{\c8262689;\fkpgzrg4s.nf2|b0|i0|c77|p8;\p2auetypz.1xf;4atvmuun.sog}\A0;\K\Q-12.464351;\W0.17719952;{\c2535112;\fw33rb0nn.1bn|b0|i0|c235|p3;\pffxzxkck.rxi,ql2gzcch.iz3,0mhloqgf.3vr,copwsph4.iki;nk1m4a2g.gyz}\A0;\L\W1.9023816;{\ftkuednqz.pvj|b0|i0|c135|p9;\pwl40fkse.d2a,025fvvre.n35,vhdxyqjv.o14;iyby1lz0.ng0}\L\W3.47368;{\C203;\fg3qms2lw.0ds|b0|i0|c218|p8;\pslna3apc.imn,otvseyms.kou,xbr1ek1b.2q1;rddtiqqp.ip5}\A1;\L\O\W0.61230688;{\c9458823;\fxz3tgdyv.pta|b0|i0|c255|p5;kp0vlqu0.aay}\L\O\Q-2.7651424;\W1.4847351;{\ftylszk2a.vk3|b1|i0|c245|p2;\p5hx5dheh.oxd,3wx3zfmh.zfs,o3ro5zgq.1ya,w3sqjn5g.a2e;lik2ngtn.53e}\A1;\L\O\T4.6061596;\W1.9942444;{\fqqukvzf1.fy5|b1|i0|c179|p2;\pnrbtbey2.0zp,a13mp5et.lpt;ep0im1ur.r02}\A1;\T1.194615;\Q-18.770796;\W2.8128996;{\fbc22lyn3.cqs|b0|i1|c25|p1;imtawj0m.p0c}\A2;\T1.9594482;\Q-5.017958;\W0.139485;{\c13507492;\f2zqqrdap.2rr|b1|i0|c225|p3;am2a2aoq.jxq}\A0;\L\O\K\T0.36404204;\W2.2131376;{\C5;\f0zhqfdku.0ku|b0|i0|c135|p8;xpn0slfj.w0e}\A0;\L\O\K\W2.3872164;{\C31;\fxn2fw3zv.w2m|b0|i0|c111|p7;\pnaftrrah.0mk,23gbmtk3.ol2;2hblr4bl.ksf}\L\K\Q18.00407;\W3.0702324;{\ffefiuqha.osm|b0|i1|c139|p0;\pqrwfmiro.hne,12vn1ffr.dfh;dibwneuh.r30}\A1;\O\K\T0.27911816;\W1.6648965;{\fa44lwuhi.5hg|b1|i0|c8|p6;\pq2qlfqde.gnz;ywwzxv5g.eua}\A0;\O\K\T3.9337352;\Q0.069008792;\W3.3820504;{\C66;\fk2lj5z45.rub|b0|i0|c43|p2;\pr0g4mh15.s2w;\Sbkbhac1p.kj3^acyljta1.au3;}\O\K\W2.3916788;{\c14932851;\frmpb1els.gsy|b0|i1|c61|p3;\p44hl4rr2.raj,p5o3kz1g.bvq,n4ls2455.paq,oqqcwq0b.bjp;ovcyxvmu.hor}\K\T2.7166284;\Q13.490015;\W1.736333;{\C7;\f5it51oih.b5g|b1|i1|c55|p2;\p2lb1fkaj.dun,stcysimr.5hr,ctq3xo44.l51;qx3h3ods.5u5}\L\O\Q2.4866544;\W0.14972139;{\c6579182;\f5ecmesgv.a04|b1|i0|c125|p5;fnmdj3cx.z0z}\A0;\L\O\T1.0003326;\W1.1852169;{\c709511;\fj0tx503o.25r|b1|i0|c181|p6;\pq35e1on1.hmj,gwligajo.mq0,murvtz2j.zxs,t2mxsnr1.ks2;jymtxmm4.gpz}\A2;\K\Q19.556924;\W0.46086956;{\fsug0lzxj.c4a|b0|i1|c93|p1;\pfphbkcdk.1h5;ndskqo4j.wxn}\O\K\W2.7285322;{\c5055231;\fuvstpjqy.xv4|b1|i0|c167|p6;ug2z30gx.43n}\A2;\L\T2.0903436;\W0.91244736;{\ffhj0pra4.zsd|b0|i0|c13|p6;\p3qmjtsv5.sul,w1xylisr.bnh;bk03wwll.q4t}\A0;\L\O\K\T0.8889176;\Q-15.273628;\W0.34766536;{\C161;\f23px44bu.p3f|b1|i1|c237|p6;\pkrbxv2gt.i1p,3nq2ui1c.2gr,affzyczf.i2b;jhravrtb.5k3}\A0;\L\Q-8.925876;\W0.47982008;{\C49;\f3v0qa03c.xkt|b1|i1|c157|p8;\pz2n3mnye.2n1,orzxzxim.3fd,hvoeihk1.x25,gnclnnzr.pij;wd5nh1r3.pkq}"
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
		_writer.SerializeWalker((in ReadOnlyMemory<char> walk) => { }, _deserializedValues);
	}


	[Benchmark]
	public void WriterSerialize()
	{
		_writer.Serialize(_deserializedValues);
	}

	private MText.Token[] randomTokens(int min, int max)
	{
		var tokenCount = this._random.Next(min, max);
		var tokens = new List<MText.Token>(tokenCount);

		for (int i = 0; i < tokenCount; i++)
		{
			if (this._random.Next(0, 10) < 8)
			{
				tokens.Add(new MText.TokenValue(this.randomFormat(), Path.GetRandomFileName().AsMemory()));
			}
			else
			{
				tokens.Add(new MText.TokenFraction(
					this.randomFormat(), this._random.Next(0, 2) == 0 ? Path.GetRandomFileName() : null,
					this._random.Next(0, 2) == 0 ? Path.GetRandomFileName() : null,
					(MText.TokenFraction.Divider)this._random.Next(0, 3)));
			}
		}

		return tokens.ToArray();
	}

	private MText.Format randomFormat()
	{
		var format = new MText.Format()
		{
			//IsHeightRelative = Convert.ToBoolean(_random.Next(0, 2)),
			IsOverline = Convert.ToBoolean(this._random.Next(0, 2)),
			IsStrikeThrough = Convert.ToBoolean(this._random.Next(0, 2)),
			IsUnderline = Convert.ToBoolean(this._random.Next(0, 2)),
		};

		if (this._random.Next(0, 2) == 0)
			format.Align = (MText.Format.Alignment)this._random.Next(0, 3);

		//if (_random.Next(0, 2) == 0)
		//    format.Height = (float)Math.Round((_random.NextDouble() * 60), 4);

		if (this._random.Next(0, 2) == 0)
			format.Obliquing = (float)(this._random.NextDouble() * 20 * (this._random.Next(0, 2) == 0 ? -1 : 1));

		if (this._random.Next(0, 2) == 0)
			format.Tracking = (float)(this._random.NextDouble() * 5);

		if (this._random.Next(0, 1) == 0)
			format.Width = (float)(this._random.NextDouble() * 4);

		if (this._random.Next(0, 2) == 0)
			format.Color = this._random.Next(0, 2) == 0
				? Color.FromTrueColor(this._random.Next(0, 1 << 24))
				: new Color((short)this._random.Next(0, 257));

		var paragraphCount = this._random.Next(0, 5);
		for (int i = 0; i < paragraphCount; i++)
		{
			format.Paragraph.Add(Path.GetRandomFileName().AsMemory());
		}

		format.Font.IsItalic = Convert.ToBoolean(this._random.Next(0, 2));
		format.Font.IsBold = Convert.ToBoolean(this._random.Next(0, 2));
		format.Font.CodePage = this._random.Next(0, 256);
		format.Font.FontFamily = Path.GetRandomFileName().AsMemory();
		format.Font.Pitch = this._random.Next(0, 10);

		return format;
	}
}

