using System;

namespace Waterfall.Libs.EnumBased
{
    /// <summary>
    /// Interface to be implemented by any enum map based waterfall-work.
    /// </summary>
    /// <typeparam name="TContext">Type of context dependency</typeparam>
    /// <typeparam name="TEnum">Type of enum return value</typeparam>
    /// <typeparam name="TInput">Type of input parameter</typeparam>
    /// <typeparam name="TResult">Type of output parameter</typeparam>
    public interface IWaterfallWork<in TContext, out TEnum, in TInput, in TResult>
        where TEnum : struct, IConvertible, IFormattable, IComparable where TResult : class
    {
        /// <summary>
        /// This method will be called by the waterfall during initialization (Ctor call) only once.
        /// </summary>
        /// <param name="dependencyContext">instance of the dependency context</param>
        void Init(TContext dependencyContext);

        /// <summary>
        /// Actual execution code.
        /// </summary>
        /// <param name="input">input instance</param>
        /// <param name="result">result instance</param>
        /// <returns>Map value of the next work to execute (Or negative value to end the execution)</returns>
        TEnum ExecuteWork(TInput input, TResult result);
    }
}