using System;

namespace Waterfall.Types.EnumBased
{
    /// <summary>
    /// Class to be implemented by user, when using Enum based map to define waterfall, which converts the enum to integer value.
    /// </summary>
    /// <typeparam name="TEnum">Type of enum</typeparam>
    public abstract class EnumConverter<TEnum> where TEnum : struct, IConvertible, IFormattable, IComparable
    {
        /// <summary>
        /// Converts the enum value to equivalent integer value.
        /// </summary>
        /// <param name="val">Value to be converted.</param>
        /// <returns>Int equivalent</returns>
        public abstract int ToInt(TEnum val);
    }
}