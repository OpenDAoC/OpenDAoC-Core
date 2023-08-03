using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Events;
using log4net;

namespace DOL.GS.GameEvents
{
	public class StatPrint
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static volatile Timer m_timer = null;
		private static long m_lastBytesIn = 0;
		private static long m_lastBytesOut = 0;
		private static long m_lastPacketsIn = 0;
		private static long m_lastPacketsOut = 0;
		private static long m_lastMeasureTick = DateTime.Now.Ticks;

		private static PerformanceCounter m_systemCpuUsedCounter;
		private static PerformanceCounter m_processCpuUsedCounter;
		private static PerformanceCounter m_memoryPages;
		private static PerformanceCounter m_physycalDisk;

		[GameServerStartedEvent]
		public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
		{
			lock (typeof(StatPrint))
			{
				//m_timerStatsByMgr = new Hashtable();
				//m_timer = new Timer(new TimerCallback(PrintStats), null, 10000, 0);

				bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
				if (isLinux) return;

				// Create performance counters
				// if (m_systemCpuUsedCounter == null) m_systemCpuUsedCounter	= CreatePerformanceCounter("Processor",				"% processor time",		"_total");
				// if (m_processCpuUsedCounter == null) m_processCpuUsedCounter = CreatePerformanceCounter("Process", "% processor time", GetProcessCounterName());
				// if (m_memoryPages == null) m_memoryPages = CreatePerformanceCounter("Memory", "Pages/sec", null);
				// if (m_physycalDisk == null) m_physycalDisk = CreatePerformanceCounter("PhysicalDisk", "Disk Transfers/sec", "_Total");
			}
		}

		/// <summary>
		/// Find the process counter name
		/// </summary>
		/// <returns></returns>
		public static string GetProcessCounterName()
		{
			Process process = Process.GetCurrentProcess();
			int id = process.Id;
			PerformanceCounterCategory perfCounterCat = new PerformanceCounterCategory("Process");
			foreach (DictionaryEntry entry in perfCounterCat.ReadCategory()["id process"])
			{
				string processCounterName = (string)entry.Key;
				if (((InstanceData)entry.Value).RawValue == id)
					return processCounterName;
			}
			return "";
		}

		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			lock (typeof(StatPrint))
			{
				if (m_timer != null)
				{
					m_timer.Change(Timeout.Infinite, Timeout.Infinite);
					m_timer.Dispose();
					m_timer = null;
				}

				// Release performance counters
				ReleasePerformanceCounter(ref m_systemCpuUsedCounter);
				ReleasePerformanceCounter(ref m_processCpuUsedCounter);
				ReleasePerformanceCounter(ref m_memoryPages);
				ReleasePerformanceCounter(ref m_physycalDisk);
			}
		}

		/// <summary>
		/// print out some periodic information on server statistics
		/// </summary>
		/// <param name="state"></param>
		public static void PrintStats(object state)
		{
			try
			{
				//Don't enable this line unless you have memory issues and
				//need more details in memory usage
				//GC.Collect();

				long newTick = DateTime.Now.Ticks;
				long time = newTick - m_lastMeasureTick;
				m_lastMeasureTick = newTick;
				time /= 10000000L;
				if (time < 1)
				{
					log.Warn("Time has not changed since last call of PrintStats");
					time = 1; // prevent division by zero?
				}
				long inRate = (Statistics.BytesIn - m_lastBytesIn) / time;
				long outRate = (Statistics.BytesOut - m_lastBytesOut) / time;
				long inPckRate = (Statistics.PacketsIn - m_lastPacketsIn) / time;
				long outPckRate = (Statistics.PacketsOut - m_lastPacketsOut) / time;
				m_lastBytesIn = Statistics.BytesIn;
				m_lastBytesOut = Statistics.BytesOut;
				m_lastPacketsIn = Statistics.PacketsIn;
				m_lastPacketsOut = Statistics.PacketsOut;

				// Get threadpool info
				int iocpCurrent, iocpMin, iocpMax;
				int poolCurrent, poolMin, poolMax;
				ThreadPool.GetAvailableThreads(out poolCurrent, out iocpCurrent);
				ThreadPool.GetMinThreads(out poolMin, out iocpMin);
				ThreadPool.GetMaxThreads(out poolMax, out iocpMax);

				int globalHandlers = GameEventMgr.NumGlobalHandlers;
				int objectHandlers = GameEventMgr.NumObjectHandlers;

				if (log.IsInfoEnabled)
				{
					StringBuilder stats = new StringBuilder(256)
						.Append("-stats- Mem=").Append(GC.GetTotalMemory(false) / 1024 / 1024).Append("MB")
						.Append("  Clients=").Append(GameServer.Instance.ClientCount)
						.Append("  Down=").Append(inRate / 1024).Append("kb/s (").Append(Statistics.BytesIn / 1024 / 1024).Append("MB)")
						.Append("  Up=").Append(outRate / 1024).Append("kb/s (").Append(Statistics.BytesOut / 1024 / 1024).Append("MB)")
						.Append("  In=").Append(inPckRate).Append("pck/s (").Append(Statistics.PacketsIn / 1000).Append("K)")
						.Append("  Out=").Append(outPckRate).Append("pck/s (").Append(Statistics.PacketsOut / 1000).Append("K)")
						.AppendFormat("  Pool={0}/{1}({2})", poolCurrent, poolMax, poolMin)
						.AppendFormat("  IOCP={0}/{1}({2})", iocpCurrent, iocpMax, iocpMin)
						.AppendFormat("  GH/OH={0}/{1}", globalHandlers, objectHandlers);

					if (m_systemCpuUsedCounter != null)
						stats.Append("  CPU=").Append(m_systemCpuUsedCounter.NextValue().ToString("0.0")).Append('%');
					if (m_processCpuUsedCounter != null)
						stats.Append("  DOL=").Append(m_processCpuUsedCounter.NextValue().ToString("0.0")).Append('%');
					if (m_memoryPages != null)
						stats.Append("  pg/s=").Append(m_memoryPages.NextValue().ToString("0.0"));
					if (m_physycalDisk != null)
						stats.Append("  dsk/s=").Append(m_physycalDisk.NextValue().ToString("0.0"));

					log.Info(stats);
				}
			}
			catch (Exception e)
			{
				log.Error("stats Log callback", e);
			}
			finally
			{
				lock (typeof(StatPrint))
				{
					if (m_timer != null)
					{
						m_timer.Change(ServerProperties.ServerProperties.STATPRINT_FREQUENCY, 0);
					}
				}
			}
		}

		public class TimerStats
		{
			public long InvokedCount;
			public long Time = -1;
		}

		/// <summary>
		/// Creates the performance counter.
		/// </summary>
		/// <param name="categoryName">Name of the category.</param>
		/// <param name="counterName">Name of the counter.</param>
		/// <param name="instanceName">Name of the instance.</param>
		/// <returns></returns>
		private static PerformanceCounter CreatePerformanceCounter(string categoryName, string counterName, string instanceName)
		{
			PerformanceCounter ret = null;
			try
			{
				ret = new PerformanceCounter(categoryName, counterName, instanceName);
				ret.NextValue();
			}
			catch (Exception ex)
			{
				ret = null;
				if (log.IsWarnEnabled)
					log.Warn(ex.GetType().Name + " '" + categoryName + "/" + counterName + "' counter won't be available: " + ex.Message);
			}

			return ret;
		}

		/// <summary>
		/// Releases the performance counter.
		/// </summary>
		/// <param name="performanceCounter">The performance counter.</param>
		private static void ReleasePerformanceCounter(ref PerformanceCounter performanceCounter)
		{
			if (performanceCounter != null)
			{
				performanceCounter.Close();
				performanceCounter = null;
			}
		}
	}
}