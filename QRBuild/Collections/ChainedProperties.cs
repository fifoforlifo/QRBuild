using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QRBuild.Collections
{
    public class ChainedProperties
    {
        public ChainedProperties()
        {
            m_parent = null;
        }
        public ChainedProperties(ChainedProperties parent)
        {
            m_parent = parent;
        }

        public object Get(object key)
        {
            object value;
            if (m_items.TryGetValue(key, out value))
            {
                if (value != null)
                {
                    return value;
                }
            }
            if (m_parent != null)
            {
                return m_parent.Get(key);
            }
            return null;
        }

        public TValue Get<TValue>(object key)
            where TValue : class
        {
            return Get(key) as TValue;
        }

        public void Set(object key, object value)
        {
            m_items[key] = value;
        }

        readonly Dictionary<object, object> m_items = new Dictionary<object, object>();
        readonly ChainedProperties m_parent;
    }
}
