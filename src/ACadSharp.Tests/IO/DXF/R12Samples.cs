using Xunit;

namespace ACadSharp.Tests.IO.DXF;

/// <summary>
/// The pre-R13 containers in the sample corpus: one drawing, written both ways.
/// </summary>
/// <remarks>
/// Shared because every pre-R13 assertion has to hold for both encodings — that is what makes the
/// behaviour about the version rather than about ASCII versus binary — and a list declared per
/// fixture is a list that drifts.
/// </remarks>
internal static class R12Samples
{
	internal static readonly string[] Names =
	{
		"sample_AC1009_ascii.dxf",
		"sample_AC1009_binary.dxf",
	};

	/// <summary>The same two, as xUnit theory data.</summary>
	public static TheoryData<string> Theory
	{
		get
		{
			TheoryData<string> data = new();
			foreach (string name in Names)
			{
				data.Add(name);
			}

			return data;
		}
	}
}
