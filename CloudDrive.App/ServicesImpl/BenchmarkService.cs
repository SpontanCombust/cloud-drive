using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CloudDrive.App.ServicesImpl
{
    internal class BenchmarkService : IBenchmarkService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly object _csvLock = new object();

        readonly string DATETIME_FORMAT = "dd.MM.yyyy hh:mm:ss.fff";

        public BenchmarkService(ILogger<BenchmarkService> logger)
        {
            _logger = logger;
        }


        public Benchmark StartBenchmark(string benchmarkName, params string[] extraInfo)
        {
            return new Benchmark(benchmarkName, extraInfo);
        }

        public void StopBenchmark(Benchmark benchmark)
        {
            benchmark.Stop();

            var benchmarkColumns = new List<string>(4);

            benchmarkColumns.Add(benchmark.Name);
            benchmarkColumns.Add(benchmark.StartDateTime.ToString(DATETIME_FORMAT));
            benchmarkColumns.Add(benchmark.EndDateTime.ToString(DATETIME_FORMAT));
            benchmarkColumns.Add(benchmark.ElapsedMillis.ToString());
            benchmarkColumns.AddRange(benchmark.ExtraInfo);

            string csvLine = string.Join(";", benchmarkColumns);

            lock (_csvLock)
            {
                File.AppendAllLines(BenchmarkCsvPath, [csvLine]);
            }
        }

        public void OpenBenchmarkFile()
        {
            if (!File.Exists(BenchmarkCsvPath))
            {
                throw new Exception("plik nie istnieje");
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BenchmarkCsvPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Błąd otwarcia pliku testów wydajności: {}", ex.Message);
                throw;
            }
        }



        private static string BenchmarkCsvPath
        {
            get
            {
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive");
                Directory.CreateDirectory(appDataPath);
                return Path.Combine(appDataPath, "benchmarks.csv");
            }
        }
    }
}
