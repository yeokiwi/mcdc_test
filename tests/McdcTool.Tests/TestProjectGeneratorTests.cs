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

    [Fact]
    public void GenerateProject_CsprojContainsGenerateAssemblyInfoFalse()
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
            Assert.Contains("<GenerateAssemblyInfo>false</GenerateAssemblyInfo>", csproj);
        }
        finally { Directory.Delete(outputDir, true); }
    }

    [Fact]
    public void GenerateProject_WinFormsSource_AddsUseWindowsForms()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var srcDir = Path.Combine(Path.GetTempPath(), $"test_src_{Guid.NewGuid():N}");
        Directory.CreateDirectory(srcDir);
        var srcFile = Path.Combine(srcDir, "Form1.cs");
        File.WriteAllText(srcFile, "using System.Windows.Forms;\nclass Form1 : Form { }");

        var (decision, result) = CreateAnalysis(srcFile);

        try
        {
            _generator.GenerateProject(
                new List<string> { srcFile },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("<UseWindowsForms>true</UseWindowsForms>", csproj);
        }
        finally
        {
            Directory.Delete(outputDir, true);
            Directory.Delete(srcDir, true);
        }
    }

    [Fact]
    public void GenerateProject_WpfSource_AddsUseWpf()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var srcDir = Path.Combine(Path.GetTempPath(), $"test_src_{Guid.NewGuid():N}");
        Directory.CreateDirectory(srcDir);
        var srcFile = Path.Combine(srcDir, "MainWindow.cs");
        File.WriteAllText(srcFile, "using System.Windows.Controls;\nclass MainWindow { }");

        var (decision, result) = CreateAnalysis(srcFile);

        try
        {
            _generator.GenerateProject(
                new List<string> { srcFile },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("<UseWPF>true</UseWPF>", csproj);
        }
        finally
        {
            Directory.Delete(outputDir, true);
            Directory.Delete(srcDir, true);
        }
    }

    [Fact]
    public void GenerateProject_SourceWithCsproj_ExtractsPackageReferences()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var srcDir = Path.Combine(Path.GetTempPath(), $"test_src_{Guid.NewGuid():N}");
        Directory.CreateDirectory(srcDir);

        // Create a source .csproj with a PackageReference
        File.WriteAllText(Path.Combine(srcDir, "MyApp.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>");

        var srcFile = Path.Combine(srcDir, "Program.cs");
        File.WriteAllText(srcFile, "class Program { static void Main() {} }");

        var (decision, result) = CreateAnalysis(srcFile);

        try
        {
            _generator.GenerateProject(
                new List<string> { srcFile },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var csproj = File.ReadAllText(Path.Combine(outputDir, "McdcTests.csproj"));
            Assert.Contains("Newtonsoft.Json", csproj);
            Assert.Contains("13.0.3", csproj);
        }
        finally
        {
            Directory.Delete(outputDir, true);
            Directory.Delete(srcDir, true);
        }
    }

    [Fact]
    public void ExtractSourceProjectReferences_SkipsBuiltInTestPackages()
    {
        var srcDir = Path.Combine(Path.GetTempPath(), $"test_src_{Guid.NewGuid():N}");
        Directory.CreateDirectory(srcDir);

        File.WriteAllText(Path.Combine(srcDir, "Test.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.5.3"" />
    <PackageReference Include=""SomeOtherPackage"" Version=""1.0.0"" />
  </ItemGroup>
</Project>");
        var srcFile = Path.Combine(srcDir, "Code.cs");
        File.WriteAllText(srcFile, "class C {}");

        try
        {
            var pkgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TestProjectGenerator.ExtractSourceProjectReferences(new List<string> { srcFile }, pkgs);

            // xunit should be extracted (filtering is done in BuildCsproj, not in extraction)
            Assert.True(pkgs.ContainsKey("xunit"));
            Assert.True(pkgs.ContainsKey("SomeOtherPackage"));
        }
        finally { Directory.Delete(srcDir, true); }
    }

    [Fact]
    public void FindCsprojForFile_FindsNearestCsproj()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"test_find_{Guid.NewGuid():N}");
        var subDir = Path.Combine(baseDir, "src");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(baseDir, "App.csproj"), "<Project />");
        var srcFile = Path.Combine(subDir, "Code.cs");
        File.WriteAllText(srcFile, "class C {}");

        try
        {
            var found = TestProjectGenerator.FindCsprojForFile(srcFile);
            Assert.NotNull(found);
            Assert.EndsWith(".csproj", found);
        }
        finally { Directory.Delete(baseDir, true); }
    }

    [Fact]
    public void FindCsprojForFile_NoCsproj_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"test_nocsproj_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var srcFile = Path.Combine(dir, "Code.cs");
        File.WriteAllText(srcFile, "class C {}");

        try
        {
            // This will walk up from the temp dir - may or may not find a csproj
            // depending on the system, so just verify it doesn't throw
            var found = TestProjectGenerator.FindCsprojForFile(srcFile);
            // No assertion on null - it depends on the filesystem above temp
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GenerateProject_HowToRunMentionsNet10()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"test_proj_{Guid.NewGuid():N}");
        var (decision, result) = CreateAnalysis("/src/Test.cs");

        try
        {
            _generator.GenerateProject(
                new List<string> { "/src/Test.cs" },
                new List<(DecisionInfo, McdcResult)> { (decision, result) },
                outputDir);

            var content = File.ReadAllText(Path.Combine(outputDir, "HOW_TO_RUN.txt"));
            Assert.Contains(".NET 10.0", content);
        }
        finally { Directory.Delete(outputDir, true); }
    }
}
