using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Waterfall.Base;

namespace Waterfall.Types.IntBased
{
    /// <summary>
    /// Class which holds the collection of individual waterfall-work items and calls the waterfall-hierarchy
    /// right from the root until the last work item.
    /// <para>The waterfall hierarchy is defined by a class which is decorated with WaterfallMapAttribute
    /// and its const int fields, except member with negative values, with WaterfallWorkAttribute</para>
    /// <para>Waterfall execution terminates when any work item returns a negative value.</para>
    /// </summary>
    /// <typeparam name="TIntMap">Type of the class (having const int fields) which describes the map (waterfall hierarchy)</typeparam>
    /// <typeparam name="TDependency">Type of dependency context instance</typeparam>
    /// <typeparam name="TInput">Type of input instance</typeparam>
    /// <typeparam name="TResult">Type of result instance</typeparam>
    public sealed class Waterfall<TIntMap, TDependency, TInput, TResult> : StatisticsHandler,
        IWaterfall<TInput, TResult> where TIntMap : class
        where TResult : class
    {
        private readonly WaterfallWork<TDependency, TInput, TResult> _waterFallRoot;
        private readonly WaterfallWork<TDependency, TInput, TResult>[] _workBranches;

        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="dependencyContext">Dependency context instance</param>
        public Waterfall(TDependency dependencyContext)
        {
            var arraySize = Validate(out _waterFallRoot);
            _waterFallRoot.Init(dependencyContext);
            _workBranches = new WaterfallWork<TDependency, TInput, TResult>[arraySize];
            PopulateWorkBranches(_workBranches, dependencyContext);
            Init(arraySize+2);
        }

        /// <summary>
        /// Execute the waterfall hierarchy on the given pair of input/result.
        /// <para>Waterfall execution terminates when any work item returns a negative value.</para>
        /// </summary>
        /// <param name="input">Input instance</param>
        /// <param name="result">Result instance</param>
        public void Execute(TInput input, TResult result)
        {
#if WATERFALL_DEBUG
            var loopCount = 0;
            var workNameQueue = new Queue<string>();
            workNameQueue.Enqueue(_waterFallRoot.GetType().FullName);
#endif

#if WATERFALLSTATS_ON
            var sw = Stopwatch.StartNew();
            var nextWorkIndex = _waterFallRoot.ExecuteWork(input, result);
            sw.Stop();
            AddStats(1, sw.ElapsedTicks);
#else
            var nextWorkIndex = _waterFallRoot.ExecuteWork(input, result);
#endif
            try
            {
                while (nextWorkIndex>=0)
                {
#if WATERFALL_DEBUG
                    workNameQueue.Enqueue(_workBranches[nextWorkIndex].GetType().FullName);
#endif
#if WATERFALLSTATS_ON
                    sw.Restart();
                    var prevIndex = nextWorkIndex;
                    nextWorkIndex = _workBranches[prevIndex].ExecuteWork(input, result);
                    sw.Stop();
                    AddStats(prevIndex+2, sw.ElapsedTicks);
#else
                    nextWorkIndex = _workBranches[nextWorkIndex].ExecuteWork(input, result);
#endif
#if WATERFALL_DEBUG
                    if (++loopCount>_workBranches.Length)
                    {
                        throw new WaterfallException(WaterfallErrorType.WaterfallExceededMaxIterations,
                            $"WaterfallTrace:{workNameQueue.Aggregate(string.Empty, (x, y) => x+Environment.NewLine+y)}");
                    }
#endif
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new WaterfallException(WaterfallErrorType.WaterfallNotDefined,
                    $"Map ({typeof (TIntMap)}) does not define work branch with UniqueIdentifier "+
                    $"value:{nextWorkIndex}");
            }
            catch (WaterfallException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new WaterfallException(WaterfallErrorType.Unknown, "UnknownError", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, WaterfallStats> ResetStatistics()
        {
#if WATERFALLSTATS_ON
            return Statistics(Reset());
#else
            return null;
#endif
        }

        private static Dictionary<int, WaterfallStats> Statistics(IList<WaterfallStats> stats)
        {
            var results = new Dictionary<int, WaterfallStats>();
            for (var index = 1; index < stats.Count; index++)
            {
                var indexedStats = stats[index];
                if (indexedStats.Count > 0)
                {
                    indexedStats.TotalTimeInMs = indexedStats.TotalTimeInMs/Stopwatch.Frequency*1000;
                    indexedStats.MinTimeMs = indexedStats.MinTimeMs / Stopwatch.Frequency * 1000;
                    indexedStats.MaxTimeMs = indexedStats.MaxTimeMs / Stopwatch.Frequency * 1000;
                    indexedStats.AvgTimeInMs = indexedStats.TotalTimeInMs / indexedStats.Count;
                }
                else
                {
                    indexedStats.MaxTimeMs =
                        indexedStats.MinTimeMs = indexedStats.AvgTimeInMs = indexedStats.TotalTimeInMs = 0;
                }
                results.Add(index - 2, indexedStats);
            }
            return results;
        }

        /// <summary>
        /// Provide the performance stats of the waterfall (all work items including root).
        /// <para>Available only when WATERFALLSTATS_ON conditional compilation symbol is present.</para>
        /// <para>This call returns null when WATERFALLSTATS_ON is not defined.</para>
        /// <para>Root statistics are keyed by -1, other stats are keyed by their enum equivalent.</para>
        /// </summary>
        /// <returns>Dictionary instance.</returns>
        public Dictionary<int, WaterfallStats> WaterfallFullStatistics()
        {
#if WATERFALLSTATS_ON
            return Statistics(WaterfallStatistics());
#else
            return null;
#endif
        }

        /// <summary>
        /// Provide the performance stats of a given waterfall-work.
        /// <para>Available only when WATERFALLSTATS_ON conditional compilation symbol is present.</para>
        /// <para>This call returns default value when WATERFALLSTATS_ON is not defined.</para>
        /// <para>For Root statistics provide -1, other stats are keyed by their int (enum to int) equivalent.</para>
        /// </summary>
        /// <param name="workuniqueUniqueIdentifier"></param>
        /// <returns>Stats instance</returns>
        public WaterfallStats WaterfallWorkStats(int workuniqueUniqueIdentifier)
        {
            if (workuniqueUniqueIdentifier < -1 || workuniqueUniqueIdentifier >= _workBranches.Length)
            {
                throw new ArgumentException(
                    $"{workuniqueUniqueIdentifier} is invalid. Provide values between -1 and {_workBranches.Length - 1}");
            }
#if WATERFALLSTATS_ON
            var stats = WorkStatistics(workuniqueUniqueIdentifier+2);
            if (stats.Count>0)
            {
                stats.TotalTimeInMs = stats.TotalTimeInMs/Stopwatch.Frequency*1000;
                stats.MinTimeMs = stats.MinTimeMs/Stopwatch.Frequency*1000;
                stats.MaxTimeMs = stats.MaxTimeMs/Stopwatch.Frequency*1000;
                stats.AvgTimeInMs = stats.TotalTimeInMs/stats.Count;
            }
            else
            {
                stats.MaxTimeMs = stats.MinTimeMs = stats.AvgTimeInMs = stats.TotalTimeInMs = 0;
            }
            return stats;
#else
            return default(WaterfallStats);
#endif
        }

        private static int Validate(out WaterfallWork<TDependency, TInput, TResult> root)
        {
            var mapType = typeof (TIntMap);
            if (!mapType.IsClass)
                throw new WaterfallException(WaterfallErrorType.UnknownTypeOfWaterfallMap,
                    $"Class expected as Map. Type:{mapType.FullName}");

            var attribute =
                Attribute.GetCustomAttribute(mapType, typeof (WaterfallMapAttribute)) as WaterfallMapAttribute;
            if (attribute == null)

                throw new WaterfallException(WaterfallErrorType.MissingWaterfallMapAttributeAttribute,
                    $"Map ({mapType.FullName}) missing WaterfallAttribute.");

            if (attribute.RootType == null)
                throw new WaterfallException(WaterfallErrorType.WaterfallAttributeSuppliedTypeIsNull,
                    $"Null RootType. Map ({mapType.FullName}) on WaterfallAttribute.");

            if (!typeof (WaterfallWork<TDependency, TInput, TResult>).IsAssignableFrom(attribute.RootType))
                throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                    $"RootType ({attribute.RootType.FullName}) not derived "+
                    $"from {typeof (WaterfallWork<TDependency, TInput, TResult>).Name}.");

            if (attribute.RootType.GetConstructor(Type.EmptyTypes) == null)
                throw new WaterfallException(WaterfallErrorType.WaterfallWorkDoesNotDefineDefaultParameterLessCtor,
                    $"Default Ctor missing on {attribute.RootType.FullName}.");

            root = Activator.CreateInstance(attribute.RootType) as WaterfallWork<TDependency, TInput, TResult>;

            return ValidateWorkBranches(mapType, attribute.RootType);
        }

        private static int ValidateWorkBranches(Type mapType, Type rootType)
        {
            var fields =
                mapType.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Public|
                                  BindingFlags.NonPublic);
            if (fields.Length == 0) return 0;
            var min = 0;
            var max = 0;
            var count = 0;

            var typeSet = new HashSet<Type> { rootType };
            foreach (var fieldInfo in fields)
            {
                var attribute =
                    Attribute.GetCustomAttribute(fieldInfo, typeof (WaterfallWorkAttribute)) as WaterfallWorkAttribute;

                if (attribute == null) continue;

                if (fieldInfo.FieldType != typeof (int))
                    throw new WaterfallException(WaterfallErrorType.MapMemberIsNotIntType,
                        $"Map ({mapType.FullName}) member ({fieldInfo.Name}) does not return int value.");
                if (attribute.WorkType == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallAttributeSuppliedTypeIsNull,
                        $"Null BranchType. Map ({mapType.FullName}) on WaterfallWorkAttribute on "+
                        $"Field ({fieldInfo.Name}).");

                if (!typeof (WaterfallWork<TDependency, TInput, TResult>).IsAssignableFrom(attribute.WorkType))
                {
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                        $"BranchType ({attribute.WorkType.FullName}) not derived "+
                        $"from {typeof (WaterfallWork<TDependency, TInput, TResult>).FullName}.");
                }
                if (!typeSet.Add(attribute.WorkType))
                {
                    throw new WaterfallException(WaterfallErrorType.RedundancyDetected,
                        $"WorkType ({attribute.WorkType.FullName}) defined on at least 2 map fields.");
                }

                var fieldValue = (int)fieldInfo.GetValue(null);
                min = Math.Min(min, fieldValue);
                max = Math.Max(max, fieldValue);
                count++;
            }

            if (min<0)
                throw new WaterfallException(WaterfallErrorType.InvalidWaterfallMap,
                    $"Map ({mapType.FullName}) defines Work with negative values (Value:{min}).");
            if (min != 0)
                throw new WaterfallException(WaterfallErrorType.InvalidWaterfallMap,
                    $"Map ({mapType.FullName}) does not define value 0.");
            if (max != count-1)
                throw new WaterfallException(WaterfallErrorType.InvalidWaterfallMap,
                    $"Map ({mapType.FullName}) must use continuous integer values (starting at 0). Expected max "+
                    $"value:{count-1}, Actual max value:{max}.");

            return count;
        }

        private static void PopulateWorkBranches(IList<WaterfallWork<TDependency, TInput, TResult>> workBranches,
            TDependency dependency)
        {
            if (workBranches.Count == 0) return;
            var mapType = typeof (TIntMap);

            foreach (
                var fieldInfo in mapType.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
            {
                var attribute =
                    Attribute.GetCustomAttribute(fieldInfo, typeof (WaterfallWorkAttribute)) as WaterfallWorkAttribute;
                if (attribute == null) continue;

                if (attribute.WorkType.GetConstructor(Type.EmptyTypes) == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkDoesNotDefineDefaultParameterLessCtor,
                        $"Default Ctor missing ({attribute.WorkType.FullName}) assocaited to "+
                        $"Map ({mapType.FullName}) for field ({fieldInfo.Name}).");

                var instance =
                    Activator.CreateInstance(attribute.WorkType) as WaterfallWork<TDependency, TInput, TResult>;

                if (instance == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                        $"BranchType ({attribute.WorkType.FullName}) not derived "+
                        $"from {typeof (WaterfallWork<TDependency, TInput, TResult>).FullName}.");

                instance.Init(dependency);
                var position = (int)fieldInfo.GetValue(null);
                if (workBranches[position] == null)
                {
                    workBranches[position] = instance;
                }
                else
                {
                    throw new WaterfallException(
                        WaterfallErrorType.TwoOrMoreWaterfallWorksReturnsSameUniqueIdentifier,
                        $"Work ({attribute.WorkType.FullName}) identifier ({position}) is same"+
                        $" as work ({workBranches[position].GetType().FullName}).");
                }
            }
        }
    }
}