using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Analysis;
using McdcTool.Models;

namespace McdcTool.Tests;

public class ExpressionParserTests
{
    private readonly ExpressionParser _parser = new();

    private ExpressionSyntax ParseExpression(string code)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ void M() {{ if ({code}) {{}} }} }}");
        var root = tree.GetRoot();
        var ifStatement = root.DescendantNodes().OfType<IfStatementSyntax>().First();
        return ifStatement.Condition;
    }

    [Fact]
    public void Parse_SimpleAnd_ReturnsAndExpression()
    {
        var syntax = ParseExpression("a && b");
        var (expression, conditions) = _parser.Parse(syntax);

        Assert.IsType<AndExpression>(expression);
        Assert.Equal(2, conditions.Count);
        Assert.Equal("A", conditions[0].Label);
        Assert.Equal("B", conditions[1].Label);
        Assert.Equal("a", conditions[0].OriginalText);
        Assert.Equal("b", conditions[1].OriginalText);
    }

    [Fact]
    public void Parse_SimpleOr_ReturnsOrExpression()
    {
        var syntax = ParseExpression("a || b");
        var (expression, conditions) = _parser.Parse(syntax);

        Assert.IsType<OrExpression>(expression);
        Assert.Equal(2, conditions.Count);
    }

    [Fact]
    public void Parse_NotExpression_ReturnsNotExpression()
    {
        var syntax = ParseExpression("!a && b");
        var (expression, conditions) = _parser.Parse(syntax);

        Assert.IsType<AndExpression>(expression);
        var and = (AndExpression)expression;
        Assert.IsType<NotExpression>(and.Left);
        Assert.Equal(2, conditions.Count);
    }

    [Fact]
    public void Parse_NestedExpression_ReturnsCorrectTree()
    {
        var syntax = ParseExpression("a && (b || c)");
        var (expression, conditions) = _parser.Parse(syntax);

        Assert.IsType<AndExpression>(expression);
        var and = (AndExpression)expression;
        Assert.IsType<Condition>(and.Left);
        Assert.IsType<OrExpression>(and.Right);
        Assert.Equal(3, conditions.Count);
        Assert.Equal("A", conditions[0].Label);
        Assert.Equal("B", conditions[1].Label);
        Assert.Equal("C", conditions[2].Label);
    }

    [Fact]
    public void Parse_ComparisonOperators_TreatedAsAtomicConditions()
    {
        var syntax = ParseExpression("x > 5 && y < 10");
        var (expression, conditions) = _parser.Parse(syntax);

        Assert.IsType<AndExpression>(expression);
        Assert.Equal(2, conditions.Count);
        Assert.Equal("x > 5", conditions[0].OriginalText);
        Assert.Equal("y < 10", conditions[1].OriginalText);
    }
}
