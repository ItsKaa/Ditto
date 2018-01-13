using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Extensions
{
    // Concurrent Collections
    public static partial class EnumerableExtensions
    {
        public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> collection)
        {
            foreach (var element in collection)
            {
                @this.Add(element);
            }
        }

        public static void EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> collection)
        {
            foreach (T obj in collection)
            {
                queue.Enqueue(obj);
            }
        }
    }

    // Arrays
    public static partial class EnumerableExtensions
    {
        /*
        public static T[] ToSingleDimension<T>(this T[][] @this)
        {
            T[] array = new T[@this.Length];

            Buffer.BlockCopy(@this, 0, array, 0, @this.Length);
            return array;
        }
        */

        public static T[] GetColumn<T>(this T[][] array, int col)
        {
            var colLength = array.GetLength(0);
            var colVector = new T[colLength];

            for (var i = 0; i < colLength; i++)
            {
                if(array[i].Length > col)
                    colVector[i] = array[i][col];
                else
                {
                    colVector[i] = default(T);
                }
            }

            return colVector;
        }
        public static int ColumnCount<T>(this T[][] array)
        {
            var max = 0;
            for (int i = 0; i < array.Length; i++)
            {
                max = Math.Max(max, array[i].Length);
            }
            return max;
        }
    }

    // IEnumerable
    public static partial class EnumerableExtensions
    {
        public static IEnumerable<T> GetColumn<T>(this IEnumerable<T[]> array, int col)
        {
            var colLength = array.Count();
            var colVector = new T[colLength];

            for (var i = 0; i < colLength; i++)
            {
                if (array.ElementAt(i).Length > col)
                    colVector[i] = array.ElementAt(i)[col];
                else
                {
                    colVector[i] = default(T);
                }
            }
            return colVector;
        }
        public static int ColumnCount<T>(this IEnumerable<T[]> array)
        {
            var max = 0;
            for (int i = 0; i < array.Count(); i++)
            {
                max = Math.Max(max, array.ElementAt(i).Length);
            }
            return max;
        }

        public static IEnumerable<TSource> Before<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool includeSource = false)
        {
            var elements = new List<TSource>();
            var amount = source.Count();
            foreach (var element in source)
            {
                if (predicate.Invoke(element))
                {
                    if (includeSource)
                    {
                        elements.Add(element);
                    }
                    break;
                }
                elements.Add(element);
            }
            return elements;
        }
        public static IEnumerable<TSource> Before<TSource>(this IEnumerable<TSource> source, TSource element, bool includeSource = false)
            => Before(source, x => x.Equals(element), includeSource);

        public static IEnumerable<TSource> From<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool includeSource = true)
        {
            var elements = new List<TSource>();
            var index = 0;
            var amount = source.Count();
            foreach (var element in source)
            {
                if (predicate.Invoke(element))
                {
                    // create list
                    for (var i = index + (includeSource ? 0 : 1); i < amount; i++)
                    {
                        elements.Add(source.ElementAt(i));
                    }
                    break;
                }
                index++;
            }
            return elements;
        }
        public static IEnumerable<TSource> From<TSource>(this IEnumerable<TSource> source, TSource element, bool includeSource = true)
            => From(source, x => x.Equals(element), includeSource);

        public static IEnumerable<TSource> After<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            => From(source, predicate, false);
        public static IEnumerable<TSource> After<TSource>(this IEnumerable<TSource> source, TSource element)
            => From(source, x => x.Equals(element), false);



        public static IEnumerable<TSource> FromIndex<TSource>(this IEnumerable<TSource> @this, int index, bool includeSource = true)
        {
            var elements = new List<TSource>();
            var amount = @this.Count();
            for (int i = index + (includeSource && index > 0 ? -1 : 0); i < amount; i++)
            {
                // create list
                elements.Add(@this.ElementAt(i));
            }
            return elements;
        }
        public static IEnumerable<TSource> AfterIndex<TSource>(this IEnumerable<TSource> @this, int index)
            => FromIndex(@this, index, false);



        //public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        //{
        //    return source.Reverse().Take(count).Reverse();
        //}

        public static IEnumerable<TSource> Part<TSource>(this IList<TSource> source, int index, int length, bool removePart = true)
        {
            var partCollection = source.Skip(index).Take(length).ToList();

            if (removePart)
            {
                for (var i = length; i > 0; i--)
                {
                    source.RemoveAt(index);
                }
            }
            return partCollection;
        }

        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selector)
        {
            return source.Where(selector).Count();
        }
    }


    // ICollection
    public static partial class EnumerableExtensions
    {
        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source as IReadOnlyCollection<T> ?? new ReadOnlyCollectionAdapter<T>(source);
        }
        sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
        {
            ICollection<T> source;
            public ReadOnlyCollectionAdapter(ICollection<T> source) { this.source = source; }
            public int Count { get { return source.Count; } }
            public IEnumerator<T> GetEnumerator() { return source.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
