using System.Text.Json;
using BenchmarkDotNet.Running;

namespace ACadSharp.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        new Benchmarks();

			var summary = BenchmarkRunner.Run<Benchmarks>();
        }


    }
}