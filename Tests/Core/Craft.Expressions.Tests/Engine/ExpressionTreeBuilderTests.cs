using System.Linq.Expressions;
using Craft.Expressions.Ast;
using Craft.Expressions.Engine;
using Craft.Expressions.Exceptions;

namespace Craft.Expressions.Tests.Engine;

public class ExpressionTreeBuilderTests
{
    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public TestClass? Child { get; set; }
        public int ScoreField;
    }

    private class MethodHost
    {
        public string Echo(string value) => value;
        public string TakesObject(object value) => value.ToString() ?? string.Empty;
        public double DoubleInput(double value) => value;
        public int NeedsInt(int value) => value;
        public string Ambiguous(string? value) => value ?? string.Empty;
        public string Ambiguous(Uri? value) => value?.ToString() ?? string.Empty;
    }

    private class MethodTargetContainer
    {
        public MethodHost Target { get; set; } = new();
    }

    private static ParameterExpression Param => Expression.Parameter(typeof(TestClass), "x");
    private static ExpressionTreeBuilder<TestClass> Builder => new();
    private static ParameterExpression MethodParam => Expression.Parameter(typeof(MethodTargetContainer), "x");
    private static ExpressionTreeBuilder<MethodTargetContainer> MethodBuilder => new();

    [Fact]
    public void Build_MemberAstNode_ReturnsMemberExpression()
    {
        // Arrange
        var node = new MemberAstNode(["Name"]);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MemberExpression>(expr, exactMatch: false);
        Assert.Equal("Name", (typed).Member.Name);
    }

    [Fact]
    public void Build_MemberAstNode_Nested_ReturnsMemberExpression()
    {
        // Arrange
        var node = new MemberAstNode(["Child", "Name"]);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MemberExpression>(expr, exactMatch: false);
        Assert.Equal("Name", (typed).Member.Name);
    }

    [Fact]
    public void Build_ConstantAstNode_String_Int_Bool_Null()
    {
        // Arrange
        var stringNode = new ConstantAstNode("hello");
        var intNode = new ConstantAstNode("42");
        var boolNode = new ConstantAstNode("true");
        var nullNode = new ConstantAstNode(null!);

        // Act
        var exprString = Builder.Build(stringNode, Param);
        var exprInt = Builder.Build(intNode, Param);
        var exprBool = Builder.Build(boolNode, Param);
        var exprNull = Builder.Build(nullNode, Param);

        // Assert
        Assert.Equal("hello", ((ConstantExpression)exprString).Value);
        Assert.Equal(42, ((ConstantExpression)exprInt).Value);
        Assert.Equal(true, ((ConstantExpression)exprBool).Value);
        Assert.Null(((ConstantExpression)exprNull).Value);
    }

    [Fact]
    public void Build_ConstantAstNode_Double()
    {
        // Arrange
        var doubleNode = new ConstantAstNode("3.14");

        // Act
        var expr = Builder.Build(doubleNode, Param);

        // Assert
        Assert.Equal(3.14, ((ConstantExpression)expr).Value);
    }

    [Fact]
    public void Build_ConstantAstNode_ThousandsDouble_UsesInvariantCulture()
    {
        // Arrange
        var node = new ConstantAstNode("1,234.5");

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var constant = Assert.IsType<ConstantExpression>(expr);
        Assert.Equal(1234.5, constant.Value);
    }

    [Fact]
    public void Build_ConstantAstNode_NonStringValue_PreservesOriginalType()
    {
        // Arrange
        var node = new ConstantAstNode(7m);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var constant = Assert.IsType<ConstantExpression>(expr);
        Assert.Equal(typeof(decimal), constant.Type);
        Assert.Equal(7m, constant.Value);
    }

    [Fact]
    public void Build_BinaryAstNode_AllOperators()
    {
        // Arrange
        var leftInt = new MemberAstNode(["Age"]);
        var rightInt = new ConstantAstNode("18");
        var leftBool = new MemberAstNode(["IsActive"]);
        var rightBool = new ConstantAstNode("true");
        var opCases = new[]
        {
            ("==", leftInt, rightInt),
            ("!=", leftInt, rightInt),
            (">", leftInt, rightInt),
            (">=", leftInt, rightInt),
            ("<", leftInt, rightInt),
            ("<=", leftInt, rightInt),
            ("&&", leftBool, rightBool),
            ("||", leftBool, rightBool)
        };

        // Act & Assert
        foreach (var (op, left, right) in opCases)
        {
            var node = new BinaryAstNode(op, left, right);
            var expr = Builder.Build(node, Param);
            Assert.IsType<BinaryExpression>(expr, exactMatch: false);
        }
    }

    [Fact]
    public void Build_BinaryAstNode_UnsupportedOperator_Throws()
    {
        // Arrange
        var node = new BinaryAstNode("^^", new ConstantAstNode("1"), new ConstantAstNode("2"));

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => Builder.Build(node, Param));
    }

    [Fact]
    public void Build_UnaryAstNode_Not()
    {
        // Arrange
        var operand = new MemberAstNode(["IsActive"]);
        var node = new UnaryAstNode("!", operand);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<UnaryExpression>(expr);
        Assert.Equal(ExpressionType.Not, (typed).NodeType);
    }

    [Fact]
    public void Build_UnaryAstNode_UnsupportedOperator_Throws()
    {
        // Arrange
        var node = new UnaryAstNode("~", new ConstantAstNode("1"));

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => Builder.Build(node, Param));
    }

    [Fact]
    public void Build_MethodCallAstNode_SingleArg()
    {
        // Arrange
        var target = new MemberAstNode(["Name"]);
        var arg = new ConstantAstNode("h");
        var node = new MethodCallAstNode(target, "Contains", [arg]);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MethodCallExpression>(expr, exactMatch: false);
        Assert.Equal("Contains", (typed).Method.Name);
    }

    [Fact]
    public void Build_MethodCallAstNode_MultipleArgs()
    {
        // Arrange
        var target = new MemberAstNode(["Name"]);
        var args = new List<AstNode> { new ConstantAstNode("a"), new ConstantAstNode("b") };
        var node = new MethodCallAstNode(target, "Replace", args);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MethodCallExpression>(expr, exactMatch: false);
        Assert.Equal("Replace", (typed).Method.Name);
    }

    [Fact]
    public void Build_MethodCallAstNode_NoArgs()
    {
        // Arrange
        var target = new MemberAstNode(["Name"]);
        var node = new MethodCallAstNode(target, "ToLower", []);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MethodCallExpression>(expr, exactMatch: false);
        Assert.Equal("ToLower", (typed).Method.Name);
    }

    [Fact]
    public void Build_MethodCallAstNode_UnsupportedMethod_Throws()
    {
        // Arrange
        var target = new MemberAstNode(["Name"]);
        var node = new MethodCallAstNode(target, "NotAMethod", []);

        // Act & Assert
        Assert.Throws<ExpressionEvaluationException>(() => Builder.Build(node, Param));
    }

    [Fact]
    public void Build_MemberAstNode_Field_ReturnsMemberExpression()
    {
        // Arrange
        var node = new MemberAstNode(["ScoreField"]);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MemberExpression>(expr, exactMatch: false);
        Assert.Equal("ScoreField", typed.Member.Name);
    }

    [Fact]
    public void Build_MemberAstNode_CaseInsensitivePath_ReturnsMemberExpression()
    {
        // Arrange
        var node = new MemberAstNode(["name"]);

        // Act
        var expr = Builder.Build(node, Param);

        // Assert
        var typed = Assert.IsType<MemberExpression>(expr, exactMatch: false);
        Assert.Equal("Name", typed.Member.Name);
    }

    [Fact]
    public void Build_MethodCallAstNode_AssignableArgument_SelectsCompatibleOverload()
    {
        // Arrange
        var target = new MemberAstNode(["Target"]);
        var node = new MethodCallAstNode(target, "TakesObject", [new ConstantAstNode("abc")]);

        // Act
        var expr = MethodBuilder.Build(node, MethodParam);

        // Assert
        var methodCall = Assert.IsType<MethodCallExpression>(expr, exactMatch: false);
        Assert.Equal("TakesObject", methodCall.Method.Name);
        Assert.Equal(typeof(object), methodCall.Method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void Build_MethodCallAstNode_NumericConversion_InsertsConvertExpression()
    {
        // Arrange
        var target = new MemberAstNode(["Target"]);
        var node = new MethodCallAstNode(target, "DoubleInput", [new ConstantAstNode("42")]);

        // Act
        var expr = MethodBuilder.Build(node, MethodParam);

        // Assert
        var methodCall = Assert.IsType<MethodCallExpression>(expr, exactMatch: false);
        Assert.Equal("DoubleInput", methodCall.Method.Name);
        var convert = Assert.IsType<UnaryExpression>(methodCall.Arguments[0]);
        Assert.Equal(ExpressionType.Convert, convert.NodeType);
        Assert.Equal(typeof(double), convert.Type);
    }

    [Fact]
    public void Build_MethodCallAstNode_NoCompatibleOverload_Throws()
    {
        // Arrange
        var target = new MemberAstNode(["Target"]);
        var node = new MethodCallAstNode(target, "NeedsInt", [new ConstantAstNode("true")]);

        // Act
        var ex = Assert.Throws<ExpressionEvaluationException>(() => MethodBuilder.Build(node, MethodParam));

        // Assert
        Assert.Contains("No compatible overload", ex.Message);
    }

    [Fact]
    public void Build_MethodCallAstNode_AmbiguousOverload_Throws()
    {
        // Arrange
        var target = new MemberAstNode(["Target"]);
        var node = new MethodCallAstNode(target, "Ambiguous", [new ConstantAstNode(null!)]);

        // Act
        var ex = Assert.Throws<ExpressionEvaluationException>(() => MethodBuilder.Build(node, MethodParam));

        // Assert
        Assert.Contains("ambiguous", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_MethodCallAstNode_NullTarget_Throws()
    {
        // Arrange
        var node = new MethodCallAstNode(null!, "ToLower", []);

        // Act & Assert
        Assert.Throws<ExpressionEvaluationException>(() => Builder.Build(node, Param));
    }

    [Fact]
    public void Build_MemberAstNode_UnknownMember_Throws()
    {
        // Arrange
        var node = new MemberAstNode(["NotAProp"]);

        // Act & Assert
        Assert.Throws<ExpressionEvaluationException>(() => Builder.Build(node, Param));
    }

    [Fact]
    public void Build_UnknownAstNodeType_Throws()
    {
        // Arrange
        var node = new DummyAstNode();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => Builder.Build(node, Param));
    }

    private class DummyAstNode : AstNode { }
}
