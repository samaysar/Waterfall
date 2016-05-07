using System;

namespace Waterfall.Base
{
    /// <summary>
    /// Enum to describe the type of error.
    /// </summary>
    public enum WaterfallErrorType
    {
        /// <summary>
        /// Unknown Error
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// WaterfallMapAttribute is missing
        /// </summary>
        MissingWaterfallMapAttributeAttribute,

        /// <summary>
        /// Type is missing in WaterfallMapAttribute/WaterfallWorkAttribute
        /// </summary>
        WaterfallAttributeSuppliedTypeIsNull,

        /// <summary>
        /// Type provided in WaterfallMapAttribute/WaterfallWorkAttribute is not correctly derived
        /// </summary>
        WaterfallWorkIsNotDerivedCorrectly,

        /// <summary>
        /// Map field is not int type
        /// </summary>
        MapMemberIsNotIntType,

        /// <summary>
        /// Map is neither class type (for int based waterfall) nor enum type (for enum based waterfall)
        /// </summary>
        UnknownTypeOfWaterfallMap,

        /// <summary>
        /// Map has known issue
        /// </summary>
        InvalidWaterfallMap,

        /// <summary>
        /// Waterfall work does not supply default parameter less ctor
        /// </summary>
        WaterfallWorkDoesNotDefineDefaultParameterLessCtor,

        /// <summary>
        /// More than 1 waterfall has same value associated to it (same enum/int)
        /// </summary>
        TwoOrMoreWaterfallWorksReturnsSameUniqueIdentifier,

        /// <summary>
        /// Waterservice either disposed or canceled
        /// </summary>
        WaterfallServiceDisposedOrCancellationDemanded,

        /// <summary>
        /// Enum based map is not inherited from int
        /// </summary>
        EnumNotInheritedFromInt,

        /// <summary>
        /// Enum map doesn't contain a member with associated int value as -1
        /// </summary>
        EnumDoesNotDefineEndOfWaterfallAsMinusOne,

        /// <summary>
        /// Enum values are not continuously increasing like -1,0,1,2,3...
        /// </summary>
        EnumValuesAreNotContinuouslyIncreasing,

        /// <summary>
        /// Thrown only when WATERFALL_DEBUG (conditional compilation symbol) is defined
        /// and when possible looped call is detected during waterfall execution (based on the policy that each waterfall work
        /// can execute only once for a given pair of input/result).
        /// </summary>
        WaterfallExceededMaxIterations,

        /// <summary>
        /// When a given waterfall work returns a value (enum/int) for which work is undefined by the map
        /// </summary>
        WaterfallNotDefined,

        /// <summary>
        /// When redundancy is identified during construction time
        /// </summary>
        RedundancyDetected
    }

    /// <summary>
    /// Waterfall world Wrapper of exception.
    /// </summary>
    public sealed class WaterfallException : Exception
    {
        internal WaterfallException(WaterfallErrorType errorType, string message, Exception inner = null)
            : base($"Reason:{errorType}. {message}", inner)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// Get associate errortype enum.
        /// </summary>
        public WaterfallErrorType ErrorType { get; private set; }
    }
}