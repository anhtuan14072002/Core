//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;

namespace Yade.Runtime.Formula
{
    public abstract class FormulaFunction
    {
        public List<object> Parameters;

        public FormulaFunction()
        {
            Parameters = new List<object>();
        }

        public abstract string GetName();
        public abstract object Evalute();
    }

    public abstract class FormulaOperator
    {
        public object LeftValue;
        public object RightValue;
        
        public abstract string GetName();
        public abstract object Evalute();
    }
}
