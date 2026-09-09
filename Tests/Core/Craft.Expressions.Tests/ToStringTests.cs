using Craft.Expressions.Ast;
using Craft.Expressions.Tokens;

namespace Craft.Expressions.Tests;

public class ToStringTests
{
    [Fact]
    public void BinaryAstNode_ToStringReturnsFormattedExpression()
    {
        // Arrange
        ConstantAstNode left = new(5);
        ConstantAstNode right = new(10);
        BinaryAstNode node = new(">", left, right);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("(5 > 10)", result);
    }

    [Fact]
    public void UnaryAstNode_ToStringReturnsFormattedExpression()
    {
        // Arrange
        ConstantAstNode operand = new(true);
        UnaryAstNode node = new("!", operand);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("!True", result);
    }

    [Fact]
    public void ConstantAstNode_ToStringReturnsValue()
    {
        // Arrange
        ConstantAstNode stringNode = new("test");
        ConstantAstNode numberNode = new(42);
        ConstantAstNode boolNode = new(true);
        ConstantAstNode nullNode = new(null!);

        // Act & Assert
        Assert.Equal("test", stringNode.ToString());
        Assert.Equal("42", numberNode.ToString());
        Assert.Equal("True", boolNode.ToString());
        Assert.Equal("null", nullNode.ToString());
    }

    [Fact]
    public void MemberAstNode_ToStringReturnsDottedPath()
    {
        // Arrange
        MemberAstNode simpleMember = new(["Name"]);
        MemberAstNode nestedMember = new(["Company", "Address", "City"]);

        // Act & Assert
        Assert.Equal("Name", simpleMember.ToString());
        Assert.Equal("Company.Address.City", nestedMember.ToString());
    }

    [Fact]
    public void MethodCallAstNode_ToStringReturnsMethodCall()
    {
        // Arrange
        MemberAstNode target = new(["Name"]);
        ConstantAstNode arg = new("John");
        MethodCallAstNode node = new(target, "Contains", [arg]);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("Name.Contains(John)", result);
    }

    [Fact]
    public void MethodCallAstNode_ToStringHandlesMultipleArguments()
    {
        // Arrange
        MemberAstNode target = new(["Text"]);
        ConstantAstNode arg1 = new("old");
        ConstantAstNode arg2 = new("new");
        MethodCallAstNode node = new(target, "Replace", [arg1, arg2]);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("Text.Replace(old, new)", result);
    }

    [Fact]
    public void Token_ToStringReturnsFormattedString()
    {
        // Arrange
        Token token = new(TokenType.Identifier, "Name", 0);

        // Act
        var result = token.ToString();

        // Assert
        Assert.Contains("Identifier", result);
        Assert.Contains("Name", result);
        Assert.Contains("0", result);
    }

    [Fact]
    public void ComplexExpression_ToStringShowsStructure()
    {
        // Arrange
        BinaryAstNode left = new(">", new MemberAstNode(["Age"]), new ConstantAstNode(18));
        BinaryAstNode right = new("==", new MemberAstNode(["IsActive"]), new ConstantAstNode(true));
        BinaryAstNode root = new("&&", left, right);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("((Age > 18) && (IsActive == True))", result);
    }
}
