using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QRBuild.ProjectSystem
{
    public abstract class BuildVariant
    {
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            IList<ElementInfo> elements = GetElementInfo();
            for (int i = 0; i < Elements.Count; i++) {
                ElementInfo elementInfo = Elements[i];
                string value = elementInfo.GetValue(this);
                builder.Append(value);
                if (i < Elements.Count - 1) {
                    builder.Append(".");
                }
            }
            string result = builder.ToString();
            return result;
        }

        public string GetVariantStringFormat()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < Elements.Count; i++) {
                ElementInfo elementInfo = Elements[i];
                builder.Append(elementInfo.Name);
                if (i < Elements.Count - 1) {
                    builder.Append(".");
                }
            }
            string result = builder.ToString();
            return result;
        }

        // TODO: delete this, probably make ElementInfo public instead
        public string GetVariantStringOptions()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < Elements.Count; i++) {
                ElementInfo elementInfo = Elements[i];
                builder.AppendFormat("{0}: ", elementInfo.Name);
                if (elementInfo.ElementType.IsEnum) {
                    // print enum elements
                    builder.Append(" {");
                    string[] names = Enum.GetNames(elementInfo.ElementType);
                    for (int j = 0; j < names.Length; j++) {
                        builder.Append(names[j]);
                        if (j < names.Length - 1) {
                            builder.Append(",");
                        }
                    }
                    builder.Append("}");
                }
                else {
                    // print out the type
                    builder.Append(elementInfo.ElementType.Name);
                }
                builder.Append("\n");
            }
            string result = builder.ToString();
            return result;
        }

        public void FromString(string variantString)
        {
            string trimmedValue = variantString.Trim();
            string[] values = trimmedValue.Split('.');
            int count = Math.Min(values.Length, Elements.Count);
            for (int i = 0; i < count; i++) {
                string value = values[i];
                if (!String.IsNullOrEmpty(value)) {
                    Elements[i].SetValue(this, value);
                }
            }
        }

        private class ElementInfo : IComparable<ElementInfo>, IComparable
        {
            public ElementInfo(PropertyInfo info, int weight)
            {
                Weight = weight;
                m_pinfo = info;
                m_finfo = null;
            }
            public ElementInfo(FieldInfo info, int weight)
            {
                Weight = weight;
                m_pinfo = null;
                m_finfo = info;
            }

            public string Name
            {
                get
                {
                    if (m_pinfo != null) {
                        return m_pinfo.Name;
                    }
                    if (m_finfo != null) {
                        return m_finfo.Name;
                    }
                    throw new InvalidOperationException();
                }
            }

            public readonly int Weight;

            public Type ElementType
            {
                get
                {
                    if (m_pinfo != null) {
                        return m_pinfo.PropertyType;
                    }
                    if (m_finfo != null) {
                        return m_finfo.FieldType;
                    }
                    throw new InvalidOperationException();
                }
            }

            public string GetValue(object variant)
            {
                if (m_pinfo != null) {
                    object valueObject = m_pinfo.GetValue(variant, null);
                    string value = valueObject.ToString();
                    return value;
                }
                if (m_finfo != null) {
                    object valueObject = m_finfo.GetValue(variant);
                    string value = valueObject.ToString();
                    return value;
                }
                throw new InvalidOperationException();
            }

            public void SetValue(object variant, string value)
            {
                if (m_pinfo != null) {
                    m_pinfo.SetValue(variant, value, null);
                }
                else if (m_finfo != null) {
                    if (m_finfo.FieldType == typeof(string)) {
                        m_finfo.SetValue(variant, value);
                    }
                    else if (m_finfo.FieldType.IsEnum) {
                        object enumValue = Enum.Parse(m_finfo.FieldType, value);
                        m_finfo.SetValue(variant, enumValue);
                    }
                    else {
                        throw new InvalidOperationException();
                    }
                }
                else {
                    throw new InvalidOperationException();
                }
            }

            public int CompareTo(ElementInfo rhs)
            {
                return Weight - rhs.Weight;
            }
            int IComparable.CompareTo(object rhs)
            {
                return CompareTo((ElementInfo)rhs);
            }

            private readonly PropertyInfo m_pinfo;
            private readonly FieldInfo m_finfo;
        }

        private IList<ElementInfo> GetElementInfo()
        {
            List<ElementInfo> elements = new List<ElementInfo>();
            Type type = GetType();

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.GetProperty |
                BindingFlags.Instance);
            foreach (var fieldInfo in fields) {
                object[] variantParts = fieldInfo.GetCustomAttributes(typeof(VariantPartAttribute), false);
                if (variantParts.Length == 1) {
                    var vpa = variantParts[0] as VariantPartAttribute;
                    ElementInfo elementInfo = new ElementInfo(fieldInfo, vpa.Weight);
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
                    ElementInfo elementInfo = new ElementInfo(propertyInfo, vpa.Weight);
                    elements.Add(elementInfo);
                }
            }

            elements.Sort();
            return elements;
        }

        private IList<ElementInfo> Elements
        {
            get
            {
                if (m_elements == null) {
                    m_elements = GetElementInfo();
                }
                return m_elements;
            }
        }
        private IList<ElementInfo> m_elements;
    }
}
