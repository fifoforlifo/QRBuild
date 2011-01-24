using System;
using System.Reflection;

namespace QRBuild.ProjectSystem
{
    public class BuildVariantElementInfo 
        : IComparable<BuildVariantElementInfo>
        , IComparable
    {
        internal BuildVariantElementInfo(PropertyInfo info, int weight)
        {
            Weight = weight;
            m_pinfo = info;
            m_finfo = null;
        }
        internal BuildVariantElementInfo(FieldInfo info, int weight)
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

        public string GetValue(BuildVariant variant)
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

        public void SetValue(BuildVariant variant, string value)
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

        public int CompareTo(BuildVariantElementInfo rhs)
        {
            return Weight - rhs.Weight;
        }
        int IComparable.CompareTo(object rhs)
        {
            return CompareTo((BuildVariantElementInfo)rhs);
        }

        private readonly PropertyInfo m_pinfo;
        private readonly FieldInfo m_finfo;
    }
}
