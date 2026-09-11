using Craft.Expressions.Ast;
using Craft.Expressions.Constants;
using Craft.Expressions.Exceptions;
using Craft.Expressions.Tokens;

namespace Craft.Expressions.Engine;

/// <summary>
/// Parses a sequence of tokens into an abstract syntax tree (AST).
/// </summary>
internal class ExpressionStringParser
{
    /// <summary>
    /// The maximum depth of nested expressions to prevent stack overflow attacks.
    /// </summary>
    private const int MaxDepth = 100;

    private int _depth = 0;
    private IEnumerator<Token> _tokens = Enumerable.Empty<Token>().GetEnumerator();
    private Token _current = null!;

    /// <summary>
    /// Parses a sequence of tokens into an AST node.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <returns>The root AST node representing the parsed expression.</returns>
    /// <exception cref="ExpressionParseException">Thrown when the tokens cannot be parsed.</exception>
    public AstNode Parse(IEnumerable<Token> tokens)
    {
        _tokens = tokens.GetEnumerator();

        MoveNext();

        var expr = ParseExpression();

        return _current.Type != TokenType.EndOfInput
            ? throw new ExpressionParseException("Unexpected token at end", _current.Position, _current.Value)
            : expr;
    }

    private void MoveNext()
    {
        _current = _tokens.MoveNext() ? _tokens.Current : new Token(TokenType.EndOfInput, string.Empty, -1);
    }

    // ParseExpression: handles '||'
    private AstNode ParseExpression()
    {
        var left = ParseAnd();

        while (_current.Type == TokenType.Operator && _current.Value == ExpressionOperators.Or)
        {
            var op = _current.Value;

            MoveNext();

            var right = ParseAnd();

            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    // ParseAnd: handles '&&'
    private AstNode ParseAnd()
    {
        var left = ParseEquality();

        while (_current.Type == TokenType.Operator && _current.Value == ExpressionOperators.And)
        {
            var op = _current.Value;

            MoveNext();

            var right = ParseEquality();

            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    // ParseEquality: handles ==, !=
    private AstNode ParseEquality()
    {
        var left = ParseRelational();

        while (_current.Type == TokenType.Operator && (_current.Value == ExpressionOperators.Equal || _current.Value == ExpressionOperators.NotEqual))
        {
            var op = _current.Value;

            MoveNext();

            var right = ParseRelational();

            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    // ParseRelational: handles >, <, >=, <=
    private AstNode ParseRelational()
    {
        var left = ParseUnary();

        while (_current.Type == TokenType.Operator &&
            (_current.Value == ExpressionOperators.GreaterThan || _current.Value == ExpressionOperators.GreaterThanOrEqual ||
             _current.Value == ExpressionOperators.LessThan || _current.Value == ExpressionOperators.LessThanOrEqual))
        {
            var op = _current.Value;

            MoveNext();

            var right = ParseUnary();

            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    // ParseUnary: handles !
    private AstNode ParseUnary()
    {
        if (_current.Type == TokenType.Operator && _current.Value == ExpressionOperators.Not)
        {
            var op = _current.Value;

            MoveNext();

            var operand = ParseUnary();

            return new UnaryAstNode(op, operand);
        }

        return ParsePrimary();
    }

    // ParsePrimary: identifiers, literals, method calls, member access, parentheses
    private AstNode ParsePrimary()
    {
        // Parenthesized expression - check depth to prevent stack overflow
        if (_current.Type == TokenType.OpenParen)
        {
            if (++_depth > MaxDepth)
                throw new ExpressionParseException($"Expression depth exceeds maximum of {MaxDepth}", _current.Position, _current.Value);

            try
            {
                MoveNext();

                var expr = ParseExpression();

                if (_current.Type != TokenType.CloseParen)
                    throw new ExpressionParseException("Expected ')'", _current.Position, _current.Value);

                MoveNext();

                return expr;
            }
            finally
            {
                _depth--;
            }
        }

        // Identifier: member access and chained method calls
        if (_current.Type == TokenType.Identifier)
        {
            AstNode node = new MemberAstNode([_current.Value]);
            MoveNext();

            while (_current.Type == TokenType.Dot)
            {
                MoveNext();

                if (_current.Type != TokenType.Identifier)
                    throw new ExpressionParseException("Expected identifier after '.'", _current.Position, _current.Value);

                var segment = _current.Value;
                MoveNext();

                if (_current.Type == TokenType.OpenParen)
                {
                    MoveNext();

                    var args = new List<AstNode>();
                    if (_current.Type != TokenType.CloseParen)
                        while (true)
                        {
                            args.Add(ParseExpression());

                            if (_current.Type == TokenType.Comma)
                                MoveNext();
                            else
                                break;
                        }

                    if (_current.Type != TokenType.CloseParen)
                        throw new ExpressionParseException("Expected ')'", _current.Position, _current.Value);

                    MoveNext();
                    node = new MethodCallAstNode(node, segment, args);
                    continue;
                }

                if (node is not MemberAstNode memberNode)
                    throw new ExpressionParseException("Expected method call after expression target", _current.Position, _current.Value);

                node = new MemberAstNode([.. memberNode.MemberPath, segment]);
            }

            return node;
        }

        // String literal
        if (_current.Type == TokenType.StringLiteral)
        {
            var value = _current.Value;
            MoveNext();
            return new ConstantAstNode(value);
        }

        // Number literal
        if (_current.Type == TokenType.NumberLiteral)
        {
            var value = _current.Value;
            MoveNext();
            return new ConstantAstNode(value);
        }

        // Boolean literal
        if (_current.Type == TokenType.BooleanLiteral)
        {
            var value = _current.Value == "true";
            MoveNext();
            return new ConstantAstNode(value);
        }

        // Null literal
        if (_current.Type == TokenType.NullLiteral)
        {
            MoveNext();
            return new ConstantAstNode(null!);
        }

        throw new ExpressionParseException("Unexpected token", _current.Position, _current.Value);
    }
}
