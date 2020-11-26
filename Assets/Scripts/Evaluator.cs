using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

// MYBRARY
// TODO: write tests
// TODO: throw exceptions rather than using Debug.Assert
public class Evaluator {
    static public string SetVar(string expression, string varname, double val) {
        return Regex.Replace(expression, @"\b" + varname +@"\b", $"{val}");
    }
    
    static List<int> GetParamIndices(string expression, int index) {
        // indices of: '(', ','*, ')'
        Debug.Assert(expression[index] == '(', "Missing opening bracket.");
        int bracket_count = 0;
        List<int> arguments = new List<int>();
        arguments.Add(index);
        for (int i = index; i < expression.Length; i++) {
            if (expression[i] == '(') {
                bracket_count += 1;
            }
            if (expression[i] == ')') {
                bracket_count -= 1;
            }
            if (bracket_count == 1 && expression[i] == ',') {
                arguments.Add(i);
            }
            if (bracket_count == 0) { // we found the exit!
                arguments.Add(i);
                return arguments;
            }
        }
        Debug.Assert(false, "Missing closing bracket.");
        return null;
    }

    // Returns List<string> l such that:
    // expression = prefix + '(' + l[0] + {',' + l[i]}* + ')' + postfix
    static List<string> GetArgs(string expression, int index) {
        List<int> seperators = GetParamIndices(expression, index);
        List<string> subexpressions = new List<string>();
        for (int i = 0; i < seperators.Count - 1; i++) {
            int start_index = seperators[i] + 1;
            int end_index = seperators[i + 1];
            // [start_index, end_index[
            subexpressions.Add(
                    expression.Substring(start_index, end_index - start_index));
        }
        return subexpressions;
    }

    static string ApplyFunction(string expression, EvalFunction func) {
        // Look for all functions of type "op" and apply them recursively.
        string func_str = func.ToString();
        int index = expression.IndexOf(func_str);
        if (index == -1) {  // found no function - done.
            return expression;
        }

        List<string> args = GetArgs(expression, index + func_str.Length);
        List<double> values = new List<double>();

        double result = 0f;
        foreach (string arg in args) {
            values.Add(Eval(arg));
        }

        if (arg_count[func] >= 1) {
            Debug.Assert(args.Count == arg_count[func],
                    $"Passed {args.Count} != {arg_count[func]} " +
                    $"arguments to {func}.");
        } else {
            // allow variable number of arguments, but at least one
            Debug.Assert(args.Count >= 1,
                    $"Passed {args.Count} < 1 " +
                    $"arguments to {func}.");
        }
        switch (func) {
          case EvalFunction.pow:
            result = Math.Pow(values[0], values[1]);
          break;
          case EvalFunction.gt:
            result = values[0] > values[1] ? 1 : 0;
          break;
          case EvalFunction.lt:
            result = values[0] < values[1] ? 1 : 0;
          break;
          case EvalFunction.clamp:
            result = Math.Min(Math.Max(values[0], values[1]), values[2]);
          break;
          case EvalFunction.max:
            result = values.Max();
          break;
          case EvalFunction.min:
            result = values.Min();
          break;
          case EvalFunction.exp:
            result = Math.Exp(values[0]);
          break;
          case EvalFunction.log:
            result = Math.Log(values[0]);
          break;
          case EvalFunction.sin:
            result = Math.Sin(values[0]);
          break;
          case EvalFunction.cos:
            result = Math.Cos(values[0]);
          break;
          case EvalFunction.tan:
            result = Math.Tan(values[0]);
          break;
          case EvalFunction.abs:
            result = Math.Abs(values[0]);
          break;
          case EvalFunction.sign:
            result = Math.Sign(values[0]);
          break;
          case EvalFunction.sqrt:
            result = Math.Sqrt(values[0]);
          break;
          default:
            Debug.Assert(false, $"Operator {func} not found.");
          break;
        }

        int end_index =
                GetParamIndices(expression, index + func_str.Length).Last() + 1;
        // "result" represents the value of [index, end_index[.
        string prefix = expression.Substring(0, index);
        string postfix = expression.Substring(end_index);

        return string.Concat(prefix, $"{result}", postfix);
    }

    static public double EvalSimple(string expression) {
        // NOTE: This only supports operators +, -, *, /, %, >, <, IN, LIKE
        try {
            DataTable dt = new DataTable();
            DataColumn dc = new DataColumn("Eval", typeof(double), expression);
            dt.Columns.Add(dc);
            dt.Rows.Add(0);
            return (double)(dt.Rows[0]["Eval"]);
        } catch (SyntaxErrorException e) {
            throw new System.ArgumentException(
                    $"[{expression}] could not be parsed.");
        }
    }

    enum EvalFunction {
        pow,
        gt,
        lt,
        clamp,
        max,
        min,
        exp,
        log,
        sin,
        cos,
        tan,
        abs,
        sign,
        sqrt,
    }

    static Dictionary<EvalFunction, int> arg_count =
            new Dictionary<EvalFunction, int> {{EvalFunction.pow, 2},
                                               {EvalFunction.gt, 2},
                                               {EvalFunction.lt, 2},
                                               {EvalFunction.clamp, 3},
                                               {EvalFunction.max, -1},
                                               {EvalFunction.min, -1},
                                               {EvalFunction.exp, 1},
                                               {EvalFunction.log, 1},
                                               {EvalFunction.sin, 1},
                                               {EvalFunction.cos, 1},
                                               {EvalFunction.tan, 1},
                                               {EvalFunction.abs, 1},
                                               {EvalFunction.sign, 1},
                                               {EvalFunction.sqrt, 1}};

    static public double Eval(string expression) {
        // NOTE: this function is not meant to be fast
        // Based on some primitive benchmarks, the overhead is in the order
        // of ~0.01 - 6ms per Eval call for moderately complicated functions.
        // If fastness is required, try using EvalSimple or look for alts.
        expression = expression.Replace("M_PI", $"{Math.PI}");
        expression = expression.Replace("M_E", $"{Math.E}");
        // NOTE: can't replace "e" as it could be present in a number (1e5).

        // any of these functions must be followed by an opening bracket
        foreach (EvalFunction func in Enum.GetValues(typeof(EvalFunction))) {
            expression = ApplyFunction(expression, func);
        }

        return EvalSimple(expression);
    }
}
