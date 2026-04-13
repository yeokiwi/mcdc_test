using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class CoverageReportTests
{
    [Fact]
    public void FileCoverageInfo_PercentCalculations_Correct()
    {
        var info = new FileCoverageInfo
        {
            FilePath = "test.cs",
            TotalStatements = 10,
            CoveredStatements = 7,
            TotalDecisions = 4,
            CoveredDecisions = 3
        };

        Assert.Equal(70.0, info.StatementCoveragePercent);
        Assert.Equal(75.0, info.DecisionCoveragePercent);
    }

    [Fact]
    public void FileCoverageInfo_ZeroTotal_ReturnsZeroPercent()
    {
        var info = new FileCoverageInfo { TotalStatements = 0, TotalDecisions = 0 };

        Assert.Equal(0.0, info.StatementCoveragePercent);
        Assert.Equal(0.0, info.DecisionCoveragePercent);
    }

    [Fact]
    public void CoverageReport_AggregatesAcrossFiles()
    {
        var report = new CoverageReport
        {
            Files = new List<FileCoverageInfo>
            {
                new() { TotalStatements = 10, CoveredStatements = 8, TotalDecisions = 2, CoveredDecisions = 2 },
                new() { TotalStatements = 20, CoveredStatements = 10, TotalDecisions = 3, CoveredDecisions = 1 }
            }
        };

        Assert.Equal(30, report.TotalStatements);
        Assert.Equal(18, report.CoveredStatements);
        Assert.Equal(60.0, report.StatementCoveragePercent);
        Assert.Equal(5, report.TotalDecisions);
        Assert.Equal(3, report.CoveredDecisions);
        Assert.Equal(60.0, report.DecisionCoveragePercent);
    }

    [Fact]
    public void CoverageReport_EmptyFiles_ReturnsZeroPercent()
    {
        var report = new CoverageReport { Files = new() };

        Assert.Equal(0, report.TotalStatements);
        Assert.Equal(0, report.CoveredStatements);
        Assert.Equal(0.0, report.StatementCoveragePercent);
        Assert.Equal(0.0, report.DecisionCoveragePercent);
    }
}

public class CoverageOptionsTests
{
    [Fact]
    public void AnyDecisionLevel_DecisionTrue_ReturnsTrue()
    {
        var opts = new CoverageOptions { DecisionCoverage = true };
        Assert.True(opts.AnyDecisionLevel);
    }

    [Fact]
    public void AnyDecisionLevel_McdcTrue_ReturnsTrue()
    {
        var opts = new CoverageOptions { McdcCoverage = true };
        Assert.True(opts.AnyDecisionLevel);
    }

    [Fact]
    public void AnyDecisionLevel_OnlyStatement_ReturnsFalse()
    {
        var opts = new CoverageOptions { StatementCoverage = true };
        Assert.False(opts.AnyDecisionLevel);
    }

    [Fact]
    public void AnyDecisionLevel_AllFalse_ReturnsFalse()
    {
        var opts = new CoverageOptions();
        Assert.False(opts.AnyDecisionLevel);
    }
}

public class LineCoverageInfoTests
{
    [Fact]
    public void LineCoverageInfo_DefaultValues()
    {
        var line = new LineCoverageInfo();

        Assert.Equal(0, line.LineNumber);
        Assert.Equal("", line.SourceText);
        Assert.False(line.IsExecutableStatement);
        Assert.False(line.IsDecisionLine);
        Assert.False(line.StatementCovered);
        Assert.False(line.DecisionCovered);
        Assert.False(line.McdcCovered);
    }

    [Fact]
    public void FileCoverageDisplay_DefaultValues()
    {
        var display = new FileCoverageDisplay();

        Assert.Equal("", display.FilePath);
        Assert.Empty(display.Lines);
    }

    [Fact]
    public void LineCoverageInfo_SetProperties()
    {
        var line = new LineCoverageInfo
        {
            LineNumber = 5,
            SourceText = "if (a && b)",
            IsExecutableStatement = true,
            IsDecisionLine = true,
            StatementCovered = true,
            DecisionCovered = true,
            McdcCovered = false
        };

        Assert.Equal(5, line.LineNumber);
        Assert.Equal("if (a && b)", line.SourceText);
        Assert.True(line.IsExecutableStatement);
        Assert.True(line.IsDecisionLine);
        Assert.True(line.StatementCovered);
        Assert.True(line.DecisionCovered);
        Assert.False(line.McdcCovered);
    }
}

public class BooleanExpressionTests
{
    [Fact]
    public void Condition_Evaluate_ReturnsDictionaryValue()
    {
        var cond = new Condition("x") { Label = "A" };
        Assert.True(cond.Evaluate(new() { ["A"] = true }));
        Assert.False(cond.Evaluate(new() { ["A"] = false }));
    }

    [Fact]
    public void Condition_GetConditions_ReturnsSelf()
    {
        var cond = new Condition("x") { Label = "A" };
        var conditions = cond.GetConditions();
        Assert.Single(conditions);
        Assert.Same(cond, conditions[0]);
    }

    [Fact]
    public void Condition_ToDisplayString_ReturnsLabel()
    {
        var cond = new Condition("x") { Label = "A" };
        Assert.Equal("A", cond.ToDisplayString());
    }

    [Fact]
    public void AndExpression_EvaluateAllCombinations()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new AndExpression(a, b);

        Assert.False(expr.Evaluate(new() { ["A"] = false, ["B"] = false }));
        Assert.False(expr.Evaluate(new() { ["A"] = false, ["B"] = true }));
        Assert.False(expr.Evaluate(new() { ["A"] = true, ["B"] = false }));
        Assert.True(expr.Evaluate(new() { ["A"] = true, ["B"] = true }));
    }

    [Fact]
    public void AndExpression_GetConditions_ReturnsBothSides()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new AndExpression(a, b);

        Assert.Equal(2, expr.GetConditions().Count);
    }

    [Fact]
    public void AndExpression_ToDisplayString()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new AndExpression(a, b);

        Assert.Equal("(A && B)", expr.ToDisplayString());
    }

    [Fact]
    public void OrExpression_EvaluateAllCombinations()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new OrExpression(a, b);

        Assert.False(expr.Evaluate(new() { ["A"] = false, ["B"] = false }));
        Assert.True(expr.Evaluate(new() { ["A"] = false, ["B"] = true }));
        Assert.True(expr.Evaluate(new() { ["A"] = true, ["B"] = false }));
        Assert.True(expr.Evaluate(new() { ["A"] = true, ["B"] = true }));
    }

    [Fact]
    public void OrExpression_GetConditions_ReturnsBothSides()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new OrExpression(a, b);

        Assert.Equal(2, expr.GetConditions().Count);
    }

    [Fact]
    public void OrExpression_ToDisplayString()
    {
        var a = new Condition("a") { Label = "A" };
        var b = new Condition("b") { Label = "B" };
        var expr = new OrExpression(a, b);

        Assert.Equal("(A || B)", expr.ToDisplayString());
    }

    [Fact]
    public void NotExpression_Evaluate()
    {
        var a = new Condition("a") { Label = "A" };
        var expr = new NotExpression(a);

        Assert.True(expr.Evaluate(new() { ["A"] = false }));
        Assert.False(expr.Evaluate(new() { ["A"] = true }));
    }

    [Fact]
    public void NotExpression_GetConditions_ReturnsOperandConditions()
    {
        var a = new Condition("a") { Label = "A" };
        var expr = new NotExpression(a);

        Assert.Single(expr.GetConditions());
    }

    [Fact]
    public void NotExpression_ToDisplayString()
    {
        var a = new Condition("a") { Label = "A" };
        var expr = new NotExpression(a);

        Assert.Equal("!A", expr.ToDisplayString());
    }
}

public class McdcResultModelTests
{
    [Fact]
    public void TruthTableRow_StoresValues()
    {
        var values = new Dictionary<string, bool> { ["A"] = true, ["B"] = false };
        var row = new TruthTableRow(3, values, true);

        Assert.Equal(3, row.RowIndex);
        Assert.True(row.ConditionValues["A"]);
        Assert.False(row.ConditionValues["B"]);
        Assert.True(row.DecisionOutcome);
    }

    [Fact]
    public void TruthTable_StoresLabelsAndRows()
    {
        var labels = new List<string> { "A", "B" };
        var rows = new List<TruthTableRow>
        {
            new(0, new() { ["A"] = false, ["B"] = false }, false)
        };
        var table = new TruthTable(labels, rows);

        Assert.Equal(2, table.ConditionLabels.Count);
        Assert.Single(table.Rows);
    }

    [Fact]
    public void IndependencePair_StoresData()
    {
        var trueRow = new TruthTableRow(1, new() { ["A"] = true }, true);
        var falseRow = new TruthTableRow(0, new() { ["A"] = false }, false);
        var pair = new IndependencePair("A", trueRow, falseRow);

        Assert.Equal("A", pair.ConditionLabel);
        Assert.Same(trueRow, pair.TrueRow);
        Assert.Same(falseRow, pair.FalseRow);
    }

    [Fact]
    public void McdcTestCase_StoresRowAndConditions()
    {
        var row = new TruthTableRow(0, new() { ["A"] = true }, true);
        var covers = new List<string> { "A", "B" };
        var tc = new McdcTestCase(row, covers);

        Assert.Same(row, tc.Row);
        Assert.Equal(2, tc.CoversConditions.Count);
    }

    [Fact]
    public void McdcResult_StoresAllFields()
    {
        var table = new TruthTable(new() { "A" }, new());
        var pairs = new Dictionary<string, List<IndependencePair>> { ["A"] = new() };
        var testSet = new List<McdcTestCase>();
        var mappings = new Dictionary<string, string> { ["A"] = "x" };

        var result = new McdcResult("x", "(A)", table, pairs, testSet, mappings);

        Assert.Equal("x", result.ExpressionText);
        Assert.Equal("(A)", result.DisplayExpression);
        Assert.Same(table, result.TruthTable);
        Assert.Same(pairs, result.IndependencePairs);
        Assert.Same(testSet, result.MinimalTestSet);
        Assert.Same(mappings, result.ConditionMappings);
    }
}

public class DecisionInfoTests
{
    [Fact]
    public void Constructor_StoresAllProperties()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { void M() { if (true) {} } }");
        var ifStmt = tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>().First();

        var info = new DecisionInfo("test.cs", 10, 5, "if", "a && b", ifStmt.Condition);

        Assert.Equal("test.cs", info.FilePath);
        Assert.Equal(10, info.LineNumber);
        Assert.Equal(5, info.Column);
        Assert.Equal("if", info.StatementType);
        Assert.Equal("a && b", info.ExpressionText);
        Assert.NotNull(info.ExpressionSyntax);
    }

    [Fact]
    public void LocationString_FormatsCorrectly()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { void M() { if (true) {} } }");
        var ifStmt = tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>().First();

        var info = new DecisionInfo("/path/to/test.cs", 42, 8, "if", "a && b", ifStmt.Condition);

        Assert.Equal("test.cs:42:8", info.LocationString);
    }
}
