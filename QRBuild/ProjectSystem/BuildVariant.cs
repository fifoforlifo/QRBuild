using System;
using System.Collections.Generic;
using System.Reflection;

namespace QRBuild.ProjectSystem
{
    public abstract class BuildVariant
        : IComparable<BuildVariant>
        , IComparable
    {
        private static Dictionary<Type, List<BuildVariantElementInfo>> s_elementInfo =
            new Dictionary<Type, List<BuildVariantElementInfo>>();

        /// Returns a list of ElementInfo, which provides convenient
        /// access to all variant properties and fields.
        public IList<BuildVariantElementInfo> Elements
        {
            get
            {
                lock (s_elementInfo) {
                    Type type = GetType();
                    List<BuildVariantElementInfo> elements;
                    bool found = s_elementInfo.TryGetValue(type, out elements);
                    if (!found) {
                        elements = CreateElementInfo(type);
                        s_elementInfo[type] = elements;
                    }
                    return elements;
                }
            }
        }

        /// Default implementation of BuildVariant.ToString() returns the VariantString.
        /// This can of course be overridden in derived classes.
        public override string ToString()
        {
            string result = this.ToVariantString();
            return result;
        }

        public virtual void FromString(string variantString)
        {
            this.FromVariantString(variantString);
        }

        private static List<BuildVariantElementInfo> CreateElementInfo(Type type)
        {
            List<BuildVariantElementInfo> elements = new List<BuildVariantElementInfo>();

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.GetProperty |
                BindingFlags.Instance);
            foreach (var fieldInfo in fields) {
                object[] variantParts = fieldInfo.GetCustomAttributes(typeof(VariantPartAttribute), false);
                if (variantParts.Length == 1) {
                    var vpa = variantParts[0] as VariantPartAttribute;
                    BuildVariantElementInfo elementInfo = new BuildVariantElementInfo(fieldInfo, vpa.Weight);
                    elements.Add(elementInfo);
                }
            }

            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.GetProperty |
                BindingFlags.Instance);
            foreach (var propertyInfo in properties) {
                object[] variantParts = propertyInfo.GetCustomAttributes(typeof(VariantPartAttribute), false);
                if (variantParts.Length == 1) {
                    var vpa = variantParts[0] as VariantPartAttribute;
                    BuildVariantElementInfo elementInfo = new BuildVariantElementInfo(propertyInfo, vpa.Weight);
                    elements.Add(elementInfo);
                }
            }

            elements.Sort();
            return elements;
        }

        /// Returns a strictly ordered comparison of each element's string value.
        public int CompareTo(BuildVariant rhs)
        {
            foreach (BuildVariantElementInfo element in Elements) {
                string lhsValue = element.GetValue(this);
                string rhsValue = element.GetValue(rhs);
                int diff = lhsValue.CompareTo(rhsValue);
                if (diff != 0) {
                    return diff;
                }
            }
            return 0;
        }
        int IComparable.CompareTo(object rhs)
        {
            return CompareTo((BuildVariant)rhs);
        }
    }
}
