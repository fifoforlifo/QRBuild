using System;

namespace QRBuild.ProjectSystem
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PrimaryProjectAttribute : Attribute
    {
        public PrimaryProjectAttribute(Type variantType)
        {
            if (!InheritsFrom(variantType, typeof(BuildVariant))) {
                throw new ArgumentException("VariantType parameter must inherit from BuildVariant", "variantType");
            }
            VariantType = variantType;
        }

        public readonly Type VariantType;

        private bool InheritsFrom(Type type, Type baseType)
        {
            for (Type current = type; current != null; current = current.BaseType) {
                if (current == baseType) {
                    return true;
                }
            }
            return false;
        }
    }
}
