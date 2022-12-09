﻿using ACadSharp.IO;
using ACadSharp.Tests.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static System.Environment;

namespace ACadSharp.Tests.IO
{
	public abstract class IOTestsBase
	{
		protected const string _samplesFolder = "../../../../samples/";

		protected const string _samplesOutFolder = "../../../../samples/out";

		public static TheoryData<string> DwgFilePaths { get; }

		public static TheoryData<string> DxfAsciiFiles { get; }

		public static TheoryData<string> DxfBinaryFiles { get; }

		public static TheoryData<ACadVersion> Versions { get; }
		public static string AcCoreConsolePath { get; }

		protected readonly ITestOutputHelper _output;

		protected readonly DocumentIntegrity _docIntegrity;

		static IOTestsBase()
		{
			DwgFilePaths = new TheoryData<string>();
			foreach (string file in Directory.GetFiles(_samplesFolder, $"*.dwg"))
			{
				DwgFilePaths.Add(file);
			}

			DxfAsciiFiles = new TheoryData<string>();
			foreach (string file in Directory.GetFiles(_samplesFolder, "*_ascii.dxf"))
			{
				DxfAsciiFiles.Add(file);
			}

			DxfBinaryFiles = new TheoryData<string>();
			foreach (string file in Directory.GetFiles(_samplesFolder, "*_binary.dxf"))
			{
				DxfBinaryFiles.Add(file);
			}

			Versions = new TheoryData<ACadVersion>();
			Versions.Add(ACadVersion.AC1021);
			Versions.Add(ACadVersion.AC1024);
			Versions.Add(ACadVersion.AC1027);
			Versions.Add(ACadVersion.AC1032);

			//Create folder, necessary in workflow
			if (!Directory.Exists(_samplesOutFolder))
			{
				Directory.CreateDirectory(_samplesOutFolder);
			}


            var acCoreConsolePath = @"D:\Programs\Autodesk\AutoCAD 2023\accoreconsole.exe";

            if (!File.Exists(acCoreConsolePath))
            {
                var programFiles = Environment.GetFolderPath(SpecialFolder.ProgramFiles);
                var autocadPath = Path.Combine(programFiles, "Autodesk");

                //"C:\\Program Files\\Autodesk\\AutoCAD LT 2021"

                var baseAutoCadPaths = new string[]
                {
                    "AutoCAD 2023",
                    "AutoCAD LT 2023",
                    "AutoCAD 2022",
                    "AutoCAD LT 2022",
                    "AutoCAD 2021",
                    "AutoCAD LT 2021",
                };

                for (int i = 0; i < baseAutoCadPaths.Length; i++)
                {
                    var consolePath = Path.Combine(autocadPath, baseAutoCadPaths[i], "accoreconsole.exe");
                    if (File.Exists(consolePath))
                    {
                        AcCoreConsolePath = consolePath;
                        break;
                    }
                }
            }

            else
            {
                AcCoreConsolePath = acCoreConsolePath;
            }
        }

		public IOTestsBase(ITestOutputHelper output)
		{
			this._output = output;
			this._docIntegrity = new DocumentIntegrity(output);
		}

		protected void onNotification(object sender, NotificationEventArgs e)
		{
			_output.WriteLine(e.Message);
		}

		protected void checkDocumentInAutocad(string path)
		{
			if (Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") != null)
				return;

			System.Diagnostics.Process process = new System.Diagnostics.Process();






            try
			{
				process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				process.StartInfo.FileName = AcCoreConsolePath;
				process.StartInfo.Arguments = $"/i \"{Path.Combine(_samplesFolder, "sample_base/empty.dwg")}\" /l en - US";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.StandardOutputEncoding = Encoding.ASCII;

				Assert.True(process.Start());

				process.StandardInput.WriteLine($"_DXFIN");
				process.StandardInput.WriteLine($"{path}");

				string l = process.StandardOutput.ReadLine();
				bool testPassed = true;
				while (!process.StandardOutput.EndOfStream)
				{
					string li = l.Replace("\0", "");
					if (!string.IsNullOrEmpty(li))
					{
						if (li.Contains("Invalid or incomplete DXF input -- drawing discarded.")
							|| li.Contains("error", StringComparison.OrdinalIgnoreCase))
						{
							testPassed = false;
						}

						_output.WriteLine(li);
					}

					var t = process.StandardOutput.ReadLineAsync();

					//The last line gets into an infinite loop
					if (t.Wait(1000))
					{
						l = t.Result;
					}
					else
					{
						break;
					}
				}

				if (!testPassed)
					throw new Exception("File loading with accoreconsole failed");
			}
			finally
			{
				process.Kill();
			}
		}

		protected static void loadSamples(string folder, string ext, TheoryData<string> files)
		{
			string path = Path.Combine(_samplesFolder, "local", folder);

			if (!Directory.Exists(path))
			{
				files.Add(string.Empty);
				return;
			}

			foreach (string file in Directory.GetFiles(path, $"*.{ext}"))
			{
				files.Add(file);
			}

			if (!files.Any())
			{
				files.Add(string.Empty);
			}
		}
	}
}
