using System;
using System.Collections.Generic;
using System.Globalization;


namespace Helper
{
    public static class ExtendedFunctions
    {
        private static readonly Random Random = new Random();
        
        /// <summary>
        /// Returns a random element from the list, or default(T) if the list is null or empty.
        /// </summary>
        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
                return default;

            var index = Random.Next(list.Count);
            return list[index];
        }

        public static string CurrencyFormat(this float amount)
        {
            return MathF.Round(amount).ToString(CultureInfo.CurrentCulture);
        }
    }
}