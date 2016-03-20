using System;
using System.Threading;
using System.Threading.Tasks;
using Waterfall.Base;

namespace Waterfall
{
    /// <summary>
    /// A wrapper class is to prepare a solution wide lifetime service which executes
    /// waterfalls asynchronously or on thread pool threads.
    /// </summary>
    /// <typeparam name="TInput">Type of Input parameter of the waterfall.</typeparam>
    /// <typeparam name="TResult">Type of Results parameter of the waterfall</typeparam>
    public sealed class WaterfallService<TInput, TResult> where TResult : class
    {
        private readonly IWaterfall<TInput, TResult> _waterfall;

        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="waterfall">Waterfall to execute. This can be enum/int based standalone waterfall OR 
        /// Chained/Concurrent waterfall.</param>
        public WaterfallService(IWaterfall<TInput, TResult> waterfall)
        {
            if (waterfall == null) throw new ArgumentNullException(nameof(waterfall));
            _waterfall = waterfall;
        }

        /// <summary>
        /// Executes the workflow work as a task. Thus, you can use async-await feature.
        /// </summary>
        /// <param name="input">Input instance</param>
        /// <param name="result">Result instance</param>
        public Task ExecuteAsync(TInput input, TResult result)
        {
            return Task.Factory.StartNew(() => _waterfall.Execute(input, result));
        }

        /// <summary>
        /// Executes the workflow work on the thread pool thread and passes post-computing input/results (errors)
        /// to the supplied actions.
        /// </summary>
        /// <param name="input">Input instance</param>
        /// <param name="result">Result instance</param>
        /// <param name="outcomeHandler">Outcome handler</param>
        /// <param name="errorHandler">Error handler</param>
        public void ExecuteParallel(TInput input, TResult result, Action<TInput, TResult> outcomeHandler,
            Action<TInput, TResult, Exception> errorHandler)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _waterfall.Execute(input, result);
                    outcomeHandler(input, result);
                }
                catch (WaterfallException e)
                {
                    errorHandler(input, result, e);
                }
                catch (Exception e)
                {
                    errorHandler(input, result,
                        new WaterfallException(WaterfallErrorType.Unknown, "UnknownErrorDuringProcessing", e));
                }
            });
        }
    }
}