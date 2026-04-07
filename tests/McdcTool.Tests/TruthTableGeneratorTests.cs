using McdcTool.Core.Analysis;
using McdcTool.Core.Models;

namespace McdcTool.Tests;

public class TruthTableGeneratorTests
{
    private readonly TruthTableGenerator _generator = new();

    [Fact]
    public void Generate_TwoConditionAnd_Returns4Rows()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expression = new AndExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var table = _generator.Generate(expression, conditions);

        Assert.Equal(2, table.ConditionLabels.Count);
        Assert.Equal(4, table.Rows.Count);

        // A=F, B=F -> F
        Assert.False(table.Rows[0].ConditionValues["A"]);
        Assert.False(table.Rows[0].ConditionValues["B"]);
        Assert.False(table.Rows[0].DecisionOutcome);

        // A=F, B=T -> F
        Assert.False(table.Rows[1].DecisionOutcome);

        // A=T, B=F -> F
        Assert.False(table.Rows[2].DecisionOutcome);

        // A=T, B=T -> T
        Assert.True(table.Rows[3].ConditionValues["A"]);
        Assert.True(table.Rows[3].ConditionValues["B"]);
        Assert.True(table.Rows[3].DecisionOutcome);
    }

    [Fact]
    public void Generate_TwoConditionOr_Returns4Rows()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expression = new OrExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var table = _generator.Generate(expression, conditions);

        // A=F, B=F -> F
        Assert.False(table.Rows[0].DecisionOutcome);
        // A=F, B=T -> T
        Assert.True(table.Rows[1].DecisionOutcome);
        // A=T, B=F -> T
        Assert.True(table.Rows[2].DecisionOutcome);
        // A=T, B=T -> T
        Assert.True(table.Rows[3].DecisionOutcome);
    }

    [Fact]
    public void Generate_ThreeConditions_Returns8Rows()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var c = new Condition("c") { Label = "C" };
        // a && (b || c)
        var expression = new AndExpression(a, new OrExpression(b, c));
        var conditions = new List<Condition> { a, b, c };

        var table = _generator.Generate(expression, conditions);

        Assert.Equal(8, table.Rows.Count);
        Assert.Equal(3, table.ConditionLabels.Count);
    }

    [Fact]
    public void Generate_NotExpression_EvaluatesCorrectly()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        // !a && b
        var expression = new AndExpression(new NotExpression(a), b);
        var conditions = new List<Condition> { a, b };

        var table = _generator.Generate(expression, conditions);

        // A=F, B=F -> !F && F = T && F = F
        Assert.False(table.Rows[0].DecisionOutcome);
        // A=F, B=T -> !F && T = T && T = T
        Assert.True(table.Rows[1].DecisionOutcome);
        // A=T, B=F -> !T && F = F && F = F
        Assert.False(table.Rows[2].DecisionOutcome);
        // A=T, B=T -> !T && T = F && T = F
        Assert.False(table.Rows[3].DecisionOutcome);
    }
}
