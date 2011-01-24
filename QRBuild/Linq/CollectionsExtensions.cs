using System.Collections.Generic;

namespace QRBuild.Linq
{
    public static class CollectionsExtensions
    {
        public static void AddRange<T>(this ICollection<T> lhs, IEnumerable<T> rhs)
        {
            foreach (T item in rhs) 
            {
                lhs.Add(item);
            }
        }

        public static void AddRange<T>(this Queue<T> lhs, IEnumerable<T> rhs)
        {
            foreach (T item in rhs) 
            {
                lhs.Enqueue(item);
            }
        }

        public static void AddRange<T>(this Stack<T> lhs, IEnumerable<T> rhs)
        {
            foreach (T item in rhs) 
            {
                lhs.Push(item);
            }
        }
    }
}
