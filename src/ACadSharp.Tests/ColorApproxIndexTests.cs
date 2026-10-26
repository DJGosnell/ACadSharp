using System;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests;

/// <summary>
/// <see cref="Color.ApproxIndex(byte, byte, byte)"/> is a nearest-neighbour search over the index
/// table. These pin the four claims that search makes: it finds the minimum, it finds it over the
/// whole table, it never answers the dummy entry, and it breaks ties at the lowest index.
/// </summary>
public class ColorApproxIndexTests
{
	/// <summary>
	/// The index table as the public surface exposes it, built from <see cref="Color.GetIndexRGB"/>
	/// rather than from the private field so that these cases carry no reflection.
	/// </summary>
	/// <remarks>
	/// It is bounded at 256 because that is what a <see cref="byte"/> addresses, which is all
	/// <see cref="Color.GetIndexRGB"/> accepts. These cases therefore do <em>not</em> detect a
	/// private table grown past 256 rows — nothing reachable from the public surface can. That
	/// invariant is held by <see cref="Color.ApproxIndex"/> bounding its own search instead, and it
	/// is the one claim in this file with no test behind it.
	/// </remarks>
	private static readonly byte[][] _table = buildTable();

	private readonly ITestOutputHelper _output;

	public ColorApproxIndexTests(ITestOutputHelper output)
	{
		this._output = output;
	}

	/// <summary>
	/// The regression this class exists for. <c>sample_AC1032.dwg</c>'s <c>$CECOLOR</c> is the true
	/// color below, and the search used to answer index 2 - pure yellow, 525 away in L1 - because
	/// it compared a signed channel sum against a running minimum it never updated. AutoCAD's own
	/// DXF export of that drawing says 193, so 193's distance is the bound to beat: an answer no
	/// worse than the one the application we interoperate with chose.
	/// </summary>
	/// <remarks>
	/// Deliberately a bound and not an index. Which index wins is a property of the metric, and
	/// <see cref="TheAnswerIsTheMinimumOverTheWholeTable"/> is what pins the metric; stating the
	/// expected index here as well would make this case fail for a second reason whenever that one
	/// fails, and would pin an answer the issue that raised this could not settle.
	/// </remarks>
	[Fact]
	public void ATrueColorAnswersAnIndexAtLeastAsCloseAsAutoCadsOwn()
	{
		const byte r = 155, g = 66, b = 236;

		byte answer = Color.ApproxIndex(r, g, b);

		this._output.WriteLine($"({r},{g},{b}) -> {answer} " +
			$"({_table[answer][0]},{_table[answer][1]},{_table[answer][2]}) d={distance(r, g, b, answer)}");

		Assert.NotEqual(2, answer);
		Assert.True(distance(r, g, b, answer) <= distance(r, g, b, 193),
			$"index {answer} is {distance(r, g, b, answer)} away; AutoCAD's 193 is {distance(r, g, b, 193)}");
	}

	/// <summary>
	/// The search has to agree with an exhaustive minimum for every color, not only for the one
	/// that raised the issue. This is what pins the metric, and what would catch a channel read
	/// from the wrong column or a running minimum seeded below every achievable distance.
	/// </summary>
	/// <remarks>
	/// The brute force below shares the metric with the implementation, so this case can only catch
	/// a substituted metric on a color the two metrics disagree about. Most colors are not such a
	/// color, so the ones that are have to be chosen deliberately rather than sampled - see the
	/// comment on the last two rows.
	/// </remarks>
	[Theory]
	[InlineData(155, 66, 236)]
	[InlineData(0, 0, 0)]
	[InlineData(255, 255, 255)]
	[InlineData(1, 1, 1)]
	[InlineData(254, 254, 254)]
	[InlineData(128, 128, 128)]
	[InlineData(255, 0, 0)]
	[InlineData(0, 255, 0)]
	[InlineData(0, 0, 255)]
	[InlineData(228, 28, 128)]
	[InlineData(17, 240, 3)]
	[InlineData(200, 100, 50)]
	// The two below are the only rows that discriminate between candidate metrics, and they are why
	// the remark above is true. It is not that the others are near a palette entry - four of them are
	// a long way from one - but that they are colors the candidate metrics happen to agree about,
	// which most colors are: measured over the whole cube, a substituted L1 picks a different index
	// than squared Euclidean for 8.9% of colors. So a discriminating color has to be chosen, and
	// these two were: (218,134,93) answers 11 at 2574 under this metric and 33 at 3030 under either
	// L1 or a luminance-weighted L2, and (209,210,116) answers 61 where both of those answer 41.
	[InlineData(218, 134, 93)]
	[InlineData(209, 210, 116)]
	public void TheAnswerIsTheMinimumOverTheWholeTable(int r, int g, int b)
	{
		byte answer = Color.ApproxIndex((byte)r, (byte)g, (byte)b);

		int best = int.MaxValue;
		for (int i = 1; i < _table.Length; i++)
		{
			best = Math.Min(best, distance(r, g, b, i));
		}

		Assert.Equal(best, distance(r, g, b, answer));
	}

	/// <summary>
	/// Every color in the table, fed back in, answers an index carrying that same RGB. Not
	/// necessarily its own index: thirteen rows duplicate an earlier one, and a tie goes to the
	/// lower. Before the fix, 248 of the 255 answered a different color altogether.
	/// </summary>
	[Fact]
	public void EveryPaletteColorAnswersAnIndexCarryingThatColor()
	{
		for (int i = 1; i < _table.Length; i++)
		{
			byte answer = Color.ApproxIndex(_table[i][0], _table[i][1], _table[i][2]);

			Assert.True(
				_table[answer][0] == _table[i][0] &&
				_table[answer][1] == _table[i][1] &&
				_table[answer][2] == _table[i][2],
				$"index {i} ({_table[i][0]},{_table[i][1]},{_table[i][2]}) answered {answer} " +
				$"({_table[answer][0]},{_table[answer][1]},{_table[answer][2]})");
		}
	}

	/// <summary>
	/// Index 0 is the table's dummy entry, and a search that includes it swallows every dark color.
	/// Excluding it is free rather than a compromise: index 250 is a genuine black, so pure black
	/// still answers at distance 0 - which is the assertion below, and is what makes this case fail
	/// if the loop is started at 0 again.
	/// </summary>
	[Fact]
	public void TheDummyEntryIsNeverAnswered()
	{
		Assert.Equal(250, Color.ApproxIndex(0, 0, 0));
		Assert.Equal(0, distance(0, 0, 0, Color.ApproxIndex(0, 0, 0)));

		for (int i = 0; i < _table.Length; i++)
		{
			Assert.NotEqual(0, Color.ApproxIndex(_table[i][0], _table[i][1], _table[i][2]));
		}
	}

	/// <summary>
	/// Ties go to the lowest index. Observable because the table repeats colors: index 1 and index
	/// 10 are both pure red, 2 and 11 are not - so the first duplicated color is the case that
	/// distinguishes a strictly-less comparison from a less-or-equal one.
	/// </summary>
	[Fact]
	public void ATieGoesToTheLowestIndex()
	{
		int tiesChecked = 0;

		for (int i = 1; i < _table.Length; i++)
		{
			int firstWithThisColor = i;
			for (int j = 1; j < i; j++)
			{
				if (_table[j][0] == _table[i][0] && _table[j][1] == _table[i][1] && _table[j][2] == _table[i][2])
				{
					firstWithThisColor = j;
					break;
				}
			}

			if (firstWithThisColor == i)
			{
				continue;
			}

			Assert.Equal(firstWithThisColor, Color.ApproxIndex(_table[i][0], _table[i][1], _table[i][2]));
			tiesChecked++;
		}

		// Without this the case passes by reaching `continue` every time, and the thirteen the
		// remarks above claim would be a number in prose rather than a number in an assertion.
		Assert.Equal(13, tiesChecked);
	}

	/// <summary>
	/// A signed channel sum lets errors cancel, so a color +100 on one channel and -100 on another
	/// scored an exact match against an entry it is nowhere near. (128,128,128) is index 8, and
	/// (228,28,128) sums to exactly 0 against it while being 20000 away.
	/// </summary>
	[Fact]
	public void ChannelErrorsDoNotCancel()
	{
		byte answer = Color.ApproxIndex(228, 28, 128);

		Assert.NotEqual(8, answer);
		Assert.True(distance(228, 28, 128, answer) < distance(228, 28, 128, 8),
			$"index {answer} is {distance(228, 28, 128, answer)} away; index 8 is {distance(228, 28, 128, 8)}");
	}

	private static int distance(int r, int g, int b, int index)
	{
		int dr = r - _table[index][0];
		int dg = g - _table[index][1];
		int db = b - _table[index][2];
		return (dr * dr) + (dg * dg) + (db * db);
	}

	private static byte[][] buildTable()
	{
		var table = new byte[256][];
		for (int i = 0; i < table.Length; i++)
		{
			table[i] = Color.GetIndexRGB((byte)i).ToArray();
		}

		return table;
	}
}
