using System.CommandLine;
using McdcTool.Analysis;
using McdcTool.CodeGen;
using McdcTool.Models;
using McdcTool.Parsing;

var pathArgument = new Argument<string>(
    name: "path",
    description: "Path to a .cs file or directory to analyze");

var outputOption = new Option<string>(
    aliases: new[] { "-o", "--output" },
    getDefaultValue: () => "./McdcTests",
    description: "Output directory for generated test files");

var verboseOption = new Option<bool>(
    aliases: new[] { "-v", "--verbose" },
    description: "Show full truth tables and all independence pairs");

var rootCommand = new RootCommand("MC/DC Test Case Generator for C# source files")
{
    pathArgument,
    outputOption,
    verboseOption
};

rootCommand.SetHandler(Run, pathArgument, outputOption, verboseOption);
return await rootCommand.InvokeAsync(args);

static void Run(string path, string outputDir, bool verbose)
{
    var files = GetCSharpFiles(path);
    if (files.Count == 0)
    {
        Console.Error.WriteLine($"No .cs files found at: {path}");
        return;
    }

    Console.WriteLine($"Analyzing {files.Count} C# file(s)...");
    Console.WriteLine();

    var extractor = new DecisionExtractor();
    var parser = new ExpressionParser();
    var analyzer = new McdcAnalyzer();
    var testGenerator = new XUnitTestGenerator();

    var allAnalyses = new List<(DecisionInfo Decision, McdcResult Result)>();
    int totalDecisions = 0;

    foreach (var file in files)
    {
        var sourceCode = File.ReadAllText(file);
        var decisions = extractor.Extract(file, sourceCode);

        if (decisions.Count == 0) continue;

        Console.WriteLine($"=== {Path.GetFileName(file)} ===");

        foreach (var decision in decisions)
        {
            totalDecisions++;
            var (expression, conditions) = parser.Parse(decision.ExpressionSyntax);
            var result = analyzer.Analyze(expression, conditions, decision.ExpressionText);

            PrintDecisionAnalysis(decision, result, verbose);
            allAnalyses.Add((decision, result));
        }
    }

    if (totalDecisions == 0)
    {
        Console.WriteLine("No compound boolean decisions found in the source files.");
        return;
    }

    // Generate test files
    Directory.CreateDirectory(outputDir);

    foreach (var (decision, result) in allAnalyses)
    {
        var testCode = testGenerator.Generate(decision, result);
        var fileName = $"{Path.GetFileNameWithoutExtension(decision.FilePath)}_Line{decision.LineNumber}_McdcTests.cs";
        var outputPath = Path.Combine(outputDir, fileName);
        File.WriteAllText(outputPath, testCode);
    }

    Console.WriteLine($"\n{"",0}============================================================");
    Console.WriteLine($"Summary: Analyzed {totalDecisions} decision(s) across {files.Count} file(s)");
    Console.WriteLine($"Generated {allAnalyses.Count} test file(s) in: {Path.GetFullPath(outputDir)}");
}

static void PrintDecisionAnalysis(DecisionInfo decision, McdcResult result, bool verbose)
{
    Console.WriteLine($"\n  [{decision.StatementType}] Line {decision.LineNumber}: {result.ExpressionText}");
    Console.WriteLine($"  Simplified: {result.DisplayExpression}");
    Console.WriteLine($"  Conditions ({result.ConditionMappings.Count}):");
    foreach (var (label, text) in result.ConditionMappings.OrderBy(kv => kv.Key))
    {
        Console.WriteLine($"    {label} = {text}");
    }

    if (verbose)
    {
        PrintTruthTable(result.TruthTable);
        PrintIndependencePairs(result.IndependencePairs, result.TruthTable.ConditionLabels);
    }

    PrintMinimalTestSet(result);
}

static void PrintTruthTable(TruthTable table)
{
    Console.WriteLine($"\n  Truth Table ({table.Rows.Count} rows):");
    var header = string.Join("  ", table.ConditionLabels) + "  | Result";
    Console.WriteLine($"    Row  {header}");
    Console.WriteLine($"    {new string('-', header.Length + 6)}");

    foreach (var row in table.Rows)
    {
        var values = table.ConditionLabels.Select(l => row.ConditionValues[l] ? "T" : "F");
        var valStr = string.Join("  ", values);
        var outcome = row.DecisionOutcome ? "T" : "F";
        Console.WriteLine($"    {row.RowIndex,3}  {valStr}  | {outcome}");
    }
}

static void PrintIndependencePairs(
    Dictionary<string, List<IndependencePair>> pairs,
    List<string> labels)
{
    Console.WriteLine($"\n  Independence Pairs:");
    foreach (var label in labels)
    {
        var pairList = pairs[label];
        Console.WriteLine($"    {label}: {pairList.Count} pair(s)");
        foreach (var pair in pairList)
        {
            Console.WriteLine($"      Row {pair.TrueRow.RowIndex} ({label}=T) <-> Row {pair.FalseRow.RowIndex} ({label}=F)");
        }
    }
}

static void PrintMinimalTestSet(McdcResult result)
{
    Console.WriteLine($"\n  Minimal MC/DC Test Set ({result.MinimalTestSet.Count} test cases):");

    var uncoveredConditions = result.IndependencePairs
        .Where(kv => kv.Value.Count == 0)
        .Select(kv => kv.Key)
        .ToList();

    if (uncoveredConditions.Count > 0)
    {
        Console.WriteLine($"  WARNING: No independence pairs found for condition(s): {string.Join(", ", uncoveredConditions)}");
        Console.WriteLine("  (These conditions cannot independently affect the decision outcome)");
    }

    foreach (var testCase in result.MinimalTestSet)
    {
        var values = result.TruthTable.ConditionLabels
            .Select(l => $"{l}={testCase.Row.ConditionValues[l]}");
        var covers = string.Join(", ", testCase.CoversConditions);
        Console.WriteLine($"    Test {testCase.Row.RowIndex}: [{string.Join(", ", values)}] -> {testCase.Row.DecisionOutcome}  (covers: {covers})");
    }
}

static List<string> GetCSharpFiles(string path)
{
    if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        return new List<string> { Path.GetFullPath(path) };
    }

    if (Directory.Exists(path))
    {
        return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
    }

    return new List<string>();
}
