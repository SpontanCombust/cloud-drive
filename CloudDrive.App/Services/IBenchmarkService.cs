using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface IBenchmarkService
    {
        Benchmark StartBenchmark(string benchmarkName, params string[] extraInfo);
        void StopBenchmark(Benchmark benchmark);
        void OpenBenchmarkFile();
    }
}
