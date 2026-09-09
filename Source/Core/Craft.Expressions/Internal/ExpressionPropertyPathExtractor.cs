using System.Linq.Expressions;

namespace Craft.Expressions.Internal;

internal static class ExpressionPropertyPathExtractor
{
    public static string GetFullPropertyPath(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        List<string> parts = [];
        Expression? current = expression;

        while (current is not null)
        {
            switch (current)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
                    current = unary.Operand;
                    break;

                case MemberExpression member:
                    parts.Add(member.Member.Name);
                    current = member.Expression;
                    break;

                case MethodCallExpression methodCall when IsNullConditionalAccess(methodCall):
                    MemberExpression? memberArgument = GetMemberFromNullConditional(methodCall);
                    if (memberArgument is null)
                    {
                        current = null;
                        break;
                    }

                    parts.Add(memberArgument.Member.Name);
                    current = memberArgument.Expression;
                    break;

                case ParameterExpression:
                    current = null;
                    break;

                default:
                    throw new ArgumentException(
                        $"Expression type '{current.GetType().Name}' is not supported for property path extraction. Expected a property or field access expression.",
                        nameof(expression));
            }
        }

        if (parts.Count == 0)
            throw new ArgumentException("Could not extract property path from expression.", nameof(expression));

        parts.Reverse();
        return string.Join(".", parts);
    }

    private static bool IsNullConditionalAccess(MethodCallExpression methodCall)
        => methodCall.Method.Name == "get_Item"
           || (methodCall.Object is null && methodCall.Arguments.Count > 0);

    private static MemberExpression? GetMemberFromNullConditional(MethodCallExpression methodCall)
        => methodCall.Arguments.Count > 0 && methodCall.Arguments[0] is MemberExpression member ? member : null;
}
