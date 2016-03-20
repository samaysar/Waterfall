using System;
using System.Threading.Tasks;
using Waterfall.Base;

namespace Waterfall.Builders
{
    /// <summary>
    /// Class which prepare a chain of waterfalls to be executed in parallel.
    /// <para>The work execution is NOT thread safe. Any number of waterfall-work can be executed in parallel
    /// from any waterfall in the chain. All resources that are used by waterfall-work
    /// must implement thread safety.</para>
    /// <para>These waterfalls can be enum/int based standalone waterfall OR 
    /// Chained/Concurrent waterfalls.</para>
    /// </summary>
    /// <typeparam name="TInput">Type of input parameter of waterfalls.</typeparam>
    /// <typeparam name="TResult">Type of result parameter of waterfalls.</typeparam>
    public sealed class ConcurrentWaterfall<TInput, TResult> : IWaterfall<TInput, TResult>
        where TResult : class
    {
        private readonly IWaterfall<TInput, TResult>[] _waterfall;

        private ConcurrentWaterfall(ConcurrentWaterfall<TInput, TResult> current,
            IWaterfall<TInput, TResult> nextWaterfall)
        {
            if (nextWaterfall == null) throw new ArgumentNullException(nameof(nextWaterfall));
            _waterfall = new IWaterfall<TInput, TResult>[current._waterfall.Length+1];
            for (var index = 0; index<current._waterfall.Length; index++)
            {
                _waterfall[index] = current._waterfall[index];
            }
            _waterfall[current._waterfall.Length] = nextWaterfall;
        }

        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="firstWaterfall">Instance of waterfall</param>
        /// <param name="anotherWaterfall">Another instance of waterfall</param>
        public ConcurrentWaterfall(IWaterfall<TInput, TResult> firstWaterfall,
            IWaterfall<TInput, TResult> anotherWaterfall)
        {
            if (firstWaterfall == null) throw new ArgumentNullException(nameof(firstWaterfall));
            if (anotherWaterfall == null) throw new ArgumentNullException(nameof(anotherWaterfall));
            _waterfall = new IWaterfall<TInput, TResult>[2];
            _waterfall[0] = firstWaterfall;
            _waterfall[1] = anotherWaterfall;
        }

        /// <summary>
        /// Adds new waterfall in the collection to be executed in parallel
        /// </summary>
        /// <param name="anotherWaterfall">Waterfall to be added in the collection for parallel processing</param>
        /// <returns>New instance of the class</returns>
        public ConcurrentWaterfall<TInput, TResult> Add(IWaterfall<TInput, TResult> anotherWaterfall)
        {
            return new ConcurrentWaterfall<TInput, TResult>(this, anotherWaterfall);
        }

        /// <summary>
        /// Executes the waterfall collection in parallel order on the given pair of input/result.
        /// </summary>
        /// <param name="input">input instance</param>
        /// <param name="result">result instance</param>
        public void Execute(TInput input, TResult result)
        {
            Parallel.ForEach(_waterfall, current => current.Execute(input, result));
        }
    }
}