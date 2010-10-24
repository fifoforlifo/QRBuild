using System;

namespace QRBuild.ProjectSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false)]
    public sealed class VariantPartAttribute : Attribute
    {
        /// Designate a field as part of a BuildVariant.
        /// The weight specifies its relative position in the variant string.
        /// Lower weights appear earlier in the variant string.
        public VariantPartAttribute(int weight)
        {
            Weight = weight;
        }

        public int Weight { get; private set; }
    }
}
