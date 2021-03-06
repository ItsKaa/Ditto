﻿using System;

namespace Ditto.Extensions
{
    public static class EnumerationExtensions
    {
        public static bool Has<T>(this Enum @enum, T value)
        {
            try
            {
                return (((int)(object)@enum & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }

        public static bool Is<T>(this Enum @enum, T value)
        {
            try
            {
                return (int)(object)@enum == (int)(object)value;
            }
            catch
            {
                return false;
            }
        }


        public static T Add<T>(this Enum @enum, T value)
        {
            try
            {
                return (T)(object)(((int)(object)@enum | (int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not append value from enumerated type '{typeof(T).Name}'.",
                    ex
                );
            }
        }
        public static T Remove<T>(this Enum @enum, T value)
        {
            try
            {
                return (T)(object)(((int)(object)@enum & ~(int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not remove value from enumerated type '{typeof(T).Name}'.",
                    ex
                );
            }
        }

    }
}
