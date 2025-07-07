using System.Diagnostics;

namespace CloudDrive.App.Model
{
    public class Benchmark
    {
        public string Name { get; }
        public string[] ExtraInfo { get; }
        public DateTime StartDateTime { get; private set; }
        public int ElapsedMillis { get; private set; }
        public DateTime EndDateTime => StartDateTime.AddMilliseconds(ElapsedMillis);

        private Stopwatch Stopwatch { get; }


        public Benchmark(string name, params string[] extraInfo)
        {
            Name = name;
            ExtraInfo = extraInfo;
            StartDateTime = DateTime.Now;
            ElapsedMillis = 0;
            Stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            Stopwatch.Stop();
            ElapsedMillis = Stopwatch.Elapsed.Milliseconds;
        }
    }
}
