//  Copyright (c) 2020-present amlovey
//  
using System;
using System.Collections.Generic;
using System.Text;

namespace Yade.Runtime
{
    public class IndexHelper
    {
        /// <summary>
        /// Convert int to alhpa based index. For example, 0 to A, 1 to B
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Alpha based Index</returns>
        public static string IntToAlphaIndex(int index)
        {
            string s = string.Empty;
            int i = index;
            do
            {
                s = AlphaPattern[i % 26].ToString() + s;
                i  = (i / 26) - 1;
            }
            while(i >= 0);

            return s;
        }

        private const string AlphaPattern = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Convert alpha based index to int index. For example, A to 0, B to 1
        /// </summary>
        /// <param name="alhpaIndex"></param>
        /// <returns></returns>
        public static int AlphaToIntIndex(string alhpaIndex)
        {
            var charArray = alhpaIndex.ToUpper();
            int index = 0;

            for (int i = 0; i < charArray.Length; i++)
            {
                if (!char.IsLetter(charArray[i]))
                {
                    return -1;
                }

                int indexInPattern = AlphaPattern.IndexOf(charArray[i]);
                
                if (indexInPattern == -1)
                {
                    return -1;
                }

                if (i == charArray.Length - 1)
                {
                    index = index + indexInPattern;
                }
                else
                {
                    int power = charArray.Length - i - 1;
                    index = index + (indexInPattern + 1) * (int)Math.Pow(26, power);
                }
            }

            return index;
        }

        /// <summary>
        /// Get alpha based cell index. For example, (0, 0) => A1, (1, 3) => D2
        /// </summary>
        /// <param name="rowIndex">Index of row</param>
        /// <param name="columnIndex">Index of column</param>
        /// <returns>Alpha based cell index</returns>
        public static string ToAlphaBasedCellIndex(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || columnIndex < 0)
            {
                return string.Empty;
            }

            var alpha = IntToAlphaIndex(columnIndex);
            return string.Format("{0}{1}", alpha, rowIndex + 1);
        }

        // Cache to speed up alpha index to cell index convertion.
        private static Dictionary<string, CellIndex> AlphaIndexMap = new Dictionary<string, CellIndex>();

        /// <summary>
        /// Convert alpha based index to cell index. For example, A1 => (0, 0)
        /// </summary>
        /// <param name="alphaCellIndex">Alpha based cell index</param>
        /// <returns></returns>
        public static CellIndex AlphaBasedToCellIndex(string alphaCellIndex)
        {
            if (string.IsNullOrEmpty(alphaCellIndex))
            {
                return null;
            }
            
            var key = alphaCellIndex.ToUpper();
            if (AlphaIndexMap.ContainsKey(key))
            {
                return AlphaIndexMap[key];
            }

            StringBuilder alpha = new StringBuilder();
            int i = 0;
            for (; i < alphaCellIndex.Length; i++)
            {
                if (char.IsLetter(alphaCellIndex[i]))
                {
                    alpha.Append(alphaCellIndex[i]);
                    continue;
                }

                if (char.IsDigit(alphaCellIndex[i]))
                {
                    break;
                }
            }

            // Invalid cell index
            if (alpha.Length == 0 || alpha.Length == alphaCellIndex.Length)
            {
                return null;
            }

            var digit = alphaCellIndex.Substring(i);
            
            int row;
            if (int.TryParse(digit, out row))
            {
                var column = AlphaToIntIndex(alpha.ToString());
                if (row == -1 || column == -1)
                {
                    return null;
                }


                var ci = new CellIndex() { row = row - 1, column = column };
                AlphaIndexMap.Add(key, ci);
                return ci;
            }

            return null;
        }
    }
}
