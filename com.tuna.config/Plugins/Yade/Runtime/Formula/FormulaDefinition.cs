//  Copyright (c) 2020-present amlovey
//  
using System;

namespace Yade.Runtime.Formula
{
    internal class MinsOperator : FormulaOperator
    {
        public override object Evalute()
        {
            var left = ValueConverter.ToNumber(LeftValue);
            var right = ValueConverter.ToNumber(RightValue);
            return left - right;
        }

        public override string GetName()
        {
            return "-";
        }
    }

    internal class AddOperator : FormulaOperator
    {
        public override object Evalute()
        {
            var left = ValueConverter.ToNumber(LeftValue);
            var right = ValueConverter.ToNumber(RightValue);
            return left + right;
        }

        public override string GetName()
        {
            return "+";
        }
    }

    internal class MultipleOperator : FormulaOperator
    {
        public override object Evalute()
        {
            var left = ValueConverter.ToNumber(LeftValue);
            var right = ValueConverter.ToNumber(RightValue);
            return left * right;

            throw new Exception("Input value type is not correct in in MultipleOperator");
        }

        public override string GetName()
        {
            return "*";
        }
    }

    internal class DivideOperator : FormulaOperator
    {
        public override object Evalute()
        {
            var left = ValueConverter.ToNumber(LeftValue);
            var right = ValueConverter.ToNumber(RightValue);
            return left / right;

            throw new Exception("Input value type is not correct in in DivideOperator");
        }

        public override string GetName()
        {
            return "/";
        }
    }

    internal class PowerOperator : FormulaOperator
    {
        public override object Evalute()
        {
            var left = ValueConverter.ToNumber(LeftValue);
            var right = ValueConverter.ToNumber(RightValue);
            return Math.Pow(left, right);

            throw new Exception("Input value type is not correct in in DivideOperator");
        }

        public override string GetName()
        {
            return "^";
        }
    }

    internal class Sum : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            double value = 0;

            foreach (var item in this.Parameters)
            {
                if (item == null)
                {
                    continue;
                }

                var itemValue = item.ToString();
                if (string.IsNullOrEmpty(itemValue))
                {
                    continue;
                }

                if (ValueConverter.IsNumber(item.ToString()))
                {
                    value += ValueConverter.ToNumber(item.ToString());
                }
            }

            return value;
        }

        public override string GetName()
        {
            return "SUM";
        }
    }

    internal class Average : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            double total = 0;
            foreach (var item in this.Parameters)
            {
                if (item == null)
                {
                    continue;
                }

                var itemValue = item.ToString();
                if (string.IsNullOrEmpty(itemValue))
                {
                    continue;
                }

                if (ValueConverter.IsNumber(item.ToString()))
                {
                    total += ValueConverter.ToNumber(item.ToString());
                }
            }

            return total / this.Parameters.Count;
        }

        public override string GetName()
        {
            return "AVERAGE";
        }
    }

    internal class Concat : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return null;
            }

            var value = string.Empty;
            foreach (var item in this.Parameters)
            {
                if (item != null)
                {
                    value += item.ToString();
                }
            }

            return value;
        }

        public override string GetName()
        {
            return "CONCAT";
        }
    }

    internal class Max : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            double value = 0;
            foreach (var item in this.Parameters)
            {
                if (item == null)
                {
                    continue;
                }

                var itemValue = item.ToString();
                if (string.IsNullOrEmpty(itemValue))
                {
                    continue;
                }

                if (ValueConverter.IsNumber(item.ToString()))
                {
                    value = Math.Max(value, ValueConverter.ToNumber(item.ToString()));
                }
            }

            return value;
        }

        public override string GetName()
        {
            return "MAX";
        }
    }

    internal class Min : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            double value = double.MaxValue;
            foreach (var item in this.Parameters)
            {
                if (item == null)
                {
                    continue;
                }

                var itemValue = item.ToString();
                if (string.IsNullOrEmpty(itemValue))
                {
                    continue;
                }
                
                if (ValueConverter.IsNumber(item.ToString()))
                {
                    value = Math.Min(value, ValueConverter.ToNumber(item.ToString()));
                }
            }

            return value;
        }

        public override string GetName()
        {
            return "MIN";
        }
    }
}
