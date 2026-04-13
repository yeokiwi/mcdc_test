using McdcTool.Core.Models;
using McdcTool.Web.Services;

namespace McdcTool.Tests;

public class McdcAnalysisServiceTests
{
    private readonly McdcAnalysisService _service = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // --- LoadSolution ---

    [Fact]
    public void LoadSolution_NonExistentFile_ReturnsNull()
    {
        var result = _service.LoadSolution("/nonexistent/path/test.sln");
        Assert.Null(result);
    }

    [Fact]
    public void LoadSolution_WrongExtension_ReturnsNull()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "test.txt");
        File.WriteAllText(file, "not a solution");

        try
        {
            var result = _service.LoadSolution(file);
            Assert.Null(result);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void LoadSolution_ValidSln_ReturnsSolution()
    {
        var dir = CreateTempDir();
        var slnPath = Path.Combine(dir, "Test.sln");
        File.WriteAllText(slnPath, @"
Microsoft Visual Studio Solution File, Format Version 12.00
");

        try
        {
            var result = _service.LoadSolution(slnPath);
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- LoadProject ---

    [Fact]
    public void LoadProject_NonExistentFile_ReturnsNull()
    {
        var result = _service.LoadProject("/nonexistent/path/test.csproj");
        Assert.Null(result);
    }

    [Fact]
    public void LoadProject_WrongExtension_ReturnsNull()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "test.txt");
        File.WriteAllText(file, "not a project");

        try
        {
            var result = _service.LoadProject(file);
            Assert.Null(result);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void LoadProject_ValidCsproj_ReturnsProject()
    {
        var dir = CreateTempDir();
        var csproj = Path.Combine(dir, "Test.csproj");
        File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
</Project>");

        try
        {
            var result = _service.LoadProject(csproj);
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- DiscoverFiles ---

    [Fact]
    public void DiscoverFiles_Directory_ReturnsCsFiles()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "A.cs"), "class A {}");
        File.WriteAllText(Path.Combine(dir, "B.cs"), "class B {}");

        try
        {
            var files = _service.DiscoverFiles(dir);
            Assert.Equal(2, files.Count);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- AnalyzeFiles ---

    [Fact]
    public void AnalyzeFiles_WithCompoundDecisions_ReturnsResults()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "Test.cs");
        File.WriteAllText(file, @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}");

        try
        {
            var results = _service.AnalyzeFiles(new List<string> { file });

            Assert.Single(results);
            Assert.Equal("if", results[0].Decision.StatementType);
            Assert.NotNull(results[0].Result);
            Assert.NotEmpty(results[0].TestCode);
            Assert.True(results[0].IsDecisionCovered);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void AnalyzeFiles_NonExistentFile_SkipsFile()
    {
        var results = _service.AnalyzeFiles(new List<string> { "/nonexistent/file.cs" });
        Assert.Empty(results);
    }

    [Fact]
    public void AnalyzeFiles_NoCompoundDecisions_ReturnsEmpty()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "Test.cs");
        File.WriteAllText(file, @"
class C {
    void M(bool a) {
        if (a) { }
    }
}");

        try
        {
            var results = _service.AnalyzeFiles(new List<string> { file });
            Assert.Empty(results);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void AnalyzeFiles_MultipleDecisions_ReturnsAll()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "Test.cs");
        File.WriteAllText(file, @"
class C {
    void M(bool a, bool b, bool c) {
        if (a && b) { }
        while (b || c) { break; }
    }
}");

        try
        {
            var results = _service.AnalyzeFiles(new List<string> { file });
            Assert.Equal(2, results.Count);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- ComputeCoverage ---

    [Fact]
    public void ComputeCoverage_DelegatesToCoverageAnalyzer()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "Test.cs");
        File.WriteAllText(file, @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}");

        try
        {
            var analysisResults = _service.AnalyzeFiles(new List<string> { file });
            var report = _service.ComputeCoverage(new List<string> { file }, analysisResults);

            Assert.NotNull(report);
            Assert.Single(report.Files);
            Assert.Equal(1, report.Files[0].TotalDecisions);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- ComputeLineCoverage ---

    [Fact]
    public void ComputeLineCoverage_ReturnsLineLevelData()
    {
        var dir = CreateTempDir();
        var file = Path.Combine(dir, "Test.cs");
        File.WriteAllText(file, @"class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}");

        try
        {
            var analysisResults = _service.AnalyzeFiles(new List<string> { file });
            var options = new CoverageOptions { StatementCoverage = true, DecisionCoverage = true };
            var display = _service.ComputeLineCoverage(file, analysisResults, options);

            Assert.Equal(file, display.FilePath);
            Assert.True(display.Lines.Count > 0);
            Assert.Contains(display.Lines, l => l.IsDecisionLine);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- GenerateTestProject ---

    [Fact]
    public void GenerateTestProject_CreatesOutputFiles()
    {
        var dir = CreateTempDir();
        var srcFile = Path.Combine(dir, "Test.cs");
        File.WriteAllText(srcFile, @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}");
        var outputDir = Path.Combine(dir, "output");

        try
        {
            var analysisResults = _service.AnalyzeFiles(new List<string> { srcFile });
            _service.GenerateTestProject(new List<string> { srcFile }, analysisResults, outputDir);

            Assert.True(Directory.Exists(outputDir));
            Assert.True(File.Exists(Path.Combine(outputDir, "McdcTests.csproj")));
            Assert.True(File.Exists(Path.Combine(outputDir, "HOW_TO_RUN.txt")));

            var testFiles = Directory.GetFiles(outputDir, "*_McdcTests.cs");
            Assert.NotEmpty(testFiles);
        }
        finally { Directory.Delete(dir, true); }
    }

    // --- ExportTestFiles ---

    [Fact]
    public void ExportTestFiles_WritesTestFiles()
    {
        var dir = CreateTempDir();
        var srcFile = Path.Combine(dir, "Test.cs");
        File.WriteAllText(srcFile, @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}");
        var outputDir = Path.Combine(dir, "export");

        try
        {
            var analysisResults = _service.AnalyzeFiles(new List<string> { srcFile });
            _service.ExportTestFiles(analysisResults, outputDir);

            Assert.True(Directory.Exists(outputDir));
            var testFiles = Directory.GetFiles(outputDir, "*_McdcTests.cs");
            Assert.Single(testFiles);

            var content = File.ReadAllText(testFiles[0]);
            Assert.Contains("using Xunit;", content);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ExportTestFiles_MultipleResults_WritesAll()
    {
        var dir = CreateTempDir();
        var srcFile = Path.Combine(dir, "Test.cs");
        File.WriteAllText(srcFile, @"
class C {
    void M(bool a, bool b, bool c) {
        if (a && b) { }
        if (b || c) { }
    }
}");
        var outputDir = Path.Combine(dir, "export");

        try
        {
            var analysisResults = _service.AnalyzeFiles(new List<string> { srcFile });
            _service.ExportTestFiles(analysisResults, outputDir);

            var testFiles = Directory.GetFiles(outputDir, "*_McdcTests.cs");
            Assert.Equal(2, testFiles.Length);
        }
        finally { Directory.Delete(dir, true); }
    }
}
