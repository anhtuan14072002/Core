//  Copyright (c) 2020-present amlovey
//  
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Yade.Runtime.Formula
{
    public class GrammarException : Exception
    {
        public GrammarException(TokenType type, int posotion, string value)
            : this(string.Format("Grammar exception: {0} type token error, at position {1}, current value is '{2}'", type.ToString(), posotion, value))
        {

        }

        public GrammarException(string message) : base(message)
        {

        }
    }

    public class ASTErrorExcpetion : Exception
    {
        public ASTErrorExcpetion(string message) : base(message)
        {

        }
    }

    public enum TokenType
    {
        OpenParen,
        CloseParen,
        Number,
        String,
        Function,
        Operator,
        CellRef,
        CellRangeRef,
        NotSupport
    }

    public class Token
    {
        public TokenType type;
        public string value;
    }

    public enum ASTNodeType
    {
        Program,
        CallExpression,
        CellRefExpresssion,
        CellRangeRefExpression,
        OperatorExpression,
        NumberLiteral,
        StringLiteral,
    }

    [Serializable]
    public class ASTNode
    {
        public ASTNodeType type;
    }

    [Serializable]
    public class ASTNodeProgram : ASTNode
    {
        public List<ASTNode> body;

        public ASTNodeProgram()
        {
            body = new List<ASTNode>();
            type = ASTNodeType.Program;
        }
    }

    [Serializable]
    public class ASTNodeCallExpression : ASTNode
    {
        public List<ASTNode> callParams;
        public string methodName;

        public ASTNodeCallExpression()
        {
            callParams = new List<ASTNode>();
            methodName = string.Empty;
            type = ASTNodeType.CallExpression;
        }
    }

    [Serializable]
    public class ASTValueNode : ASTNode
    {
        public string value;
    }

    [Serializable]
    public class ASTNodeNumberLiteral : ASTValueNode
    {
        public ASTNodeNumberLiteral()
        {
            type = ASTNodeType.NumberLiteral;
        }
    }

    [Serializable]
    public class ASTNodeStringLiteral : ASTValueNode
    {
        public ASTNodeStringLiteral()
        {
            type = ASTNodeType.StringLiteral;
        }
    }

    [Serializable]
    public class ASTNodeCellRefExpression : ASTValueNode
    {
        public ASTNodeCellRefExpression()
        {
            type = ASTNodeType.CellRefExpresssion;
        }
    }

    [Serializable]
    public class ASTNodeCellRangeRefExpression : ASTValueNode
    {
        public ASTNodeCellRangeRefExpression()
        {
            type = ASTNodeType.CellRangeRefExpression;
        }
    }

    public class ASTNodeOperatorExpression : ASTValueNode
    {
        public ASTNode leftNode;
        public ASTNode rightNode;

        public ASTNodeOperatorExpression()
        {
            type = ASTNodeType.OperatorExpression;
        }
    }

    public class Tokenzier
    {
        HashSet<string> functions = new HashSet<string>() { "sum", "max", "min", "average", "asset", "concat", "enum" };
        private int count;

        public Tokenzier()
        {
            count = 0;
        }

        public List<Token> GetTokens(string formula, HashSet<string> functionsOverride = null)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return new List<Token>();
            }

            if (functionsOverride != null)
            {
                functions = functionsOverride;
            }

            count = 0;
            var tokens = new List<Token>();
            while (count < formula.Length)
            {
                char currentChar = GetCharInFormula(formula, count);
                // If char is open paren
                if (currentChar == '(')
                {
                    count++;
                    tokens.Add(new Token() { type = TokenType.OpenParen, value = currentChar.ToString() });
                    continue;
                }

                // If char is close paren
                if (currentChar == ')')
                {
                    count++;
                    tokens.Add(new Token() { type = TokenType.CloseParen, value = currentChar.ToString() });
                    continue;
                }

                if (currentChar == '-')
                {
                    string value = currentChar.ToString();
                    count++;
                    var tempCount = count;
                    var ch = GetCharInFormula(formula, tempCount);
                    while (tempCount < formula.Length && char.IsDigit(ch))
                    {
                        value = value + ch.ToString();
                        tempCount++;
                        if (tempCount < formula.Length)
                        {
                            ch = GetCharInFormula(formula, tempCount);
                        }
                    }

                    if (value.Length == 1)
                    {
                        tokens.Add(new Token() { type = TokenType.Operator, value = currentChar.ToString() });
                    }
                    else
                    {
                        if (tokens.Count > 0
                            && (tokens[tokens.Count - 1].type == TokenType.Operator
                                || tokens[tokens.Count - 1].type == TokenType.OpenParen))
                        {
                            tokens.Add(new Token() { type = TokenType.Number, value = value });
                            count = tempCount;
                        }
                        else
                        {
                            tokens.Add(new Token() { type = TokenType.Operator, value = "-" });
                        }
                    }
                    continue;
                }

                // if char is operator
                if (currentChar == '+'
                    || currentChar == '*'
                    || currentChar == '^'
                    || currentChar == '/')
                {
                    count++;
                    tokens.Add(new Token() { type = TokenType.Operator, value = currentChar.ToString() });
                    continue;
                }

                // If char is double quotation marks
                if (currentChar == '"')
                {
                    string value = string.Empty;
                    count++;
                    currentChar = GetCharInFormula(formula, count);
                    while (currentChar != '"' && count < formula.Length - 1)
                    {
                        value = value + currentChar.ToString();
                        count++;
                        currentChar = GetCharInFormula(formula, count);
                    }

                    if (count == formula.Length - 1 && currentChar != '"')
                    {
                        throw new GrammarException(TokenType.String, count, value);
                    }

                    count++;
                    tokens.Add(new Token() { type = TokenType.String, value = value });
                    continue;
                }

                // If char is alpha, it may be string or function or cell or cellrange
                if (char.IsLetter(currentChar))
                {
                    string value = currentChar.ToString();
                    count++;
                    currentChar = GetCharInFormula(formula, count);
                    bool hasDigit = char.IsDigit(currentChar);
                    while (count < formula.Length && (char.IsLetterOrDigit(currentChar) || currentChar == ':'))
                    {
                        if (!hasDigit)
                        {
                            hasDigit = char.IsDigit(currentChar);
                        }
                        value = value + currentChar.ToString();

                        count++;
                        if (count < formula.Length)
                        {
                            currentChar = GetCharInFormula(formula, count);
                        }
                    }

                    // Don't have digit char means it's fuction string
                    if (!hasDigit)
                    {
                        if (!functions.Contains(value.ToLower().Trim()))
                        {
                            throw new GrammarException(TokenType.NotSupport, count - 1, value);
                        }

                        tokens.Add(new Token() { type = TokenType.Function, value = value });
                        continue;
                    }

                    if (value.Contains(":"))
                    {
                        var temp = value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length != 2)
                        {
                            throw new GrammarException(TokenType.CellRangeRef, count - 1, value);
                        }

                        if (IsCellTypeToken(temp[0]) && IsCellTypeToken(temp[1]))
                        {
                            tokens.Add(new Token() { type = TokenType.CellRangeRef, value = value });
                        }
                        else
                        {
                            throw new GrammarException(TokenType.CellRangeRef, count - 1, value);
                        }
                    }
                    else
                    {
                        if (IsCellTypeToken(value))
                        {
                            tokens.Add(new Token() { type = TokenType.CellRef, value = value });
                        }
                        else
                        {
                            throw new GrammarException(TokenType.CellRef, count - 1, value);
                        }
                    }

                    continue;
                }

                // If it's digit
                if (char.IsDigit(currentChar))
                {
                    string value = currentChar.ToString();
                    count++;
                    if (count < formula.Length)
                    {
                        currentChar = GetCharInFormula(formula, count);
                    }
                    while (count < formula.Length && (char.IsDigit(currentChar) || currentChar == '.'))
                    {
                        value = value + currentChar.ToString();
                        count++;
                        if (count < formula.Length)
                        {
                            currentChar = GetCharInFormula(formula, count);
                        }
                    }

                    tokens.Add(new Token() { type = TokenType.Number, value = value });
                    continue;
                }

                // If it's white space, just contine
                if (Regex.IsMatch(currentChar.ToString(), "\\s") || currentChar == ',')
                {
                    count++;
                    continue;
                }

                count++;
            }

            return tokens;
        }

        private char GetCharInFormula(string formula, int index)
        {
            if (index > formula.Length - 1)
            {
                throw new GrammarException(string.Format("Index out of bouds of formula '{0}', at {1}", formula, index));
            }

            return formula[index];
        }

        private bool IsCellTypeToken(string value)
        {
            return Regex.IsMatch(value.Trim().ToLower(), @"[a-z]+\d+\b");
        }
    }

    public class ASTParser
    {
        private int count;
        private List<Token> tokens;
        private ASTNodeProgram programNode;

        public ASTParser()
        {
            count = 0;
            tokens = null;
        }

        private void Skip(TokenType type)
        {
            if (count < this.tokens.Count)
            {
                var token = this.tokens[count];
                if (token.type == type)
                {
                    count++;
                }
            }
        }

        private ASTNode GetFactor()
        {
            if (count > this.tokens.Count - 1)
            {
                return null;
            }

            var token = this.tokens[count];
            switch (token.type)
            {
                case TokenType.CellRef:
                    count++;
                    return new ASTNodeCellRefExpression() { value = token.value };
                case TokenType.CellRangeRef:
                    count++;
                    return new ASTNodeCellRangeRefExpression() { value = token.value };
                case TokenType.Number:
                    count++;
                    return new ASTNodeNumberLiteral() { value = token.value };
                case TokenType.String:
                    count++;
                    return new ASTNodeStringLiteral() { value = token.value };
                case TokenType.Function:
                    var functionNode = new ASTNodeCallExpression();
                    functionNode.methodName = token.value;
                    Skip(TokenType.Function);
                    Skip(TokenType.OpenParen);
                    while (count < this.tokens.Count)
                    {
                        if (count > this.tokens.Count - 1)
                        {
                            break;
                        }

                        token = tokens[count];
                        if (token.type == TokenType.CloseParen)
                        {
                            Skip(TokenType.CloseParen);
                            break;
                        }
                        functionNode.callParams.Add(GetExpression());
                    }

                    return functionNode;
                case TokenType.OpenParen:
                    Skip(TokenType.OpenParen);
                    var exr = GetExpression();
                    Skip(TokenType.CloseParen);
                    return exr;
            }

            return null;
            // throw new ASTErrorExcpetion("Unexcepted token when parsing at position " + count);
        }

        private ASTNode GetTerm()
        {
            var node = GetFactor();
            if (count > this.tokens.Count - 1)
            {
                return node;
            }

            var token = this.tokens[count];
            if (token.type == TokenType.Operator)
            {
                switch (token.value)
                {
                    case "*":
                    case "/":
                    case "^":
                        var opNode = new ASTNodeOperatorExpression();
                        opNode.value = token.value;
                        opNode.leftNode = node;
                        count++;
                        opNode.rightNode = GetTerm();
                        return opNode;
                }
            }

            return node;
        }

        private ASTNode GetExpression()
        {
            var node = GetTerm();
            if (count > this.tokens.Count - 1)
            {
                return node;
            }

            var token = this.tokens[count];
            if (token.type == TokenType.Operator)
            {
                switch (token.value)
                {
                    case "+":
                    case "-":
                        var opNode = new ASTNodeOperatorExpression();
                        opNode.value = token.value;
                        opNode.leftNode = node;
                        count++;
                        opNode.rightNode = GetExpression();
                        return opNode;
                }
            }

            return node;
        }

        public ASTNodeProgram Parse(List<Token> tokens)
        {
            /*
                Grammar:
                    factor: Number|String|CellRef|CellRangeRef|(expression)|Function(expression)|-(expression)
                    term: factor [[*|/|^] expression]
                    expression: term [[+|-] term]
            */

            count = 0;
            this.programNode = new ASTNodeProgram();
            this.tokens = tokens;
            while (count < this.tokens.Count)
            {
                this.programNode.body.Add(GetExpression());
            }

            return programNode;
        }
    }

    public class FormulaEngine
    {
        private Tokenzier tokenzier;
        private ASTParser parser;
        private YadeSheetData dataProxy;

        private static Dictionary<string, Type> operators;
        private static Dictionary<string, Type> functions;

        private Dictionary<string, List<Token>> TokenCache;

        public bool IsValidFormula(string formula)
        {
            try
            {
                var tokens = tokenzier.GetTokens(formula);
                
                var count = tokens.Count;
                if (count == 0)
                {
                    return false;
                }

                if (tokens[count - 1].value != ")")
                {
                    return false;
                }

                if (tokens[0].value == "(")
                {
                    return false;
                }

                if (!functions.ContainsKey(tokens[0].value.ToLower()))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public FormulaEngine(YadeSheetData dataProxy)
        {
            this.dataProxy = dataProxy;
            tokenzier = new Tokenzier();
            parser = new ASTParser();
            operators = new Dictionary<string, Type>();
            functions = new Dictionary<string, Type>();
            TokenCache = new Dictionary<string, List<Token>>();
            
            Init();
        }

        public List<Token> GetTokens(string formula)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return new List<Token>();
            }

            if (TokenCache.ContainsKey(formula))
            {
                return TokenCache[formula];
            }

            var registeredFunctions = new HashSet<string>();
            foreach (var key in functions.Keys)
            {
                registeredFunctions.Add(key);
            }

            var tokens = tokenzier.GetTokens(formula, registeredFunctions);
            TokenCache.Add(formula, tokens);
            return tokens;
        }

        private void Init()
        {
            AddOperator<MinsOperator>();
            AddOperator<AddOperator>();
            AddOperator<PowerOperator>();
            AddOperator<MultipleOperator>();
            AddOperator<DivideOperator>();

            AddFunction<Sum>();
            AddFunction<Average>();
            AddFunction<Concat>();
            AddFunction<Max>();
            AddFunction<Min>();
        }

        private void AddOperator<T>() where T : FormulaOperator
        {
            var t = Activator.CreateInstance<T>();
            var key = t.GetName().ToLower();
            if (!operators.ContainsKey(key))
            {
                operators.Add(key, typeof(T));
            }
        }

        public string[] GetFunctionNames()
        {
            List<string> names = new List<string>();
            foreach (var item in functions.Keys)
            {
                names.Add(item);
            }
            return names.ToArray();
        }

        public void AddFunction(FormulaFunction function)
        {
            var key = function.GetName().ToLower();
            if (!functions.ContainsKey(key))
            {
                functions.Add(key, function.GetType());
            }
        }

        public void AddFunction<T>() where T : FormulaFunction
        {
            var t = Activator.CreateInstance<T>();
            var key = t.GetName().ToLower();
            if (!functions.ContainsKey(key))
            {
                functions.Add(key, typeof(T));
            }
        }

        public object Evaluate(string formula)
        {
            try
            {
                var program = parser.Parse(this.GetTokens(formula));
                var value = Visit(program);
                if (value == null)
                {
                    return string.Empty;
                }

                return value;
            }
            catch
            {
                // throw;
            }

            return string.Empty;
        }

        private object Visit(ASTNode node)
        {
            if (node == null)
            {
                return null;
            }

            switch (node.type)
            {
                case ASTNodeType.Program:
                    return Visit((node as ASTNodeProgram).body[0]);
                case ASTNodeType.NumberLiteral:
                    var numberNode = node as ASTNodeNumberLiteral;
                    return numberNode.value;
                case ASTNodeType.StringLiteral:
                    return (node as ASTNodeStringLiteral).value;
                case ASTNodeType.CellRefExpresssion:
                    var cellRefNode = node as ASTNodeCellRefExpression;
                    var cellIndex = IndexHelper.AlphaBasedToCellIndex(cellRefNode.value);
                    var cell = this.dataProxy.GetCell(cellIndex.row, cellIndex.column);
                    if (cell == null)
                    {
                        return null;
                    }

                    if (cell.HasUnityObject())
                    {
                        return cell.GetUnityObject();
                    }
                    else
                    {
                        return cell.GetValue();
                    }
                case ASTNodeType.CellRangeRefExpression:
                    var cellRangeRefNode = node as ASTNodeCellRangeRefExpression;
                    var temp = cellRangeRefNode.value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    var startCell = IndexHelper.AlphaBasedToCellIndex(temp[0]);
                    var endCell = IndexHelper.AlphaBasedToCellIndex(temp[1]);
                    var cellRange = new CellRange(startCell.row, endCell.row, startCell.column, endCell.column);
                    List<string> values = new List<string>();
                    cellRange.ForEach((row, Column) =>
                    {
                        var c = this.dataProxy.GetCell(row, Column);
                        if (c != null)
                        {
                            values.Add(this.dataProxy.GetCell(row, Column).GetValue());
                        }
                    });
                    return values;
                case ASTNodeType.OperatorExpression:
                    var operatorNode = node as ASTNodeOperatorExpression;
                    var key = operatorNode.value.ToLower();
                    var operatorDef = Activator.CreateInstance(operators[key]) as FormulaOperator;
                    operatorDef.LeftValue = Visit(operatorNode.leftNode);
                    operatorDef.RightValue = Visit(operatorNode.rightNode);
                    return operatorDef.Evalute();
                case ASTNodeType.CallExpression:
                    var callNode = node as ASTNodeCallExpression;
                    var callKey = callNode.methodName.ToLower();
                    var functioDefinition = Activator.CreateInstance(functions[callKey]) as FormulaFunction;
                    foreach (var paramNode in callNode.callParams)
                    {
                        var value = Visit(paramNode);
                        if (value is List<string>)
                        {
                            var list = value as List<string>;
                            foreach (var v in list)
                            {
                                functioDefinition.Parameters.Add(v);
                            }
                        }
                        else
                        {
                            functioDefinition.Parameters.Add(value);
                        }
                    }

                    return functioDefinition.Evalute();
            }

            throw new Exception("Not support nodes, type is " + node.type.ToString());
        }
    }
}