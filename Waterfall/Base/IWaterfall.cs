namespace Waterfall.Base
{
    /// <summary>
    /// Interface implemented by classes which executes the actual waterfall-work.
    /// </summary>
    /// <typeparam name="TInput">Type of Input parameter of the waterfall</typeparam>
    /// <typeparam name="TResult">Type of Result parameter of the waterfall</typeparam>
    public interface IWaterfall<in TInput, in TResult> where TResult : class
    {
        /// <summary>
        /// Method to process the waterfall work items
        /// </summary>
        /// <param name="input">Input instance</param>
        /// <param name="result">Result instance</param>
        void Execute(TInput input, TResult result);
    }
}