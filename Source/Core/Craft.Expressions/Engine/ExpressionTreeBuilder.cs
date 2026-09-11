using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Craft.Expressions.Ast;
using Craft.Expressions.Constants;
using Craft.Expressions.Exceptions;

namespace Craft.Expressions.Engine;

/// <summary>
/// Builds LINQ expression trees from abstract syntax tree (AST) nodes.
/// </summary>
/// <typeparam name="T">The type of the parameter in the expression.</typeparam>
internal class ExpressionTreeBuilder<T>
{
    /// <summary>
    /// Builds a LINQ expression from an AST node.
    /// </summary>
    /// <param name="node">The AST node to build from.</param>
    /// <param name="param">The parameter expression representing the input value.</param>
    /// <returns>A LINQ expression representing the AST node.</returns>
    /// <exception cref="NotSupportedException">Thrown when an unsupported AST node type or operator is encountered.</exception>
    /// <exception cref="ExpressionEvaluationException">Thrown when a member cannot be resolved on the target type.</exception>
    public Expression Build(AstNode node, ParameterExpression param)
    {
        return node switch
        {
            BinaryAstNode binary => BuildBinary(binary, param),
            UnaryAstNode unary => BuildUnary(unary, param),
            MemberAstNode member => BuildMember(member, param),
            ConstantAstNode constant => BuildConstant(constant),
            MethodCallAstNode methodCall => BuildMethodCall(methodCall, param),
            _ => throw new NotSupportedException($"AST node type '{node.GetType().Name}' is not supported."),
        };
    }

    private Expression BuildBinary(BinaryAstNode node, ParameterExpression param)
    {
        var left = Build(node.Left, param);
        var right = Build(node.Right, param);

        return node.Operator switch
        {
            ExpressionOperators.And => Expression.AndAlso(left, right),
            ExpressionOperators.Or => Expression.OrElse(left, right),
            ExpressionOperators.Equal => Expression.Equal(left, right),
            ExpressionOperators.NotEqual => Expression.NotEqual(left, right),
            ExpressionOperators.GreaterThan => Expression.GreaterThan(left, right),
            ExpressionOperators.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            ExpressionOperators.LessThan => Expression.LessThan(left, right),
            ExpressionOperators.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            _ => throw new NotSupportedException($"Operator '{node.Operator}' is not supported.")
        };
    }

    private Expression BuildUnary(UnaryAstNode node, ParameterExpression param)
    {
        var operand = Build(node.Operand, param);

        return node.Operator switch
        {
            ExpressionOperators.Not => Expression.Not(operand),
            _ => throw new NotSupportedException($"Unary operator '{node.Operator}' is not supported.")
        };
    }

    private static Expression BuildMember(MemberAstNode node, ParameterExpression param)
    {
        Expression expr = param;

        foreach (var member in node.MemberPath)
        {
            var prop = expr.Type.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop != null)
            {
                expr = Expression.Property(expr, prop);
                continue;
            }

            var field = expr.Type.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (field != null)
            {
                expr = Expression.Field(expr, field);
                continue;
            }

            throw new ExpressionEvaluationException("Member not found", expr.Type, member);
        }

        return expr;
    }

    private static Expression BuildConstant(ConstantAstNode node)
    {
        // Try to infer type from value
        object? value = node.Value;

        if (value is string s)
        {
            // Try to parse as number or bool if possible, using invariant culture for consistency
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                ? Expression.Constant(i)
                : double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d)
                ? Expression.Constant(d)
                : bool.TryParse(s, out var b) ? Expression.Constant(b) : Expression.Constant(s);
        }

        return Expression.Constant(value, value?.GetType() ?? typeof(object));
    }

    private Expression BuildMethodCall(MethodCallAstNode node, ParameterExpression param)
    {
        Expression target = node.Target != null
            ? Build(node.Target, param)
            : throw new ExpressionEvaluationException("Method call must have a target", typeof(T), node.MethodName);

        var argExprs = node.Arguments.Select(arg => Build(arg, param)).ToArray();
        var candidates = target.Type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == node.MethodName && m.GetParameters().Length == argExprs.Length)
            .ToArray();

        if (candidates.Length == 0)
            throw new ExpressionEvaluationException($"Method '{node.MethodName}' not found", target.Type, node.MethodName);

        var compatible = candidates
            .Select(method => new
            {
                Method = method,
                Parameters = method.GetParameters(),
            })
            .Where(candidate => candidate.Parameters
                .Select((parameter, index) => GetCompatibilityScore(argExprs[index], parameter.ParameterType))
                .All(score => score >= 0))
            .Select(candidate => new
            {
                candidate.Method,
                candidate.Parameters,
                Score = candidate.Parameters
                    .Select((parameter, index) => GetCompatibilityScore(argExprs[index], parameter.ParameterType))
                    .Sum()
            })
            .ToArray();

        if (compatible.Length == 0)
            throw new ExpressionEvaluationException($"No compatible overload for method '{node.MethodName}' was found", target.Type, node.MethodName);

        int maxScore = compatible.Max(candidate => candidate.Score);
        var bestMatches = compatible.Where(candidate => candidate.Score == maxScore).ToArray();

        if (bestMatches.Length != 1)
            throw new ExpressionEvaluationException($"Method call '{node.MethodName}' is ambiguous", target.Type, node.MethodName);

        var method = bestMatches[0].Method;

        // Convert arguments if needed
        var convertedArgs = method.GetParameters()
            .Select((parameter, index) => ConvertIfNeeded(argExprs[index], parameter.ParameterType))
            .ToArray();

        return Expression.Call(target, method, convertedArgs);
    }

    private static Expression ConvertIfNeeded(Expression argument, Type targetType)
        => argument.Type == targetType ? argument : Expression.Convert(argument, targetType);

    private static int GetCompatibilityScore(Expression argument, Type parameterType)
    {
        if (argument.Type == parameterType)
            return 3;

        if (parameterType.IsAssignableFrom(argument.Type))
            return 2;

        if (argument is ConstantExpression { Value: null })
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null ? 1 : -1;

        return IsNumericType(argument.Type) && IsNumericType(parameterType) ? 0 : -1;
    }

    private static bool IsNumericType(Type type)
    {
        Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        return Type.GetTypeCode(effectiveType) is TypeCode.Byte
            or TypeCode.SByte
            or TypeCode.Int16
            or TypeCode.UInt16
            or TypeCode.Int32
            or TypeCode.UInt32
            or TypeCode.Int64
            or TypeCode.UInt64
            or TypeCode.Single
            or TypeCode.Double
            or TypeCode.Decimal;
    }
}
