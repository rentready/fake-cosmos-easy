using Newtonsoft.Json.Linq;
using RR.FakeCosmosEasy.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RR.FakeCosmosEasy.SQLParser
{
    public class PredicateParser
    {
        public static Func<JObject, bool> ConvertToPredicate(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            return ConvertToExpression(condition, parameters).Compile();
        }

        private static Expression<Func<JObject, bool>> ConvertToExpression(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            return ParseOrCondition(condition, parameters);
        }

        private static Expression<Func<JObject, bool>> ParseOrCondition(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            var parts = SplitByTopLevel(condition, " OR ");
            if (parts.Length == 1)
            {
                return ParseAndCondition(parts[0], parameters);
            }

            var expr = ParseAndCondition(parts[0], parameters);
            for (int i = 1; i < parts.Length; i++)
            {
                expr = expr.Or(ParseAndCondition(parts[i], parameters));
            }

            return expr;
        }

        private static Expression<Func<JObject, bool>> ParseAndCondition(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            var parts = SplitByTopLevel(condition, " AND ");
            if (parts.Length == 1)
            {
                return ParseBasicCondition(parts[0], parameters);
            }

            var expr = ParseBasicCondition(parts[0], parameters);
            for (int i = 1; i < parts.Length; i++)
            {
                expr = expr.And(ParseBasicCondition(parts[i], parameters));
            }

            return expr;
        }

        private static Expression<Func<JObject, bool>> ParseBasicCondition(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            condition = condition.Trim();

            // Handle nested conditions with parentheses
            if (condition.StartsWith("(") && condition.EndsWith(")"))
            {
                return ConvertToExpression(condition.Substring(1, condition.Length - 2), parameters);
            }

            // Handle functions
            if (TryParseFunction(condition, out var expr, parameters))
            {
                return expr;
            }

            return ConvertToSimplePredicate(condition, parameters);
        }

        private static bool TryParseFunction(string condition, out Expression<Func<JObject, bool>> expression, IReadOnlyList<(string Name, object Value)> parameters)
        {
            expression = null;
            bool negateResult = false;

            if (condition.StartsWith("NOT "))
            {
                negateResult = true;
                condition = condition.Substring(4).Trim();
            }

            // Simplified logic using a Dictionary to map function names to their handling method.
            var functions = BuildFunctionCollection();

            foreach (var func in functions)
            {
                if (condition.StartsWith(func.Key))
                {
                    var match = Regex.Match(condition, $@"^{func.Key}\((.+?)\)$");
                    if (match.Success)
                    {
                        var property = match.Groups[1].Value.Replace("c.", "");
                        expression = func.Value(property);

                        if (negateResult)
                        {
                            expression = PredicateBuilder.Not(expression);
                        }

                        return true;
                    }
                }
            }

            return expression != null;
        }

        public static Dictionary<string, Func<string, Expression<Func<JObject, bool>>>> BuildFunctionCollection()
        {
            var functions = new Dictionary<string, Func<string, Expression<Func<JObject, bool>>>>();

            foreach (var method in typeof(PredicateBuilder).GetMethods())
            {
                var attribute = method.GetCustomAttribute<PredicateFunctionAttribute>();
                if (attribute != null)
                {
                    functions.Add(attribute.Name, propName =>
                    {
                        // Construct the dynamic JObject property access.
                        var itemParameter = Expression.Parameter(typeof(JObject), "j");
                        var getValueMethod = typeof(JObject).GetMethod(nameof(JObject.GetValue), new Type[] { typeof(string), typeof(StringComparison) });
                        var valueExpression = Expression.Call(itemParameter, getValueMethod, Expression.Constant(propName), Expression.Constant(StringComparison.OrdinalIgnoreCase));

                        // Invoke the method dynamically
                        var lambda = Expression.Lambda<Func<JObject, JToken>>(valueExpression, itemParameter);
                        return (Expression<Func<JObject, bool>>)method.MakeGenericMethod(typeof(JObject)).Invoke(null, new object[] { lambda });
                    });
                }
            }

            return functions;
        }

        private static string[] SplitByTopLevel(string condition, string delimiter)
        {
            var parts = new List<string>();
            int depth = 0, lastSplit = 0;

            for (int i = 0; i < condition.Length; i++)
            {
                if (condition[i] == '(') depth++;
                else if (condition[i] == ')') depth--;

                if (depth == 0 && i + delimiter.Length <= condition.Length && condition.Substring(i, delimiter.Length) == delimiter)
                {
                    parts.Add(condition.Substring(lastSplit, i - lastSplit));
                    i += delimiter.Length - 1;
                    lastSplit = i + 1;
                }
            }

            parts.Add(condition.Substring(lastSplit));

            return parts.ToArray();
        }

        private static Expression<Func<JObject, bool>> ConvertToSimplePredicate(string condition, IReadOnlyList<(string Name, object Value)> parameters)
        {
            var match = MatchCondition(condition);
            if (!match.Success) return PredicateBuilder.False<JObject>();

            var itemParameter = Expression.Parameter(typeof(JObject), "item");

            // Handle IS_DEFINED or NOT IS_DEFINED condition
            if (match.Groups[4].Success) // This group matches the 'IS_DEFINED' pattern
            {
                var propNameForIsDefined = match.Groups[5].Value;
                var selector = Expression.Lambda<Func<JObject, JToken>>(
                    ConstructPropertyAccessExpression(itemParameter, propNameForIsDefined),
                    itemParameter
                );
                var isDefinedExpr = PredicateBuilder.IsDefined(selector);

                return match.Groups[4].Value.Trim().ToUpper() == "NOT IS_DEFINED" ? PredicateBuilder.Not(isDefinedExpr) : isDefinedExpr;
            }
            else
            {
                // Handle standard conditions
                var propName = match.Groups[1].Value;
                var op = match.Groups[2].Value;
                var paramName = match.Groups[3].Value;

                var tokenProperty = ConstructPropertyAccessExpression(itemParameter, propName);
                var paramValue = parameters.FirstOrDefault(p => p.Name == paramName).Value;
                var compareCall = ConstructComparisonExpression(tokenProperty, paramValue, op);

                var conditionBody = Expression.AndAlso(
                    Expression.NotEqual(tokenProperty, Expression.Constant(null, typeof(JToken))),
                    compareCall
                );

                return Expression.Lambda<Func<JObject, bool>>(conditionBody, itemParameter);
            }
        }

        private static string[] SupportedFunctions
        {
            get
            {
                return typeof(PredicateBuilder).GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(m => m.GetCustomAttribute<PredicateFunctionAttribute>() != null)
                    .Select(m => m.GetCustomAttribute<PredicateFunctionAttribute>().Name.ToUpper()) // Using the custom name from the attribute
                    .ToArray();
            }
        }

        private static string GetFunctionPattern()
        {
            return string.Join("|", SupportedFunctions);
        }

        private static Match MatchCondition(string condition)
        {
            var pattern = $@"(?:c\.(\w+)\s*(=|>|<|!=|<>|<=|>=)?\s*(@\w+))|(NOT\s*)?({GetFunctionPattern()}\((c\.\w+)\))";
            return Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
        }

        private static MethodCallExpression ConstructPropertyAccessExpression(ParameterExpression itemParameter, string propName)
        {
            var getValueMethod = typeof(JObject).GetMethod(nameof(JObject.GetValue), new Type[] { typeof(string), typeof(StringComparison) });
            return Expression.Call(itemParameter, getValueMethod, Expression.Constant(propName), Expression.Constant(StringComparison.OrdinalIgnoreCase));
        }

        private static MethodCallExpression ConstructComparisonExpression(Expression tokenProperty, object paramValue, string op)
        {
            var compareMethod = typeof(PredicateParser).GetMethod(nameof(CompareValues), BindingFlags.Static | BindingFlags.NonPublic);
            var paramValueAsObject = Expression.Convert(Expression.Constant(paramValue), typeof(object));
            return Expression.Call(null, compareMethod, tokenProperty, paramValueAsObject, Expression.Constant(op));
        }

        private static bool CompareValues(JToken token, object paramValue, string op)
        {
            if (token == null || paramValue == null)
                return false;

            // Try to extract and compare as decimal if possible
            try
            {
                if (paramValue is DateTime dateParam)
                {
                    if (token.Type == JTokenType.Date && token.Value<DateTime>() is DateTime tokenDate)
                    {
                        switch (op)
                        {
                            case "=":
                                return tokenDate == dateParam;
                            case ">":
                                return tokenDate > dateParam;
                            case ">=":
                                return tokenDate >= dateParam;
                            case "<":
                                return tokenDate < dateParam;
                            case "<=":
                                return tokenDate <= dateParam;
                            case "<>":
                            case "!=":
                                return tokenDate != dateParam;
                        }
                    }
                }

                var tokenValue = token.Value<decimal>();
                var paramDecimal = Convert.ToDecimal(paramValue);

                switch (op)
                {
                    case "=":
                        return tokenValue == paramDecimal;
                    case ">":
                        return tokenValue > paramDecimal;
                    case ">=":
                        return tokenValue >= paramDecimal;
                    case "<":
                        return tokenValue < paramDecimal;
                    case "<=":
                        return tokenValue <= paramDecimal;
                    case "<>":
                    case "!=":
                        return tokenValue != paramDecimal;
                }
            }
            catch
            {
                // If conversion to decimal fails, continue to string comparison
            }

            // If all else fails, compare as strings
            switch (op)
            {
                case "=":
                    return token.ToString() == paramValue.ToString();
                case "<>":
                case "!=":
                    return token.ToString() != paramValue.ToString();
            }

            return false; // Unsupported operator
        }
    }
}
