//  Copyright (c) 2020-present amlovey
//  
using System;

namespace Yade.Runtime
{
    /// <summary>
    /// Extended API for <see cref="T:Yade.Runtime.Cell"/> class
    /// </summary>
    public static class CellExtensions
    {
        /// <summary>
        /// Convert value to long type
        /// </summary>
        public static long GetLong(this Cell cell)
        {
            return GetParseValue<long>(cell, long.Parse);
        }

        /// <summary>
        /// Convert value to int type
        /// </summary>
        public static int GetInt(this Cell cell)
        {
            return GetParseValue<int>(cell, int.Parse);
        }

        /// <summary>
        /// Convert value to float type
        /// </summary>
        public static float GetFloat(this Cell cell)
        {
            return GetParseValue<float>(cell, float.Parse);
        }

        /// <summary>
        /// Convert value to double type
        /// </summary>
        public static double GetDouble(this Cell cell)
        {
            return GetParseValue<double>(cell, double.Parse);
        }

        /// <summary>
        /// Convert value to bool type
        /// </summary>
        public static bool GetBool(this Cell cell)
        {
            return GetParseValue<bool>(cell, bool.Parse);
        }

        /// <summary>
        /// Convert value to type
        /// </summary>
        /// <param name="parseFunction">Parse function of type</param>
        /// <returns>Type converted</returns>
        public static T GetParseValue<T>(this Cell cell, Func<string, T> parseFunction)
        {
            if (cell == null)
            {
                return default(T);
            }

            var value = cell.GetValue();
            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }

            return parseFunction(cell.GetValue());
        }
    }
}
