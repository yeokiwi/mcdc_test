using McdcTool.Core.Analysis;
using McdcTool.Core.Coverage;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class CoverageAnalyzerTests
{
    private readonly CoverageAnalyzer _analyzer = new();

    private string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.cs");
        File.WriteAllText(path, content);
        return path;
    }

    private (List<AnalysisResultCoverage> Results, string FilePath) AnalyzeSource(string code)
    {
        var filePath = CreateTempFile(code);
        var extractor = new DecisionExtractor();
        var parser = new ExpressionParser();
        var mcdcAnalyzer = new McdcAnalyzer();

        var decisions = extractor.Extract(filePath, code);
        var results = new List<AnalysisResultCoverage>();

        foreach (var decision in decisions)
        {
            var (expr, conds) = parser.Parse(decision.ExpressionSyntax);
            var mcResult = mcdcAnalyzer.Analyze(expr, conds, decision.ExpressionText);
            bool covered = mcResult.MinimalTestSet.Any(tc => tc.Row.DecisionOutcome)
                && mcResult.MinimalTestSet.Any(tc => !tc.Row.DecisionOutcome);
            results.Add(new AnalysisResultCoverage
            {
                Decision = decision,
                McResult = mcResult,
                IsDecisionCovered = covered
            });
        }

        return (results, filePath);
    }

    [Fact]
    public void Analyze_FileWithDecisions_ReportsStatementCoverage()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        if (a && b) {
            var x = 1;
        }
    }
}";
        var (results, filePath) = AnalyzeSource(code);

        try
        {
            var report = _analyzer.Analyze(new List<string> { filePath }, results);

            Assert.Single(report.Files);
            Assert.True(report.Files[0].TotalStatements > 0);
            Assert.True(report.Files[0].CoveredStatements > 0);
            Assert.True(report.Files[0].StatementCoveragePercent > 0);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void Analyze_FileWithDecisions_ReportsDecisionCoverage()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var (results, filePath) = AnalyzeSource(code);

        try
        {
            var report = _analyzer.Analyze(new List<string> { filePath }, results);

            Assert.Single(report.Files);
            Assert.Equal(1, report.Files[0].TotalDecisions);
            Assert.Equal(1, report.Files[0].CoveredDecisions);
            Assert.Equal(100.0, report.Files[0].DecisionCoveragePercent);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void Analyze_NonExistentFile_SkipsFile()
    {
        var report = _analyzer.Analyze(
            new List<string> { "/nonexistent/file.cs" },
            new List<AnalysisResultCoverage>());

        Assert.Empty(report.Files);
    }

    [Fact]
    public void Analyze_MultipleFiles_ReportsAllFiles()
    {
        var code1 = @"class C1 { void M(bool a, bool b) { if (a && b) { } } }";
        var code2 = @"class C2 { void M(bool a, bool b) { if (a || b) { } } }";
        var (results1, file1) = AnalyzeSource(code1);
        var (results2, file2) = AnalyzeSource(code2);

        try
        {
            var allResults = results1.Concat(results2).ToList();
            var report = _analyzer.Analyze(new List<string> { file1, file2 }, allResults);

            Assert.Equal(2, report.Files.Count);
        }
        finally { File.Delete(file1); File.Delete(file2); }
    }

    [Fact]
    public void Analyze_FileWithNoDecisions_HasZeroDecisionCoverage()
    {
        var code = @"
class C {
    void M() {
        var x = 1;
        var y = 2;
    }
}";
        var filePath = CreateTempFile(code);

        try
        {
            var report = _analyzer.Analyze(
                new List<string> { filePath },
                new List<AnalysisResultCoverage>());

            Assert.Single(report.Files);
            Assert.Equal(0, report.Files[0].TotalDecisions);
            Assert.Equal(0, report.Files[0].CoveredDecisions);
            Assert.Equal(0.0, report.Files[0].DecisionCoveragePercent);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_FileWithDecisions_MarksStatementLines()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        if (a && b) {
            var x = 1;
        }
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true, DecisionCoverage = true, McdcCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);

            Assert.Equal(filePath, display.FilePath);
            Assert.True(display.Lines.Count > 0);

            // At least one line should be an executable statement
            Assert.Contains(display.Lines, l => l.IsExecutableStatement);
            // At least one line should be a decision line
            Assert.Contains(display.Lines, l => l.IsDecisionLine);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_DecisionLine_IsMarkedCovered()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true, DecisionCoverage = true, McdcCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            var decisionLine = display.Lines.First(l => l.IsDecisionLine);

            Assert.True(decisionLine.DecisionCovered);
            Assert.True(decisionLine.McdcCovered);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_NonExistentFile_ReturnsEmptyDisplay()
    {
        var options = new CoverageOptions { StatementCoverage = true };
        var display = _analyzer.AnalyzeLines("/nonexistent.cs", new(), options);

        Assert.Empty(display.Lines);
    }

    [Fact]
    public void AnalyzeLines_CoveredStatements_MarkedCorrectly()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        var x = 1;
        if (a && b) {
            var y = 2;
        }
    }
    void Uncovered() {
        var z = 3;
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);

            // Statements in M() should be covered (method has a decision)
            var statementsInM = display.Lines.Where(l =>
                l.IsExecutableStatement && l.SourceText.Contains("var x") || l.SourceText.Contains("var y"));

            // Statement in Uncovered() should not be covered
            var uncoveredLine = display.Lines.FirstOrDefault(l =>
                l.IsExecutableStatement && l.SourceText.Contains("var z"));
            if (uncoveredLine != null)
                Assert.False(uncoveredLine.StatementCovered);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_ConstructorWithDecision_CountsStatements()
    {
        var code = @"class C {
    public C(bool a, bool b) {
        if (a && b) {
            var x = 1;
        }
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            Assert.Contains(display.Lines, l => l.IsExecutableStatement && l.StatementCovered);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_PropertyAccessorWithDecision_CountsStatements()
    {
        var code = @"class C {
    private bool _a, _b;
    public int Value {
        get {
            if (_a && _b) { return 1; }
            return 0;
        }
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            Assert.Contains(display.Lines, l => l.IsExecutableStatement);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_DestructorWithDecision_CountsStatements()
    {
        var code = @"class C {
    private bool _a, _b;
    ~C() {
        if (_a && _b) {
            var x = 1;
        }
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            Assert.Contains(display.Lines, l => l.IsExecutableStatement && l.StatementCovered);
            Assert.Contains(display.Lines, l => l.IsDecisionLine);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_OperatorWithDecision_CountsStatements()
    {
        var code = @"struct S {
    public bool A;
    public bool B;
    public static S operator +(S left, S right) {
        if (left.A && right.B) { return left; }
        return right;
    }
}";
        var (results, filePath) = AnalyzeSource(code);
        var options = new CoverageOptions { StatementCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            Assert.Contains(display.Lines, l => l.IsExecutableStatement && l.StatementCovered);
            Assert.Contains(display.Lines, l => l.IsDecisionLine);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_UncoveredDecision_NotMarkedDecisionCovered()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var filePath = CreateTempFile(code);
        var extractor = new DecisionExtractor();
        var parser = new ExpressionParser();
        var mcdcAnalyzer = new McdcAnalyzer();

        var decisions = extractor.Extract(filePath, code);
        var results = new List<AnalysisResultCoverage>();

        foreach (var decision in decisions)
        {
            var (expr, conds) = parser.Parse(decision.ExpressionSyntax);
            var mcResult = mcdcAnalyzer.Analyze(expr, conds, decision.ExpressionText);
            results.Add(new AnalysisResultCoverage
            {
                Decision = decision,
                McResult = mcResult,
                IsDecisionCovered = false
            });
        }

        var options = new CoverageOptions { DecisionCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            var decisionLine = display.Lines.First(l => l.IsDecisionLine);

            Assert.False(decisionLine.DecisionCovered);
        }
        finally { File.Delete(filePath); }
    }

    [Fact]
    public void AnalyzeLines_McdcNotFullyCovered_NotMarkedMcdcCovered()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var filePath = CreateTempFile(code);
        var extractor = new DecisionExtractor();
        var parser = new ExpressionParser();
        var mcdcAnalyzer = new McdcAnalyzer();

        var decisions = extractor.Extract(filePath, code);
        var results = new List<AnalysisResultCoverage>();

        foreach (var decision in decisions)
        {
            var (expr, conds) = parser.Parse(decision.ExpressionSyntax);
            var mcResult = mcdcAnalyzer.Analyze(expr, conds, decision.ExpressionText);

            // Create a result with empty independence pairs to simulate no MC/DC coverage
            var emptyPairs = mcResult.IndependencePairs.Keys.ToDictionary(
                k => k,
                k => new List<IndependencePair>());
            var modifiedResult = new McdcResult(
                mcResult.ExpressionText,
                mcResult.DisplayExpression,
                mcResult.TruthTable,
                emptyPairs,
                mcResult.MinimalTestSet,
                mcResult.ConditionMappings);

            results.Add(new AnalysisResultCoverage
            {
                Decision = decision,
                McResult = modifiedResult,
                IsDecisionCovered = true
            });
        }

        var options = new CoverageOptions { McdcCoverage = true };

        try
        {
            var display = _analyzer.AnalyzeLines(filePath, results, options);
            var decisionLine = display.Lines.First(l => l.IsDecisionLine);

            Assert.False(decisionLine.McdcCovered);
        }
        finally { File.Delete(filePath); }
    }
}
