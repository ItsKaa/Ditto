﻿namespace Ditto.Extensions
{
    public static class NumberExtensions
    {
        public static int KiB(this int value) => value * 1024;
        public static ulong KiB(this ulong value) => value * 1024;

        public static int KB(this int value) => value * 1000;
        public static ulong KB(this ulong value) => value * 1000;
        
        public static int MiB(this int value) => value.KiB() * 1024;
        public static ulong MiB(this ulong value) => value.KiB() * 1024;

        public static int MB(this int value) => value.KB() * 1000;
        public static ulong MB(this ulong value) => value.KB() * 1000;

        public static int GiB(this int value) => value.MiB() * 1024;
        public static ulong GiB(this ulong value) => value.MiB() * 1024;

        public static int GB(this int value) => value.MB() * 1000;
        public static ulong GB(this ulong value) => value.MB() * 1000;

        public static string Ordinal(this int number)
        {
            var work = number.ToString();
            if ((number % 100) == 11 || (number % 100) == 12 || (number % 100) == 13)
                return work + "th";
            switch (number % 10)
            {
                case 1: work += "st"; break;
                case 2: work += "nd"; break;
                case 3: work += "rd"; break;
                default: work += "th"; break;
            }
            return work;
        }
    }
}
