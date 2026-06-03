//  Copyright (c) 2020-present amlovey
//  
using System.Text.RegularExpressions;
using System;

namespace Yade.Runtime
{
    /// <summary>
    /// Value Converter
    /// </summary>
    internal class ValueConverter
    {
        /// <summary>
        /// Is number string
        /// </summary>
        /// <param name="s">String</param>
        /// <returns>True for it's a number string, False for not a number string</returns>
        public static bool IsNumber(string s)
        {
            return Regex.IsMatch(s, @"\d+[.\d+]?");
        }

        /// <summary>
        /// Convert to double, return 0 if parse failed
        /// </summary>
        /// <param name="s">String</param>
        /// <returns>Double type number</returns>
        public static double ToNumber(object s)
        {
            try
            {
                if (s == null)
                {
                    return 0;
                }

                return Convert.ToDouble(s.ToString());
            }
            catch
            {
                return 0;
            }
        }
    }
}