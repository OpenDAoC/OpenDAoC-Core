using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class CurrentProcessCpuUsagePercentStatistic : IPerformanceStatistic
    {
        private IPerformanceStatistic _processorTimeRatioStatistic;

        public CurrentProcessCpuUsagePercentStatistic()
        {
            _processorTimeRatioStatistic = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                           new PerformanceCounterStatistic("Process", "% processor time", Process.GetCurrentProcess().ProcessName) :
                                           new LinuxCurrentProcessUsagePercentStatistic();
        }

        public double GetNextValue()
        {
            return _processorTimeRatioStatistic.GetNextValue();
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxCurrentProcessUsagePercentStatistic : IPerformanceStatistic
    {
        // private IPerformanceStatistic _idleProcessorTimeStatistic;
        private IPerformanceStatistic _totalProcessorTimeStatistic ;
        private IPerformanceStatistic _currentProcessProcessorTimeStatistic;

        public LinuxCurrentProcessUsagePercentStatistic()
        {
            // _idleProcessorTimeStatistic = new PerSecondStatistic(new LinuxSystemIdleProcessorTimeInSeconds());
            _totalProcessorTimeStatistic = new PerSecondStatistic(new LinuxTotalProcessorTimeInSeconds());
            _currentProcessProcessorTimeStatistic = new PerSecondStatistic(new LinuxCurrentProcessProcessorTimeInSeconds());
        }

        public double GetNextValue()
        {
            // double idleTime = _idleProcessorTimeStatistic.GetNextValue();
            double totalTime = _totalProcessorTimeStatistic.GetNextValue();
            double processTime = _currentProcessProcessorTimeStatistic.GetNextValue();
            return processTime / totalTime * 100 * Environment.ProcessorCount;
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxCurrentProcessProcessorTimeInSeconds : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            int pid = Environment.ProcessId;
            string[] statArray = File.ReadAllText($"/proc/{pid}/stat").Split(' ');
            long processorTime = Convert.ToInt64(statArray[13]) + Convert.ToInt64(statArray[14]);
            return processorTime * 0.001f;
        }
    }
}
