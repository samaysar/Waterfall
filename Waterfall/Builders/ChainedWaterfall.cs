using System;
using Waterfall.Base;

namespace Waterfall.Builders
{
    /// <summary>
    /// Class which prepare a chain of waterfalls to be executed in serial order.
    /// <para>These waterfalls can be enum/int based standalone waterfall OR 
    /// Chained/Concurrent waterfalls.</para>
    /// </summary>
    /// <typeparam name="TInput">Type of input parameter of waterfalls.</typeparam>
    /// <typeparam name="TResult">Type of result parameter of waterfalls.</typeparam>
    public sealed class ChainedWaterfall<TInput, TResult> : IWaterfall<TInput, TResult>
        where TResult : class
    {
        private readonly IWaterfall<TInput, TResult>[] _waterfall;

        private ChainedWaterfall(ChainedWaterfall<TInput, TResult> current,
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
        /// <param name="waterfallHead">First link in the waterfall chain, i.e. execution will start at this waterfall.</param>
        /// <param name="nextWaterfall">Next waterfall in the chain.</param>
        public ChainedWaterfall(IWaterfall<TInput, TResult> waterfallHead,
            IWaterfall<TInput, TResult> nextWaterfall)
        {
            if (waterfallHead == null) throw new ArgumentNullException(nameof(waterfallHead));
            if (nextWaterfall == null) throw new ArgumentNullException(nameof(nextWaterfall));
            _waterfall = new IWaterfall<TInput, TResult>[2];
            _waterfall[0] = waterfallHead;
            _waterfall[1] = nextWaterfall;
        }

        /// <summary>
        /// Extends the chain by adding the given waterfall at the end of the existing chain.
        /// </summary>
        /// <param name="nextWaterfall">Waterfall to be added at the end of processing chain.</param>
        /// <returns>New instance of the class</returns>
        public ChainedWaterfall<TInput, TResult> Add(IWaterfall<TInput, TResult> nextWaterfall)
        {
            return new ChainedWaterfall<TInput, TResult>(this, nextWaterfall);
        }

        /// <summary>
        /// Executes the whole waterfall chain in serial order on the given pair of input/result.
        /// </summary>
        /// <param name="input">input instance</param>
        /// <param name="result">result instance</param>
        public void Execute(TInput input, TResult result)
        {
            foreach (var current in _waterfall)
            {
                current.Execute(input, result);
            }
        }
    }
}