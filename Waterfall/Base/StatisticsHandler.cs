/*
This code is based on the work presented by Thomas Kejser (http://kejser.org/thomas-kejser-biography/)
in a series of his blog entries:
Part 1: http://kejser.org/synchronisation-in-net-part-1-lock-dictionaries-and-arrays/
Part 2: http://blog.kejser.org/synchronisation-in-net-part-2-unsafe-data-structure-and-padding/
Part 3: http://kejser.org/synchronisation-in-net-part-3-spinlocks-and-interlocks/
Part 4: http://kejser.org/synchronisation-in-net-part-4-partitioned-data-structures/
*/

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Waterfall.Base
{
    /// <summary>
    /// Struct containing performance stats of waterfall.
    /// <para>Activated by WATERFALLSTATS_ON conditional compilation symbol</para>
    /// </summary>
    public struct WaterfallStats
    {
        /// <summary>
        /// Work execution count.
        /// </summary>
        public long Count;
        /// <summary>
        /// Aggregated time in milliseconds.
        /// </summary>
        public double TotalTimeInMs;
        /// <summary>
        /// Avg. time in milliseconds.
        /// </summary>
        public double AvgTimeInMs;
        /// <summary>
        /// Max time in milliseconds.
        /// </summary>
        public double MaxTimeMs;
        /// <summary>
        /// Min time in milliseconds.
        /// </summary>
        public double MinTimeMs;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="minTime">Initial min time value</param>
        internal WaterfallStats(double minTime)
        {
            AvgTimeInMs = TotalTimeInMs = MaxTimeMs = Count = 0;
            MinTimeMs = minTime;
        }
    }

    /// <summary>
    /// Class to compute statistics.
    /// <para>Based on the work presented by Thomas Kejser (http://kejser.org/thomas-kejser-biography/)</para>
    /// </summary>
    public abstract class StatisticsHandler
    {
#if WATERFALLSTATS_ON
        [DllImport("kernel32.dll")]
        private static extern int GetCurrentProcessorNumber();

        [ThreadStatic] private static bool _knowsCore;
        [ThreadStatic] private static int _cpuIndex;

        private int _totalWork = 0;
        private WorkStats[,] _stats = null;
#endif

        /// <summary>
        /// Initializes stats container for total number of work items.
        /// </summary>
        /// <param name="totalWork">Total work items</param>
        protected void Init(int totalWork)
        {
#if WATERFALLSTATS_ON
            _totalWork = totalWork;
            _stats = GetInitializedStatsArray(totalWork);
#endif
        }

        private static WorkStats[,] GetInitializedStatsArray(int totalWork)
        {
            var processorCount = Environment.ProcessorCount;
            var stats = new WorkStats[processorCount, totalWork];
            for (var processerIndex = 0; processerIndex < processorCount; processerIndex++)
            {
                for (var workIndex = 0; workIndex < totalWork; workIndex++)
                {
                    stats[processerIndex, workIndex] = new WorkStats(long.MaxValue);
                }
            }
            return stats;
        }

        /// <summary>
        /// Updates stats for a given work item.
        /// </summary>
        /// <param name="workPosition">work identifier</param>
        /// <param name="timeInTicks">total execution time in timer ticks</param>
        protected unsafe void AddStats(int workPosition, long timeInTicks)
        {
#if WATERFALLSTATS_ON
            try {}
            finally
            {
                if (!_knowsCore)
                {
                    _cpuIndex = GetCurrentProcessorNumber();
                    _knowsCore = true;
                }
                fixed (WorkStats* s = &_stats[_cpuIndex, workPosition])
                {
                    //These 2 are crucial info
                    Interlocked.Add(ref s->TimeTicks, timeInTicks);
                    Interlocked.Add(ref s->Count, 1L);

                    //avoiding CAS with LOOP for these 2
                    if (s->MinTimeTicks>timeInTicks)
                    {
                        Interlocked.Exchange(ref s->MinTimeTicks, timeInTicks);
                    }
                    if (s->MaxTimeTicks<timeInTicks)
                    {
                        Interlocked.Exchange(ref s->MaxTimeTicks, timeInTicks);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Provides stats for a given work item.
        /// </summary>
        /// <param name="position">work identifier</param>
        /// <returns>Performance Statistics</returns>
        protected WaterfallStats WorkStatistics(int position)
        {
#if WATERFALLSTATS_ON
            var waterfallStats = new WaterfallStats(double.MaxValue);
            var localStats = _stats;
            for (var i = 0; i<Environment.ProcessorCount; i++)
            {
                var countj = Interlocked.Read(ref localStats[i, position].Count);
                if (countj<=0) continue;
                waterfallStats.Count += countj;
                waterfallStats.TotalTimeInMs += Interlocked.Read(ref localStats[i, position].TimeTicks);
                waterfallStats.MinTimeMs = Math.Min(waterfallStats.MinTimeMs,
                    Interlocked.Read(ref localStats[i, position].MinTimeTicks));
                waterfallStats.MaxTimeMs = Math.Max(waterfallStats.MaxTimeMs,
                    Interlocked.Read(ref localStats[i, position].MaxTimeTicks));
            }
            return waterfallStats;
#else
            return default(WaterfallStats);
#endif
        }

        /// <summary>
        /// Resets the stats and return the old stats.
        /// </summary>
        /// <returns>Old stats</returns>
        protected WaterfallStats[] Reset()
        {
#if WATERFALLSTATS_ON
            return ComputeStats(Interlocked.Exchange(ref _stats, GetInitializedStatsArray(_totalWork)), _totalWork);
#else
            return null;
#endif
        }

        private static WaterfallStats[] ComputeStats(WorkStats[,] stats, int totalWork)
        {
            var waterfallStats = new WaterfallStats[totalWork];
            for (var workIndex = 0; workIndex < totalWork; workIndex++)
            {
                waterfallStats[workIndex] = new WaterfallStats(double.MaxValue);
            }
            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                for (var j = 0; j < totalWork; j++)
                {
                    var countj = Interlocked.Read(ref stats[i, j].Count);
                    if (countj <= 0) continue;
                    waterfallStats[j].Count += countj;
                    waterfallStats[j].TotalTimeInMs += Interlocked.Read(ref stats[i, j].TimeTicks);
                    waterfallStats[j].MinTimeMs = Math.Min(waterfallStats[j].MinTimeMs,
                        Interlocked.Read(ref stats[i, j].MinTimeTicks));
                    waterfallStats[j].MaxTimeMs = Math.Max(waterfallStats[j].MaxTimeMs,
                        Interlocked.Read(ref stats[i, j].MaxTimeTicks));
                }
            }
            return waterfallStats;
        }

        /// <summary>
        /// Provides stats of the whole waterfall.
        /// </summary>
        /// <returns>Waterfall wide performace statistics</returns>
        protected WaterfallStats[] WaterfallStatistics()
        {
#if WATERFALLSTATS_ON
            return ComputeStats(_stats, _totalWork);
#else
            return null;
#endif
        }


        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct WorkStats
        {
            private fixed byte Pad0 [64];
            public long TimeTicks;
            public long Count;
            public long MaxTimeTicks;
            public long MinTimeTicks;
            private fixed byte Pad1 [64];

            public WorkStats(long minTime)
            {
                TimeTicks = Count = MaxTimeTicks = 0;
                MinTimeTicks = minTime;
            }
        }
    }
}