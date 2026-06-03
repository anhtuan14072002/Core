//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using System;
using System.Globalization;

namespace Yade.Editor
{
    internal class CellValueComparer : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            double numOne;
            double numTwo;

            var oneIsNum = double.TryParse(s1, out numOne);
            var twoIsNum = double.TryParse(s2, out numTwo); 

            if (oneIsNum && twoIsNum)
            {
                return numOne > numTwo ? 1 : numOne == numTwo ? 0 : -1;
            }

            if (oneIsNum)
            {
                return -1;
            }

            if (twoIsNum)
            {
                return 1;
            }

            return string.Compare(s1, s2, true, CultureInfo.InvariantCulture);
        }
    }
}