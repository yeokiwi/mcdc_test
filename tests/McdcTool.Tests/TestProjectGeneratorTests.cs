using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Core.Analysis;
using McdcTool.Core.CodeGen;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class TestProjectGeneratorTests
{
    private readonly TestProjectGenerator _generator = new();

    private (DecisionInfo Decision, McdcResult Result) CreateAnalysis(string filePath)
    {
        var code = "class C { void M(bool a, bool b) { if (a && b) {} } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var ifStmt = tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>().First();
        var condition = ifStmt.Condition;

        var parser = new ExpressionParser();
        var (expr, conds) = parser.Parse(condition);
        var analyzer = new McdcAnalyzer();
        var result = analyzer.Analyze(expr, conds, condition.ToString());

        var decision = new DecisionInfo(filePath, 1, 1, "if", condition.ToString(), condition);
        return (decision, result);
    }

    [Fact]
    public void GenerateProject_CreatesCsprojFile()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            Assert.True(File.Exists(Path.Combine(outputDir, "McdcTests.csproj")));
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_CreatesTestFiles()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csFiles = Directory.GetFiles(outputDir, "*_McdcTests.cs");
            Assert.NotEmpty(csFiles);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_CreatesHowToRunFile()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var howToRun = Path.Combine(outputDir, "HOW_TO_RUN.txt");
            Assert.True(File.Exists(howToRun));
            var content = File.ReadAllText(howToRun);
            Assert.Contains("dotnet test", content);
            Assert.Contains("XPlat Code Coverage", content);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_CsprojContainsXunitReference()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("xunit", csproj);
            Assert.Contains("Microsoft.NET.Test.Sdk", csproj);
            Assert.Contains("coverlet.collector", csproj);
            Assert.Contains("xunit.runner.visualstudio", csproj);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_CsprojContainsSourceFileReference()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("<Compile Include=", csproj);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_CsprojTargetsNet10()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("net10.0", csproj);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_MultipleSourceFiles_AllIncluded()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/File1.cs", "/src/File2.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("File1.cs", csproj);
            Assert.Contains("File2.cs", csproj);
        }
        finally { Directory.Delete(outputDir, true); }
    }
}
