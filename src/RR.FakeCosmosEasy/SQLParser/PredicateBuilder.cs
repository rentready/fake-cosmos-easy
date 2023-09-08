using Newtonsoft.Json.Linq;
using RR.FakeCosmosEasy.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RR.FakeCosmosEasy.SQLParser
{
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() => param => true;
        public static Expression<Func<T, bool>> False<T>() => param => false;

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }

        [PredicateFunction("IS_DEFINED")]
        public static Expression<Func<T, bool>> IsDefined<T>(Expression<Func<T, JToken>> expr)
        {
            var typeProperty = typeof(JToken).GetProperty("Type");
            var typeGetterMethod = typeProperty.GetGetMethod();

            // Check if the expr.Body is not null
            var notNullExpression = Expression.NotEqual(expr.Body, Expression.Constant(null, typeof(JToken)));

            // Check for property not being JTokenType.Null
            var nullCheckExpression = Expression.NotEqual(
                Expression.Call(expr.Body, typeGetterMethod),
                Expression.Constant(JTokenType.Null));

            // Check for property not being JTokenType.Undefined
            var undefinedCheckExpression = Expression.NotEqual(
                Expression.Call(expr.Body, typeGetterMethod),
                Expression.Constant(JTokenType.Undefined));

            // Combine all checks using AND
            var combinedCheckExpression = Expression.AndAlso(
                Expression.AndAlso(notNullExpression, nullCheckExpression),
                undefinedCheckExpression
            );

            return Expression.Lambda<Func<T, bool>>(combinedCheckExpression, expr.Parameters);
        }

        [PredicateFunction("IS_STRING")]
        public static Expression<Func<T, bool>> IsString<T>(Expression<Func<T, JToken>> expr)
        {
            var typeProperty = typeof(JToken).GetProperty("Type");
            var typeGetterMethod = typeProperty.GetGetMethod();

            // Check if the expr.Body is of type JTokenType.String
            var stringCheckExpression = Expression.Equal(
                Expression.Call(expr.Body, typeGetterMethod),
                Expression.Constant(JTokenType.String));

            return Expression.Lambda<Func<T, bool>>(stringCheckExpression, expr.Parameters);
        }

        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.Not(expr.Body), expr.Parameters);
        }
    }
}
