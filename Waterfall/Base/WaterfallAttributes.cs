using System;

namespace Waterfall.Base
{
    /// <summary>
    /// Attribute to be place on the top of Enum (for enum waterfall map) OR
    /// Class (for int waterfall map).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false)]
    public sealed class WaterfallMapAttribute : Attribute
    {
        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="waterfallRootType">Type of the waterfall root.</param>
        public WaterfallMapAttribute(Type waterfallRootType)
        {
            RootType = waterfallRootType;
        }

        /// <summary>
        /// Get the type of waterfall root.
        /// </summary>
        public Type RootType { get; private set; }
    }

    /// <summary>
    /// Attribute to place on the enum members (for enum waterfall map) OR
    /// Class const int type fields (for int waterfall map).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class WaterfallWorkAttribute : Attribute
    {
        /// <summary>
        /// Default Ctor.
        /// </summary>
        /// <param name="waterfallWorkType">Type of the waterfall work.</param>
        public WaterfallWorkAttribute(Type waterfallWorkType)
        {
            WorkType = waterfallWorkType;
        }

        /// <summary>
        /// Get the type of waterfall work.
        /// </summary>
        public Type WorkType { get; private set; }
    }
}