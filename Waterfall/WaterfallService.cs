using System;
using System.Threading;
using System.Threading.Tasks;
using Waterfall.Base;

namespace Waterfall
{
    /// <summary>
    /// A wrapper class to offer a solution-lifetime execution of waterfall-pipeline. Enables execution of
    /// waterfalls asynchronously on thread pool threads.
    /// </summary>
    /// <typeparam name="TInput">Type of Input parameter of the waterfall.</typeparam>
    /// <typeparam name="TResult">Type of Results parameter of the waterfall</typeparam>
    public sealed class WaterfallService<TInput, TResult> where TResult : class
    {
        private readonly IWaterfall<TInput, TResult> _waterfall;
        private readonly Action<TInput, TResult> _outcomeHandler;
        private readonly Action<TInput, TResult, Exception> _errorHandler;

        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="waterfall">Waterfall to execute. This can be enum/int based standalone waterfall OR 
        /// Chained/Concurrent waterfall.</param>
        /// <param name="outcomeHandler">Outcome handler</param>
        /// <param name="errorHandler">Error handler</param>
        public WaterfallService(IWaterfall<TInput, TResult> waterfall, Action<TInput, TResult> outcomeHandler,
            Action<TInput, TResult, Exception> errorHandler)
        {
            if (waterfall == null) throw new ArgumentNullException(nameof(waterfall));
            if (outcomeHandler == null) throw new ArgumentNullException(nameof(outcomeHandler));
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _waterfall = waterfall;
            _outcomeHandler = outcomeHandler;
            _errorHandler = errorHandler;
        }
        
        /// <summary>
        /// Executes the workflow work on the thread pool thread and passes post-computing input/results (errors)
        /// to the supplied actions methods.
        /// </summary>
        /// <param name="input">Input instance</param>
        /// <param name="result">Result instance</param>
        public void ExecuteParallel(TInput input, TResult result)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _waterfall.Execute(input, result);
                    _outcomeHandler(input, result);
                }
                catch (WaterfallException e)
                {
                    _errorHandler(input, result, e);
                }
                catch (Exception e)
                {
                    _errorHandler(input, result,
                        new WaterfallException(WaterfallErrorType.Unknown, "UnknownErrorDuringProcessing", e));
                }
            });
        }
    }
}