using System.Collections.Generic;

namespace Ditto.Extensions
{
    public static class RandomExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = Randomizer.New(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }
        
        public static T RandomElement<T>(this IEnumerable<T> enumerable)
        {
            return Randomizer.RandomEnumerableElement(enumerable);
        }
        public static T RandomElement<T>(this T[] array)
        {
            return Randomizer.RandomArrayElement(array);
        }
    }
}
