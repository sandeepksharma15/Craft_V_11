using System.Linq.Expressions;
using Craft.Expressions.Comparison;
using Craft.Expressions.Rewriting;

namespace Craft.Expressions.Extensions;

public static class PredicateExpressionExtensions
{
    extension<T>(Expression<Func<T, bool>> expression)
    {
        /// <summary>
        /// Removes the specified condition from the current predicate expression.
        /// </summary>
        public Expression<Func<T, bool>>? RemoveCondition(Expression<Func<T, bool>> condition)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(condition);

            Expression alignedCondition = AlignConditionBody(condition, expression.Parameters[0]);
            if (expression.Body.IsSemanticallyEquivalentTo(alignedCondition))
                return null;

            Expression modifiedBody = new ConditionRemovingVisitor(alignedCondition).Visit(expression.Body)!;
            return Expression.Lambda<Func<T, bool>>(modifiedBody, expression.Parameters);
        }

        /// <summary>
        /// Removes the specified conditions from the current predicate expression.
        /// </summary>
        public Expression<Func<T, bool>>? RemoveConditions(params Expression<Func<T, bool>>[] conditions)
        {
            ArgumentNullException.ThrowIfNull(expression);

            if (conditions is null || conditions.Length == 0)
                return expression;

            Expression body = expression.Body;

            foreach (Expression<Func<T, bool>> condition in conditions)
            {
                ArgumentNullException.ThrowIfNull(condition);

                Expression alignedCondition = AlignConditionBody(condition, expression.Parameters[0]);

                if (body.IsSemanticallyEquivalentTo(alignedCondition))
                    return null;

                body = new ConditionRemovingVisitor(alignedCondition).Visit(body)!;
            }

            return Expression.Lambda<Func<T, bool>>(body, expression.Parameters);
        }

        /// <summary>
        /// Replaces a specific condition within the current predicate expression.
        /// </summary>
        public Expression<Func<T, bool>> ReplaceCondition(Expression<Func<T, bool>> oldCondition, Expression<Func<T, bool>> newCondition)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(oldCondition);
            ArgumentNullException.ThrowIfNull(newCondition);

            Expression alignedOldCondition = AlignConditionBody(oldCondition, expression.Parameters[0]);
            Expression alignedNewCondition = AlignConditionBody(newCondition, expression.Parameters[0]);
            Expression modifiedBody = new ConditionReplacingVisitor(alignedOldCondition, alignedNewCondition).Visit(expression.Body)!;

            return Expression.Lambda<Func<T, bool>>(modifiedBody, expression.Parameters);
        }
    }

    extension(Expression expression)
    {
        /// <summary>
        /// Determines whether two expressions are semantically equivalent.
        /// </summary>
        public bool IsSemanticallyEquivalentTo(Expression other)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(other);

            Expression left = expression.CanReduce ? expression.Reduce() : expression;
            Expression right = other.CanReduce ? other.Reduce() : other;

            return new ExpressionSemanticEqualityComparer().Equals(left, right);
        }
    }

    private static Expression AlignConditionBody<T>(Expression<Func<T, bool>> condition, ParameterExpression targetParameter)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(targetParameter);

        return condition.Parameters.Count != 1
            ? throw new ArgumentException("Predicate conditions must declare exactly one parameter.", nameof(condition))
            : ExpressionParameterRebinder.Rebind(condition.Body, condition.Parameters[0], targetParameter);
    }

    private sealed class ConditionRemovingVisitor(Expression conditionToRemove) : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.AndAlso)
            {
                if (node.Left.IsSemanticallyEquivalentTo(conditionToRemove))
                    return Visit(node.Right);

                if (node.Right.IsSemanticallyEquivalentTo(conditionToRemove))
                    return Visit(node.Left);
            }

            return base.VisitBinary(node);
        }
    }

    private sealed class ConditionReplacingVisitor(Expression oldCondition, Expression newCondition) : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.AndAlso)
            {
                if (node.Left.IsSemanticallyEquivalentTo(oldCondition))
                    return Expression.AndAlso(newCondition, Visit(node.Right));

                if (node.Right.IsSemanticallyEquivalentTo(oldCondition))
                    return Expression.AndAlso(Visit(node.Left), newCondition);
            }

            return base.VisitBinary(node);
        }
    }
}
