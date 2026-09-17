using ACadSharp.Entities;
using CSMath;
using ACadSharp.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DXF;

/// <summary>
/// Two edges of the pre-R13 dimension path, each pinned against a rewritten copy of the R12 fixture.
/// </summary>
/// <remarks>
/// Both are about the boundaries of the subtype recovery rather than the recovery itself: what
/// happens to a record carrying a group code its subtype does not define, and what happens to a
/// file that is not pre-R13 at all. Neither can be asserted against the corpus as it stands, because
/// the corpus contains no such file — so the fixture is rewritten in memory, one group pair at a
/// time, and the original is never touched.
/// </remarks>
public class DxfLegacyDimensionRecordShapeTests : IOTestsBase
{
	public DxfLegacyDimensionRecordShapeTests(ITestOutputHelper output) : base(output)
	{
	}

	/// <summary>Handle of the fixture's two-line angular dimension, as the file spells it.</summary>
	private const string AngularHandle = "516";

	private static string[] FixtureLines()
		=> File.ReadAllLines(Path.Combine(TestVariables.SamplesFolder, "sample_AC1009_ascii.dxf"));

	/// <summary>
	/// Inserts one group pair into the DIMENSION record carrying <paramref name="handle"/>,
	/// immediately after that record's group 70.
	/// </summary>
	/// <remarks>
	/// After the 70 rather than at the end of the record, because these records end in extended
	/// data: a pair appended past the <c>1001</c> is read as XDATA and never reaches the entity
	/// reader at all, which makes the rewrite prove nothing. Immediately after the 70 is also where
	/// a real file would carry it.
	/// </remarks>
	private static string[] InsertAfterFlags(string[] lines, string handle, string code, string value)
	{
		List<string> outLines = new(lines.Length + 2);
		bool inRecord = false;
		bool done = false;

		for (int i = 0; i < lines.Length; i++)
		{
			string trimmed = lines[i].Trim();

			outLines.Add(lines[i]);

			if (!done && !inRecord && trimmed == "5" && i + 1 < lines.Length
				&& lines[i + 1].Trim() == handle)
			{
				inRecord = true;
				continue;
			}

			if (inRecord && trimmed == "70" && i + 1 < lines.Length)
			{
				outLines.Add(lines[i + 1]);   // the flag value
				outLines.Add(code);
				outLines.Add(value);
				i++;                          // already emitted the value
				inRecord = false;
				done = true;
			}
		}

		Assert.True(done, $"record {handle} has no group 70, so this test proves nothing");
		return outLines.ToArray();
	}

	private static string[] ReplaceValueAfter(string[] lines, string code, string oldValue, string newValue)
	{
		string[] copy = (string[])lines.Clone();
		bool done = false;

		for (int i = 0; i + 1 < copy.Length; i++)
		{
			if (copy[i].Trim() == code && copy[i + 1].Trim() == oldValue)
			{
				copy[i + 1] = newValue;
				done = true;
				break;
			}
		}

		Assert.True(done, $"no {code}/{oldValue} pair, so this test proves nothing");
		return copy;
	}

	private static CadDocument ReadRewritten(string[] lines)
	{
		string path = Path.Combine(Path.GetTempPath(), $"acadsharp-r12-{Guid.NewGuid():N}.dxf");
		try
		{
			File.WriteAllLines(path, lines);
			return DxfReader.Read(path);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	/// <summary>
	/// A group code 50 on a record that is not a linear dimension does not turn it into one.
	/// </summary>
	/// <remarks>
	/// <c>readDimension</c>'s <c>case 50:</c> arm rebuilds the object as a <see cref="DimensionLinear"/>
	/// whenever it is not already one. That is right for a placeholder, and right for a
	/// <see cref="DimensionAligned"/> whose record turns out to be rotated —
	/// <c>CadDimensionTemplate.SetDimensionObject</c> carries an aligned dimension's points across.
	/// For every other subtype nothing carries them, so before this the stray code emptied the
	/// entity, left <c>Flags</c> naming a type the object no longer was, and produced a
	/// <c>NaN</c> measurement that no validity rule declines — which a DWG then writes as group 42.
	/// Harmless while every R12 dimension loaded empty anyway; this branch is what puts real data
	/// there for it to destroy.
	/// </remarks>
	[Fact]
	public void AStrayRotationOnAnAngularRecordDoesNotRebuildItAsLinear()
	{
		CadDocument doc = ReadRewritten(InsertAfterFlags(FixtureLines(), AngularHandle, "50", "45.0"));

		DimensionAngular2Line angular = Assert.IsType<DimensionAngular2Line>(
			doc.Entities.OfType<Dimension>().Single(d => d.Handle == 0x516));

		Assert.Equal(DimensionType.Angular, angular.Flags);
		Assert.Equal(1.6475682180646770, angular.Measurement, 12);
		Assert.NotEqual(XYZ.Zero, angular.FirstPoint);
		Assert.NotEqual(XYZ.Zero, angular.SecondPoint);
		Assert.NotEqual(XYZ.Zero, angular.AngleVertex);
		Assert.NotEqual(XYZ.Zero, angular.DimensionArc);
	}

	/// <summary>
	/// The counter-case: a group code 50 on an <em>aligned</em> record still rebuilds it, because
	/// that is what a rotated linear dimension looks like in a pre-R13 file.
	/// </summary>
	/// <remarks>
	/// Without this, the guard above would be satisfied by one that refused every conversion — and
	/// the fixture's own rotated dimension would stop reading its rotation.
	/// </remarks>
	[Fact]
	public void AStrayRotationOnAnAlignedRecordStillRebuildsItAsLinear()
	{
		// Handle 527 is the fixture's aligned dimension (group 70 = 1).
		CadDocument doc = ReadRewritten(InsertAfterFlags(FixtureLines(), "527", "50", "45.0"));

		DimensionLinear linear = Assert.IsType<DimensionLinear>(
			doc.Entities.OfType<Dimension>().Single(d => d.Handle == 0x527));

		Assert.Equal(Math.PI / 4.0, linear.Rotation, 12);
		Assert.Equal(381.5863668824889, linear.FirstPoint.X, 6);
		Assert.Equal(415.7845718197619, linear.SecondPoint.X, 6);
	}

	/// <summary>
	/// The whole pre-R13 dimension path is confined to pre-R13 containers.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Two arms of <c>readDimension</c> are gated on <c>ACadVersion.AC1012</c>: the one that picks a
	/// subtype from group code 70, and the search that then resolves the subtype's own codes. This
	/// pins the pair by relabelling the R12 fixture's <c>$ACADVER</c> and changing nothing else —
	/// which produces the one file that tells them apart from an ungated reader: R13-era container,
	/// pre-R13 record shape, no <c>100</c> markers anywhere.
	/// </para>
	/// <para>
	/// Ten of its eleven DIMENSIONs are then discarded, not merely read empty: with the subtype arm
	/// gated off the template keeps its placeholder, and <c>readEntity</c> drops a placeholder. That
	/// is the deliberate choice — this branch repairs the container that has the defect and does not
	/// widen the repair to modern files on the strength of a file nobody has produced. Anyone who
	/// later decides it should will fail here first, which is the point of writing it down.
	/// </para>
	/// <para>
	/// The eleventh survives, and which one it is says something. Handle 1300 is the fixture's one
	/// record carrying a group code 50, and <c>case 50:</c> is <em>not</em> version-gated: it builds a
	/// <see cref="DimensionLinear"/> at any version, so that record alone keeps a subtype. Its
	/// defining points are still at the origin, because they are read before the 50 arrives and the
	/// search that would resolve them is gated. The asymmetry is worth pinning rather than tidying
	/// away — it is the same ungated arm the two tests above are about.
	/// </para>
	/// <para>
	/// <b>The search gate on its own is not separately killable, and that is measured rather than
	/// assumed.</b> Deleting those four lines leaves the fork suite at its baseline 17 failures of
	/// 2574 and <c>CadSharp.IO.Tests</c> entirely green, because from R13 every real file carries the
	/// markers that make the primary lookup succeed, so the fallback is never reached. It is a scope
	/// bound, not a behaviour carrier. What this test does kill is the subtype arm's gate, which is
	/// the half with consequences.
	/// </para>
	/// </remarks>
	[Fact]
	public void NoPreR13DimensionPathRunsAboveAC1012()
	{
		CadDocument relabelled = ReadRewritten(
			ReplaceValueAfter(FixtureLines(), "1", "AC1009", "AC1015"));

		Assert.Equal(ACadVersion.AC1015, relabelled.Header.Version);

		Dimension survivor = Assert.Single(relabelled.Entities.OfType<Dimension>());
		Assert.Equal(1300UL, survivor.Handle);
		DimensionLinear linear = Assert.IsType<DimensionLinear>(survivor);
		Assert.Equal(MathHelper.DegToRad(30.0), linear.Rotation, 12);
		Assert.Equal(XYZ.Zero, linear.FirstPoint);
		Assert.Equal(XYZ.Zero, linear.SecondPoint);

		// The control: the same rewrite pipeline, the same file, its own version. Without this the
		// assertion above would also hold for a rewrite that corrupted the file into unreadability.
		CadDocument asR12 = ReadRewritten(FixtureLines());

		Assert.Equal(ACadVersion.AC1009, asR12.Header.Version);
		Assert.Equal(11, asR12.Entities.OfType<Dimension>().Count());
		Assert.NotEqual(XYZ.Zero,
			asR12.Entities.OfType<DimensionLinear>().Single(d => d.Handle == 1300UL).FirstPoint);
		Assert.NotEqual(XYZ.Zero, asR12.Entities.OfType<DimensionAngular2Line>().Single().AngleVertex);

		// And the relabelled file did parse, so "ten dimensions gone" is about the dimensions and not
		// about the file having failed to read. Only a lower bound: relabelling changes far more than
		// the dimension path — the R13 reader keeps a polyline's vertices as entities of their own,
		// so this one file yields 645 entities where its R12 self yields 165 — and pinning either
		// number here would pin a behaviour this test is not about.
		Assert.True(relabelled.Entities.Count > 100,
			$"the relabelled file yielded {relabelled.Entities.Count} entities");
	}
}
