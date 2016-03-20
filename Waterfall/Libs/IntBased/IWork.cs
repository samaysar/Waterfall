using Waterfall.Libs.EnumBased;

namespace Waterfall.Libs.IntBased
{
    /// <summary>
    /// Interface to be implemented by any int map based waterfall-work.
    /// </summary>
    /// <typeparam name="TContext">Type of context dependency</typeparam>
    /// <typeparam name="TInput">Type of input parameter</typeparam>
    /// <typeparam name="TResult">Type of output parameter</typeparam>
    public interface IWaterfallWork<in TContext, in TInput, in TResult> : IWaterfallWork<TContext, int, TInput, TResult>
        where TResult : class {}
}