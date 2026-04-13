using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Core.Analysis;
using McdcTool.Core.CodeGen;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class XUnitTestGeneratorTests
{
    private readonly XUnitTestGenerator _generator = new();

    private (DecisionInfo Decision, McdcResult Result) CreateAnalysis(string code, string fileName = "Test.cs")
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var ifStmt = root.DescendantNodes().OfType<IfStatementSyntax>().First();
        var condition = ifStmt.Condition;

        var parser = new ExpressionParser();
        var (expr, conds) = parser.Parse(condition);
        var analyzer = new McdcAnalyzer();
        var result = analyzer.Analyze(expr, conds, condition.ToString());

        var lineSpan = condition.GetLocation().GetLineSpan();
        var decision = new DecisionInfo(
            fileName,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1,
            "if",
            condition.ToString(),
            condition);

        return (decision, result);
    }

    [Fact]
    public void Generate_ContainsUsingXunit()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        Assert.Contains("using Xunit;", code);
    }

    [Fact]
    public void Generate_ContainsClassName()
    {
        var (decision, result) = CreateAnalysis(
            "class C { void M(bool a, bool b) { if (a && b) {} } }",
            "MyFile.cs");
        var code = _generator.Generate(decision, result);

        Assert.Contains("MyFile_Line", code);
        Assert.Contains("_McdcTests", code);
    }

    [Fact]
    public void Generate_ContainsTheoryAttribute()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        Assert.Contains("[Theory]", code);
    }

    [Fact]
    public void Generate_ContainsInlineDataForEachTestCase()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        int inlineDataCount = code.Split("[InlineData").Length - 1;
        Assert.Equal(result.MinimalTestSet.Count, inlineDataCount);
    }

    [Fact]
    public void Generate_ContainsConditionMappingComments()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        Assert.Contains("A =", code);
        Assert.Contains("B =", code);
    }

    [Fact]
    public void Generate_ContainsMethodWithParameters()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        Assert.Contains("bool a", code);
        Assert.Contains("bool b", code);
        Assert.Contains("bool expectedOutcome", code);
    }

    [Fact]
    public void Generate_ContainsTodoComment()
    {
        var (decision, result) = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }");
        var code = _generator.Generate(decision, result);

        Assert.Contains("// TODO:", code);
    }

    [Fact]
    public void GenerateAll_MultipleDecisions_GeneratesAll()
    {
        var analysis1 = CreateAnalysis("class C { void M(bool a, bool b) { if (a && b) {} } }", "File1.cs");
        var analysis2 = CreateAnalysis("class C { void M(bool a, bool b) { if (a || b) {} } }", "File2.cs");

        var analyses = new List<(DecisionInfo, McdcResult)>
        {
            (analysis1.Decision, analysis1.Result),
            (analysis2.Decision, analysis2.Result)
        };

        var code = _generator.GenerateAll(analyses);

        Assert.Contains("File1_Line", code);
        Assert.Contains("File2_Line", code);
        Assert.Contains("using Xunit;", code);
    }

    [Fact]
    public void Generate_SpecialCharactersInFileName_Sanitized()
    {
        var (decision, result) = CreateAnalysis(
            "class C { void M(bool a, bool b) { if (a && b) {} } }",
            "my-file.test.cs");
        var code = _generator.Generate(decision, result);

        Assert.Contains("my_file_test_Line", code);
    }
}
