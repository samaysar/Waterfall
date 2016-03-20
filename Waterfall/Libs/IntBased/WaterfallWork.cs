using Waterfall.Libs.EnumBased;

namespace Waterfall.Libs.IntBased
{
    /// <summary>
    /// Abstract class to be implemented by any int map based waterfall-work.
    /// </summary>
    /// <typeparam name="TContext">Type of context dependency</typeparam>
    /// <typeparam name="TInput">Type of input parameter</typeparam>
    /// <typeparam name="TResult">Type of output parameter</typeparam>
    public abstract class WaterfallWork<TContext, TInput, TResult> : WaterfallWork<TContext, int, TInput, TResult>
        where TResult : class {}
}