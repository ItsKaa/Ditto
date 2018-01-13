using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ditto
{
    public static class Randomizer
    {
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
        private const string STRING_VALUES = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NUMBER_VALUES = "0123456789";

#region Strings
        /// <summary>
        /// Creates a random (alphanumeric) string
        /// </summary>
        /// <param name="length">The desired length, must be greater than 0.</param>
        /// <param name="alphanumeric">if true, the generated string will include numeric values.</param>
        public static string RandomString(int length, bool alphanumeric = true)
        {
            if (length <= 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append(RandomArrayElement(
                    (alphanumeric ? (New(1) == 1 ? NUMBER_VALUES : STRING_VALUES)
                    : STRING_VALUES).ToCharArray()
                ));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Gets a random word from http://www.wordgenerator.net
        /// </summary>
        public static string RandomWordFromWeb()
        {
            try
            {
                var words = (WebHelper.GetSourceCode(@"http://www.wordgenerator.net/application/p.php?id=dictionary_words&type=1&spaceflag=false")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                );
                return words[New(0, words.Length - 1)];
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
                return string.Empty;
            }
        }

#endregion Strings



#region Numbers
        /// <summary>
        /// Returns a random double.
        /// </summary>
        public static double New()
        {
            return _random.Value.NextDouble();
        }



        /// <summary>
        /// Returns a random double in the range [min, max]
        /// </summary>
        public static double New(double min, double max)
        {
            if (min == max)
                return min;
            return (max - min) * New() + min;
        }
        /// <summary>
        /// Returns a random non-negative double that is less than the specified maximum.
        /// </summary>
        public static double New(double max) => New(0, max);

        /// <summary>
        /// Returns a random float in the range [min, max]
        /// </summary>
        public static float New(float min, float max)
        {
            if (min == max)
                return min;
            return Convert.ToSingle(New(min * 1.0, max * 1.0));
        }
        /// <summary>
        /// Returns a random non-negative float that is less than the specified maximum.
        /// </summary>
        public static float New(float max) => New(0, max);



        /// <summary>
        /// Returns a random boolean (true or false)
        /// </summary>
        public static bool NewBoolean()
        {
            return 0 == New(0, 1);
        }



        /// <summary>
        /// Returns a random 8-bit signed integer in the range [min, max]
        /// </summary>
        public static sbyte New(sbyte min, sbyte max)
        {
            if (min == max)
                return min;
            return (sbyte)New((int)min, max);
        }
        /// <summary>
        /// Returns a random non-negative 8-bit signed integer that is less than the specified maximum.
        /// </summary>
        public static sbyte New(sbyte max) => New((sbyte)0, max);

        /// <summary>
        /// Returns a random 8-bit unsigned integer in the range [min, max]
        /// </summary>
        public static byte New(byte min, byte max)
        {
            if (min == max)
                return min;
            return (byte)New((int)min, max);
        }
        /// <summary>
        /// Returns a random non-negative 8-bit unsigned integer that is less than the specified maximum.
        /// </summary>
        public static byte New(byte max) => New((byte)0, max);



        /// <summary>
        /// Returns a random 16-bit signed integer in the range [min, max]
        /// </summary>
        public static short New(short min, short max)
        {
            if (min == max)
                return min;
            return (short)New((int)min, max);
        }
        /// <summary>
        /// Returns a random non-negative 16-bit signed integer that is less than the specified maximum.
        /// </summary>
        public static short New(short max) => New((short)0, max);

        /// <summary>
        /// Returns a random 16-bit unsigned integer in the range [min, max]
        /// </summary>
        public static ushort New(ushort min, ushort max)
        {
            if (min == max)
                return min;
            return (ushort)New((uint)min, max);
        }
        /// <summary>
        /// Returns a random non-negative 16-bit unsigned integer that is less than the specified maximum.
        /// </summary>
        public static ushort New(ushort max) => New((ushort)0, max);

        /// <summary>
        /// Returns a random 16-bit unicode character in the range [min, max]
        /// </summary>
        public static char New(char min, char max)
        {
            if (min == max)
                return min;
            return (char)New((int)min, max);
        }
        /// <summary>
        /// Returns a random non-negative 16-bit unicode character that is less than the specified maximum.
        /// </summary>
        public static char New(char max) => New((char)0, max);



        /// <summary>
        /// Returns a random signed integer in the range [min, max]
        /// </summary>
        public static int New(int min, int max)
        {
            if (min == max)
                return min;
            return _random.Value.Next(Math.Min(min, max + 1), Math.Max(min, max + 1));
        }
        /// <summary>
        /// Returns a random non-negative 32-bit signed integer that is less than the specified maximum.
        /// </summary>
        public static int New(int max) => New(0, max);

        /// <summary>
        /// Returns a random 32-bit unsigned integer in the range [min, max]
        /// </summary>
        public static uint New(uint min, uint max)
        {
            if (min == max)
                return min;
            return Convert.ToUInt32(New(min * 1.0, max * 1.0));
        }
        /// <summary>
        /// Returns a random non-negative 32-bit unsigned integer that is less than the specified maximum.
        /// </summary>
        public static uint New(uint max) => New(0, max);



        /// <summary>
        /// Returns a random 64-bit signed integer in the range [min, max]
        /// </summary>
        public static long New(long min, long max)
        {
            if (min == max)
                return min;
            return Convert.ToInt64(New(min * 1.0, max * 1.0));
        }
        /// <summary>
        /// Returns a random non-negative 64-bit signed integer that is less than the specified maximum.
        /// </summary>
        public static long New(long max) => New(0, max);

        /// <summary>
        /// Returns a random 64-bit unsigned integer in the range [min, max]
        /// </summary>
        public static ulong New(ulong min, ulong max)
        {
            if (min == max)
                return min;
            return Convert.ToUInt64(New(min * 1.0, max * 1.0));
        }
        /// <summary>
        /// Returns a random non-negative 64-bit unsigned integer that is less than the specified maximum.
        /// </summary>
        public static ulong New(ulong max) => New(0, max);



        /// <summary>
        /// Returns a random DateTime in the range [min, max]
        /// </summary>
        public static DateTime New(DateTime min, DateTime max)
        {
            if (min.Ticks == max.Ticks)
                return min;
            return new DateTime(New(min.Ticks, max.Ticks));
        }
        /// <summary>
        /// Returns a random DateTime that is less than the specified maximum.
        /// </summary>
        public static DateTime New(DateTime max) => new DateTime(New(DateTime.MinValue.Ticks, max.Ticks ));

        /// <summary>
        /// Returns a random TimeSpan in the range [min, max]
        /// </summary>
        public static TimeSpan New(TimeSpan min, TimeSpan max)
        {
            if (min.Ticks == max.Ticks)
                return min;
            return new TimeSpan(New(min.Ticks, max.Ticks));
        }
        /// <summary>
        /// Returns a random TimeSpan that is less than the specified maximum.
        /// </summary>
        public static TimeSpan New(TimeSpan max) => new TimeSpan(New(TimeSpan.MinValue.Ticks, max.Ticks));
        


        /// <summary>
        /// Returns an exponentially distributed, positive, random deviate 
        /// of unit mean.
        /// </summary>
        public static double NewExponential()
        {
            double dum = 0.0;
            while (dum == 0.0)
                dum = New();
            return -1.0 * Math.Log(dum, Math.E);
        }
        
        /// <summary>
        /// Returns a normally distributed deviate with zero mean and unit 
        /// variance.
        /// </summary>
        public static double NewNormal()
        {
            // based on algorithm from Numerical Recipes
            double rsq = 0.0;
            double v1 = 0.0, v2 = 0.0, fac = 0.0;
            while (rsq >= 1.0 || rsq == 0.0)
            {
                v1 = 2.0 * New() - 1.0;
                v2 = 2.0 * New() - 1.0;
                rsq = v1 * v1 + v2 * v2;
            }
            fac = Math.Sqrt(-2.0 * Math.Log(rsq, Math.E) / rsq);
            return v2 * fac;
        }

#endregion Numbers



#region Miscellaneous
        public static T RandomArrayElement<T>(T[] array)
        {
            if (array.Length == 0)
                return default(T);
            return array[New(array.Length - 1)];
        }
        public static T RandomEnumerableElement<T>(IEnumerable<T> enumerable)
        {
            if (enumerable.Count() == 0)
                return default(T);
            return enumerable.ElementAt(New(enumerable.Count() - 1));
        }

        /// <summary>
        /// Returns a random value of an enumerable.
        /// </summary>
        public static T RandomEnumValue<T>()
        {
            var values = Enum.GetValues(typeof(T)).OfType<T>().ToList();
            if (values.Count == 0)
                return default(T);
            return values[New(0, values.Count - 1)];
        }

#endregion Miscellaneous
    }
}
