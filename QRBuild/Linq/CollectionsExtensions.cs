using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QRBuild.Linq
{
    public static class CollectionsExtensions
    {
        public static void AddRange<T>(this ICollection<T> lhs, ICollection<T> rhs)
        {
            foreach (T item in rhs)
            {
                lhs.Add(item);
            }
        }
    }
}
