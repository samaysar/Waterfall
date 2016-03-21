using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Waterfall.Base;

namespace Waterfall.Types.EnumBased
{
    /// <summary>
    /// Class which holds the collection of individual waterfall-work items and calls the waterfall-hierarchy
    /// right from the root until the last work item.
    /// <para>The waterfall hierarchy is defined by an enum which is decorated with WaterfallMapAttribute
    /// and enum members, except member with value -1, with WaterfallWorkAttribute</para>
    /// <para>Waterfall execution terminates when any work item returns a negative value.</para>
    /// </summary>
    /// <typeparam name="TEnumMap">Type of the enum which describes the map (waterfall hierarchy)</typeparam>
    /// <typeparam name="TDependency">Type of dependency context instance</typeparam>
    /// <typeparam name="TInput">Type of input instance</typeparam>
    /// <typeparam name="TResult">Type of result instance</typeparam>
    public sealed class Waterfall<TEnumMap, TDependency, TInput, TResult> : StatisticsHandler, IWaterfall<TInput, TResult>
        where TEnumMap : struct, IComparable, IFormattable, IConvertible
        where TResult : class
    {
        private readonly WaterfallWork<TDependency, TEnumMap, TInput, TResult> _waterFallRoot;
        private readonly EnumConverter<TEnumMap> _enumConvertor;
        private readonly WaterfallWork<TDependency, TEnumMap, TInput, TResult>[] _workBranches;

        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="dependencyContext">Dependency context instance</param>
        /// <param name="enumConvertor">Enum to int converter instance</param>
        public Waterfall(TDependency dependencyContext, EnumConverter<TEnumMap> enumConvertor)
        {
            if (enumConvertor == null) throw new ArgumentNullException(nameof(enumConvertor));
            _enumConvertor = enumConvertor;
            var arraySize = Validate(out _waterFallRoot);
            _waterFallRoot.Init(dependencyContext);
            _workBranches = new WaterfallWork<TDependency, TEnumMap, TInput, TResult>[arraySize];
            PopulateWorkBranches(_enumConvertor, _workBranches, dependencyContext);
            Init(arraySize + 1);
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
            var nextWorkIndex = _enumConvertor.ToInt(_waterFallRoot.ExecuteWork(input, result));
            sw.Stop();
            AddStats(0, sw.ElapsedTicks);
#else
            var nextWorkIndex = _enumConvertor.ToInt(_waterFallRoot.ExecuteWork(input, result));
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
                    nextWorkIndex = _enumConvertor.ToInt(_workBranches[prevIndex].ExecuteWork(input, result));
                    sw.Stop();
                    AddStats(prevIndex+1, sw.ElapsedTicks);
#else
                    nextWorkIndex = _enumConvertor.ToInt(_workBranches[nextWorkIndex].ExecuteWork(input, result));
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
                    $"Map ({typeof (TEnumMap)}) does not define work branch with UniqueIdentifier"+
                    $" value:{nextWorkIndex}");
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
            for (var index = 0; index < stats.Count; index++)
            {
                var indexedStats = stats[index];
                if (indexedStats.Count > 0)
                {
                    indexedStats.TotalTimeInMs = indexedStats.TotalTimeInMs / Stopwatch.Frequency * 1000;
                    indexedStats.MinTimeMs = indexedStats.MinTimeMs / Stopwatch.Frequency * 1000;
                    indexedStats.MaxTimeMs = indexedStats.MaxTimeMs / Stopwatch.Frequency * 1000;
                    indexedStats.AvgTimeInMs = indexedStats.TotalTimeInMs / indexedStats.Count;
                }
                else
                {
                    indexedStats.MaxTimeMs =
                        indexedStats.MinTimeMs = indexedStats.AvgTimeInMs = indexedStats.TotalTimeInMs = 0;
                }
                results.Add(index - 1, indexedStats);
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
            var stats = WorkStatistics(workuniqueUniqueIdentifier + 1);
            if (stats.Count > 0)
            {
                stats.TotalTimeInMs = stats.TotalTimeInMs / Stopwatch.Frequency * 1000;
                stats.MinTimeMs = stats.MinTimeMs / Stopwatch.Frequency * 1000;
                stats.MaxTimeMs = stats.MaxTimeMs / Stopwatch.Frequency * 1000;
                stats.AvgTimeInMs = stats.TotalTimeInMs / stats.Count;
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

        private static int Validate(out WaterfallWork<TDependency, TEnumMap, TInput, TResult> root)
        {
            var mapType = typeof(TEnumMap);
            if (!mapType.IsEnum)
                throw new WaterfallException(WaterfallErrorType.UnknownTypeOfWaterfallMap,
                    $"Enum expected as Map. Type:{mapType.FullName}");

            if (mapType.GetEnumUnderlyingType() != typeof(int))
                throw new WaterfallException(WaterfallErrorType.EnumNotInheritedFromInt,
                    $"Map ({mapType.FullName}) ActualUnderlyingType:{mapType.GetEnumUnderlyingType().FullName}.");

            var attribute = Attribute.GetCustomAttribute(mapType, typeof(WaterfallMapAttribute)) as WaterfallMapAttribute;
            if (attribute == null)
                throw new WaterfallException(WaterfallErrorType.MissingWaterfallMapAttributeAttribute,
                    $"Map ({mapType.FullName}) missing WaterfallAttribute.");

            if (attribute.RootType == null)
                throw new WaterfallException(WaterfallErrorType.WaterfallAttributeSuppliedTypeIsNull,
                    $"Null RootType. Map ({mapType.FullName}) on WaterfallAttribute.");

            if (!typeof(WaterfallWork<TDependency, TEnumMap, TInput, TResult>).IsAssignableFrom(attribute.RootType))
                throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                    $"RootType ({attribute.RootType.FullName}) not derived "+
                    $"from {typeof(WaterfallWork<TDependency, TEnumMap, TInput, TResult>).Name}.");

            if (attribute.RootType.GetConstructor(Type.EmptyTypes) == null)
                throw new WaterfallException(WaterfallErrorType.WaterfallWorkDoesNotDefineDefaultParameterLessCtor,
                    $"Default Ctor missing on {attribute.RootType.FullName}.");

            root =
                Activator.CreateInstance(attribute.RootType) as
                    WaterfallWork<TDependency, TEnumMap, TInput, TResult>;

            return ValidateWorkBranches(mapType, attribute.RootType);
        }

        private static int ValidateWorkBranches(Type mapType, Type rootType)
        {
            var castedIntArray = Enum.GetValues(mapType).Cast<int>();
            var intArray = castedIntArray as int[] ?? castedIntArray.ToArray();
            var minValue = intArray.Min();
            var maxValue = intArray.Max();

            if (minValue != -1)
                throw new WaterfallException(WaterfallErrorType.EnumDoesNotDefineEndOfWaterfallAsMinusOne,
                    $"Map ({mapType.FullName}) ActualMinValue:{minValue},EnumName"+
                    $":{Enum.GetName(typeof(TEnumMap), minValue)}.");
            if (intArray.Length != maxValue + 2)
                throw new WaterfallException(WaterfallErrorType.EnumValuesAreNotContinuouslyIncreasing,
                    $"Map ({mapType.FullName}) ExpectedEnumCount:{maxValue + 2},ActualEnumCount:{intArray.Length}");

            var fields =
                mapType.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Public|
                                  BindingFlags.NonPublic);
            var endOfWaterfallEnum = Enum.Parse(mapType, "-1");
            var typeSet = new HashSet<Type> {rootType};

            foreach (var fieldInfo in fields)
            {
                var attribute =
                    Attribute.GetCustomAttribute(fieldInfo, typeof(WaterfallWorkAttribute)) as WaterfallWorkAttribute;
                var enumVal = fieldInfo.GetValue(null);
                if (attribute == null)
                {
                    if (!enumVal.Equals(endOfWaterfallEnum))
                        throw new WaterfallException(WaterfallErrorType.InvalidWaterfallMap,
                            $"In Map ({mapType.FullName}) except -1 enum, all values must define"+
                            $" {typeof (WaterfallWorkAttribute).Name}. Attribute undefined for {enumVal}.");
                    continue;
                }
                if (enumVal.Equals(endOfWaterfallEnum))
                {
                    throw new WaterfallException(WaterfallErrorType.InvalidWaterfallMap,
                        $"In Map ({mapType.FullName}) enum with value -1 defines"+
                        $" {typeof (WaterfallWorkAttribute).Name}. However, no work should be defined for this value.");
                }
                if (attribute.WorkType == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallAttributeSuppliedTypeIsNull,
                        $"Null WorkType. Map ({mapType.FullName}) on WaterfallWorkAttribute on Field ({fieldInfo.Name}).");

                if (
                    !typeof (WaterfallWork<TDependency, TEnumMap, TInput, TResult>).IsAssignableFrom(
                        attribute.WorkType))
                {
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                        $"WorkType ({attribute.WorkType.FullName}) not derived "+
                        $"from {typeof (WaterfallWork<TDependency, TEnumMap, TInput, TResult>).FullName}.");
                }
                if (!typeSet.Add(attribute.WorkType))
                {
                    throw new WaterfallException(WaterfallErrorType.RedundancyDetected,
                        $"WorkType ({attribute.WorkType.FullName}) defined on at least 2 enum members.");
                }
            }
            return maxValue + 1;
        }

        private static void PopulateWorkBranches(EnumConverter<TEnumMap> enumConvertor,
            IList<WaterfallWork<TDependency, TEnumMap, TInput, TResult>> workBranches,
            TDependency dependency)
        {
            if (workBranches.Count == 0) return;
            var mapType = typeof(TEnumMap);

            foreach (
                var fieldInfo in mapType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public|BindingFlags.NonPublic))
            {
                var attribute =
                    Attribute.GetCustomAttribute(fieldInfo, typeof(WaterfallWorkAttribute)) as WaterfallWorkAttribute;
                if (attribute == null) continue;

                if (attribute.WorkType.GetConstructor(Type.EmptyTypes) == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkDoesNotDefineDefaultParameterLessCtor,
                        $"Default Ctor missing ({attribute.WorkType.FullName}) assocaited to Map"+
                        $" ({mapType.FullName}) for field ({fieldInfo.Name}).");

                var instance =
                    Activator.CreateInstance(attribute.WorkType) as WaterfallWork<TDependency, TEnumMap, TInput, TResult>;

                if (instance == null)
                    throw new WaterfallException(WaterfallErrorType.WaterfallWorkIsNotDerivedCorrectly,
                        $"WorkType ({attribute.WorkType.FullName}) not derived "+
                        $"from {typeof(WaterfallWork<TDependency, TEnumMap, TInput, TResult>).FullName}.");

                instance.Init(dependency);
                var instanceIndex = enumConvertor.ToInt((TEnumMap)fieldInfo.GetValue(null));
                if (workBranches[instanceIndex] == null)
                {
                    workBranches[instanceIndex] = instance;
                }
                else
                {
                    throw new WaterfallException(WaterfallErrorType.TwoOrMoreWaterfallWorksReturnsSameUniqueIdentifier,
                        $"Work ({attribute.WorkType.FullName}) identifier ({fieldInfo.GetValue(null)}) is "+
                        $"same as work ({workBranches[instanceIndex].GetType().FullName}).");
                }
            }
        }
    }
}