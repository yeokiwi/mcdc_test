using McdcTool.Analysis;
using McdcTool.Models;

namespace McdcTool.Tests;

public class McdcAnalyzerTests
{
    private readonly McdcAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_TwoConditionAnd_FindsIndependencePairs()
    {
        // a && b
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expression = new AndExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var result = _analyzer.Analyze(expression, conditions, "a && b");

        // Both conditions should have independence pairs
        Assert.True(result.IndependencePairs["A"].Count > 0, "Condition A should have independence pairs");
        Assert.True(result.IndependencePairs["B"].Count > 0, "Condition B should have independence pairs");

        // For A: when B=T, flipping A changes outcome (row 1,3 -> FT=F, TT=T)
        var pairA = result.IndependencePairs["A"][0];
        Assert.NotEqual(pairA.TrueRow.DecisionOutcome, pairA.FalseRow.DecisionOutcome);

        // For B: when A=T, flipping B changes outcome (row 2,3 -> TF=F, TT=T)
        var pairB = result.IndependencePairs["B"][0];
        Assert.NotEqual(pairB.TrueRow.DecisionOutcome, pairB.FalseRow.DecisionOutcome);
    }

    [Fact]
    public void Analyze_TwoConditionAnd_MinimalTestSetHas3Cases()
    {
        // a && b: needs N+1 = 3 test cases
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expression = new AndExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var result = _analyzer.Analyze(expression, conditions, "a && b");

        // Should have exactly 3 test cases (N+1 for 2 conditions)
        Assert.Equal(3, result.MinimalTestSet.Count);
    }

    [Fact]
    public void Analyze_TwoConditionOr_MinimalTestSetHas3Cases()
    {
        // a || b: needs N+1 = 3 test cases
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expression = new OrExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var result = _analyzer.Analyze(expression, conditions, "a || b");

        Assert.Equal(3, result.MinimalTestSet.Count);
    }

    [Fact]
    public void Analyze_ThreeConditionMixed_MinimalTestSetHas4Cases()
    {
        // a && (b || c): needs N+1 = 4 test cases
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var c = new Condition("c") { Label = "C" };
        var expression = new AndExpression(a, new OrExpression(b, c));
        var conditions = new List<Condition> { a, b, c };

        var result = _analyzer.Analyze(expression, conditions, "a && (b || c)");

        // Should have at most N+1 = 4 test cases
        Assert.True(result.MinimalTestSet.Count <= 4,
            $"Expected at most 4 test cases, got {result.MinimalTestSet.Count}");
        // Should have at least N+1 = 4 (all conditions must be covered)
        Assert.True(result.MinimalTestSet.Count >= 4,
            $"Expected at least 4 test cases, got {result.MinimalTestSet.Count}");
    }

    [Fact]
    public void Analyze_IndependencePairsAreValid()
    {
        // Verify that independence pairs satisfy the MC/DC criteria
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var c = new Condition("c") { Label = "C" };
        var expression = new AndExpression(a, new OrExpression(b, c));
        var conditions = new List<Condition> { a, b, c };

        var result = _analyzer.Analyze(expression, conditions, "a && (b || c)");

        foreach (var (label, pairs) in result.IndependencePairs)
        {
            foreach (var pair in pairs)
            {
                // The target condition must have different values
                Assert.NotEqual(
                    pair.TrueRow.ConditionValues[label],
                    pair.FalseRow.ConditionValues[label]);

                // The decision outcome must differ
                Assert.NotEqual(pair.TrueRow.DecisionOutcome, pair.FalseRow.DecisionOutcome);

                // All other conditions must be the same
                var otherLabels = result.TruthTable.ConditionLabels.Where(l => l != label);
                foreach (var other in otherLabels)
                {
                    Assert.Equal(
                        pair.TrueRow.ConditionValues[other],
                        pair.FalseRow.ConditionValues[other]);
                }
            }
        }
    }

    [Fact]
    public void Analyze_AllConditionsCovered()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var c = new Condition("c") { Label = "C" };
        var expression = new AndExpression(a, new OrExpression(b, c));
        var conditions = new List<Condition> { a, b, c };

        var result = _analyzer.Analyze(expression, conditions, "a && (b || c)");

        // Every condition should appear in at least one test case's coverage
        var coveredConditions = result.MinimalTestSet
            .SelectMany(tc => tc.CoversConditions)
            .Distinct()
            .ToHashSet();

        foreach (var label in result.TruthTable.ConditionLabels)
        {
            Assert.Contains(label, coveredConditions);
        }
    }

    [Fact]
    public void Analyze_ConditionMappings_AreCorrect()
    {
        var a = new Condition("x > 5") { Label = "A" };
        var b = new Condition("y < 10") { Label = "B" };
        var expression = new AndExpression(a, b);
        var conditions = new List<Condition> { a, b };

        var result = _analyzer.Analyze(expression, conditions, "x > 5 && y < 10");

        Assert.Equal("x > 5", result.ConditionMappings["A"]);
        Assert.Equal("y < 10", result.ConditionMappings["B"]);
    }
}
