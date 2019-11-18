using System;
using System.Collections.Generic;

namespace TeamProject3
{
    public static class Helper
    {
        private static Random _random = new Random();

        public static int ScreenWidth => 1920;
        public static int ScreenHeight => 1080;

        public static void Shuffle<T>(this IList<T> collection)
        {
            var n = collection.Count;

            while (n > 1)
            {
                n--;
                var k = _random.Next(n + 1);
                T value = collection[k];
                collection[k] = collection[n];
                collection[n] = value;
            }
        }
    }
}
