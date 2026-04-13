using McdcTool.Core.Analysis;
using McdcTool.Core.CodeGen;
using McdcTool.Core.Coverage;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Web.Services;

public class AnalysisResult
{
    public DecisionInfo Decision { get; set; } = null!;
    public McdcResult Result { get; set; } = null!;
    public string TestCode { get; set; } = "";
    public bool IsDecisionCovered { get; set; }
}

public class McdcAnalysisService
{
    public SolutionInfo? LoadSolution(string slnPath)
    {
        if (!File.Exists(slnPath) || !slnPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return null;
        return SolutionParser.ParseSolution(slnPath);
    }

    public ProjectFileInfo? LoadProject(string csprojPath)
    {
        if (!File.Exists(csprojPath) || !csprojPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return null;
        return SolutionParser.ParseProject(csprojPath);
    }

    public List<string> DiscoverFiles(string path)
    {
        return SolutionParser.GetCSharpFiles(path);
    }

    public List<AnalysisResult> AnalyzeFiles(List<string> filePaths)
    {
        var extractor = new DecisionExtractor();
        var parser = new ExpressionParser();
        var analyzer = new McdcAnalyzer();
        var testGenerator = new XUnitTestGenerator();

        var results = new List<AnalysisResult>();

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;

            var sourceCode = File.ReadAllText(filePath);
            var decisions = extractor.Extract(filePath, sourceCode);

            foreach (var decision in decisions)
            {
                var (expression, conditions) = parser.Parse(decision.ExpressionSyntax);
                var mcdcResult = analyzer.Analyze(expression, conditions, decision.ExpressionText);
                var testCode = testGenerator.Generate(decision, mcdcResult);
                bool covered = mcdcResult.MinimalTestSet.Any(tc => tc.Row.DecisionOutcome)
                    && mcdcResult.MinimalTestSet.Any(tc => !tc.Row.DecisionOutcome);

                results.Add(new AnalysisResult
                {
                    Decision = decision,
                    Result = mcdcResult,
                    TestCode = testCode,
                    IsDecisionCovered = covered
                });
            }
        }

        return results;
    }

    public CoverageReport ComputeCoverage(List<string> filePaths, List<AnalysisResult> results)
    {
        var coverageItems = results.Select(r => new AnalysisResultCoverage
        {
            Decision = r.Decision,
            McResult = r.Result,
            IsDecisionCovered = r.IsDecisionCovered
        }).ToList();

        return new CoverageAnalyzer().Analyze(filePaths, coverageItems);
    }

    public FileCoverageDisplay ComputeLineCoverage(string filePath, List<AnalysisResult> results, CoverageOptions options)
    {
        var coverageItems = results.Select(r => new AnalysisResultCoverage
        {
            Decision = r.Decision,
            McResult = r.Result,
            IsDecisionCovered = r.IsDecisionCovered
        }).ToList();

        return new CoverageAnalyzer().AnalyzeLines(filePath, coverageItems, options);
    }

    public void GenerateTestProject(List<string> sourceFiles, List<AnalysisResult> results, string outputDir)
    {
        var items = results
            .Select(r => (r.Decision, r.Result))
            .ToList();
        new TestProjectGenerator().GenerateProject(sourceFiles, items, outputDir);
    }

    public void ExportTestFiles(List<AnalysisResult> results, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        foreach (var result in results)
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(result.Decision.FilePath)}_Line{result.Decision.LineNumber}_McdcTests.cs";
            var outputPath = Path.Combine(outputDir, fileName);
            File.WriteAllText(outputPath, result.TestCode);
        }
    }
}
