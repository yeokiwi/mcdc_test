using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Core.Models;

namespace McdcTool.Core.Coverage;

public class CoverageAnalyzer
{
    /// <summary>
    /// Computes statement and decision coverage for the given files based on the MC/DC analysis results.
    /// Coverage is computed statically: a statement is "covered" when it resides in a method whose
    /// span contains at least one analyzed boolean decision. A decision is "covered" when its
    /// MC/DC minimal test set exercises both true and false outcomes.
    /// </summary>
    public CoverageReport Analyze(List<string> filePaths, List<AnalysisResultCoverage> analysisResults)
    {
        var report = new CoverageReport();

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;

            var source = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(source,
                new CSharpParseOptions(LanguageVersion.Preview));
            var root = tree.GetRoot();

            // Decisions analyzed in this file
            var decisionsInFile = analysisResults
                .Where(r => string.Equals(r.Decision.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Decision coverage
            int totalDecisions = decisionsInFile.Count;
            int coveredDecisions = decisionsInFile.Count(r => r.IsDecisionCovered);

            // Statement coverage using syntax walker
            var walker = new StatementWalker(tree);
            walker.Visit(root);

            // A method is "exercised" if any analyzed decision line falls within it
            var coveredLineNumbers = decisionsInFile.Select(r => r.Decision.LineNumber).ToHashSet();
            int coveredStatements = walker.CountCoveredStatements(coveredLineNumbers);

            report.Files.Add(new FileCoverageInfo
            {
                FilePath = filePath,
                TotalStatements = walker.TotalStatements,
                CoveredStatements = coveredStatements,
                TotalDecisions = totalDecisions,
                CoveredDecisions = coveredDecisions
            });
        }

        return report;
    }

    private static bool IsDecisionCovered(McdcResult result)
    {
        return result.MinimalTestSet.Any(tc => tc.Row.DecisionOutcome)
            && result.MinimalTestSet.Any(tc => !tc.Row.DecisionOutcome);
    }
}

/// <summary>
/// Lightweight DTO used by CoverageAnalyzer so it doesn't depend on the Web service layer.
/// </summary>
public class AnalysisResultCoverage
{
    public required Parsing.DecisionInfo Decision { get; set; }
    public required McdcResult McResult { get; set; }
    public bool IsDecisionCovered { get; set; }
}

/// <summary>
/// Walks the syntax tree and counts executable statements, grouped by the method/accessor that contains them.
/// BlockSyntax nodes are not counted (they are containers, not executable statements).
/// </summary>
internal class StatementWalker : CSharpSyntaxWalker
{
    private readonly SyntaxTree _tree;
    // Maps first-line-number of each method to the count of statements inside it
    private readonly Dictionary<int, List<int>> _methodToStatementLines = new();
    public int TotalStatements { get; private set; }

    public StatementWalker(SyntaxTree tree) : base(SyntaxWalkerDepth.Node)
    {
        _tree = tree;
    }

    public override void VisitBlock(BlockSyntax node)
    {
        // Visit children but don't count the block itself
        base.VisitBlock(node);
    }

    public override void DefaultVisit(SyntaxNode node)
    {
        if (node is StatementSyntax stmt && stmt is not BlockSyntax)
        {
            var lineSpan = stmt.GetLocation().GetLineSpan();
            int stmtLine = lineSpan.StartLinePosition.Line + 1; // 1-based

            // Find the enclosing method/constructor/accessor
            int methodStartLine = GetEnclosingMemberStartLine(stmt);

            if (!_methodToStatementLines.TryGetValue(methodStartLine, out var lines))
            {
                lines = new List<int>();
                _methodToStatementLines[methodStartLine] = lines;
            }
            lines.Add(stmtLine);
            TotalStatements++;
        }

        base.DefaultVisit(node);
    }

    /// <summary>
    /// Returns the start line of the nearest enclosing method-like member, or -1 if none.
    /// </summary>
    private int GetEnclosingMemberStartLine(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax
                or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
            {
                return ancestor.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            }
        }
        return -1; // top-level or initializer context
    }

    /// <summary>
    /// Counts statements that belong to methods whose span contains at least one of the given decision line numbers.
    /// </summary>
    public int CountCoveredStatements(HashSet<int> decisionLineNumbers)
    {
        if (decisionLineNumbers.Count == 0) return 0;

        // For each method start line, check if any decision falls inside its span
        // We use the statement lines as a proxy: if any statement line in the method
        // matches a decision line (approximately), the method is considered exercised.
        // More precisely, we need the method's full line range.
        // We'll rebuild that from the walker data by finding all statement lines per method.

        int covered = 0;
        foreach (var (methodStartLine, stmtLines) in _methodToStatementLines)
        {
            // Method line range approximated as [methodStartLine .. max(stmtLines)]
            int methodEndLine = stmtLines.Count > 0 ? stmtLines.Max() : methodStartLine;

            bool methodExercised = decisionLineNumbers.Any(dl => dl >= methodStartLine && dl <= methodEndLine + 5);
            if (methodExercised)
                covered += stmtLines.Count;
        }
        return covered;
    }
}
