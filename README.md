# MC/DC Testing Tool for C#

A .NET tool that performs **Modified Condition/Decision Coverage (MC/DC)** analysis on C# source files. It parses C# code using Roslyn, extracts boolean decisions, generates truth tables, identifies independence pairs, selects minimal test sets satisfying Unique-Cause MC/DC, and produces xUnit test case code.

MC/DC is a structural testing criterion required for safety-critical software (e.g., DO-178C Level A in aviation). It ensures each condition in a boolean decision independently affects the outcome, reducing test cases from 2^N (exhaustive) to N+1 while maintaining strong fault detection.

## Features

- **C# source parsing** via Roslyn (supports up to C# 13 / .NET 10 syntax)
- **Decision extraction** from `if`, `while`, `for`, `do-while`, and ternary (`?:`) statements
- **Truth table generation** for each compound boolean decision
- **Independence pair identification** using the Unique-Cause MC/DC algorithm
- **Minimal test set selection** via greedy set cover (typically N+1 test cases for N conditions)
- **xUnit test code generation** with `[Theory]` / `[InlineData]` attributes
- **Statement and decision coverage analysis** — statically estimates coverage provided by the generated test set using Roslyn syntax walking
- **Runnable test project generation** — exports a complete `.csproj` (xunit + coverlet) so tests can be compiled and run with `dotnet test --collect:"XPlat Code Coverage"`
- **Two interfaces**:
  - **CLI** for batch analysis and scripting
  - **Blazor Web UI** for interactive analysis with visual truth tables, coverage summary, and copy-to-clipboard
- **Solution/project-aware file selection** — load `.sln` or `.csproj` files to browse project structure, or select individual `.cs` files and directories

## Project Structure

```
McdcTool.sln
├── src/
│   ├── McdcTool.Core/          # Shared class library (models, parsing, analysis, codegen)
│   │   ├── Analysis/           # ExpressionParser, TruthTableGenerator, McdcAnalyzer
│   │   ├── CodeGen/            # XUnitTestGenerator, TestProjectGenerator
│   │   ├── Coverage/           # CoverageAnalyzer (statement + decision coverage)
│   │   ├── Models/             # BooleanExpression, McdcResult, CoverageReport
│   │   └── Parsing/            # DecisionExtractor, DecisionInfo, SolutionParser
│   ├── McdcTool/               # Console application (CLI)
│   └── McdcTool.Web/           # Blazor Server web application
├── tests/
│   └── McdcTool.Tests/         # Unit tests (xUnit)
└── samples/
    └── SampleCode.cs           # Sample C# file for testing
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0.201 or later)

## Dependencies

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| Microsoft.CodeAnalysis.CSharp | 4.12.0 | McdcTool.Core | Roslyn C# syntax parsing and coverage analysis |
| System.CommandLine | 2.0.0-beta4 | McdcTool | CLI argument parsing |
| xunit | 2.5.3 | McdcTool.Tests | Unit test framework |
| Microsoft.NET.Test.Sdk | 17.8.0 | McdcTool.Tests | Test runner host |
| coverlet.collector | 6.0.0 | McdcTool.Tests | Code coverage collection |

> **Generated test projects** also reference `xunit 2.5.3`, `xunit.runner.visualstudio 2.5.3`, `Microsoft.NET.Test.Sdk 17.8.0`, and `coverlet.collector 6.0.0` — these are written into the generated `.csproj` automatically.

All dependencies are restored automatically via NuGet on build.

## Installation

```bash
git clone <repository-url>
cd mcdc_test
dotnet restore
dotnet build
```

## Running Tests

```bash
dotnet test
```

All 25 unit tests cover the expression parser, truth table generator, MC/DC analyzer, and decision extractor. Coverage analysis and project generation are exercised via the Web UI and CLI.

## Usage

### CLI

```
dotnet run --project src/McdcTool -- <path> [options]
```

**Arguments:**
- `<path>` — Path to a `.cs` file or directory to analyze

**Options:**
- `-o, --output <dir>` — Output directory for generated test files (default: `./McdcTests`)
- `-v, --verbose` — Show full truth tables and all independence pairs

**Examples:**

```bash
# Analyze a single file
dotnet run --project src/McdcTool -- samples/SampleCode.cs

# Analyze with verbose output and custom output directory
dotnet run --project src/McdcTool -- samples/SampleCode.cs -v -o ./GeneratedTests

# Analyze all .cs files in a directory
dotnet run --project src/McdcTool -- ./src/MyProject/
```

**Sample output:**

```
Analyzing 1 C# file(s)...

=== SampleCode.cs ===

  [if] Line 14: a && (b || c)
  Simplified: (A && (B || C))
  Conditions (3):
    A = a
    B = b
    C = c

  Minimal MC/DC Test Set (4 test cases):
    Test 1: [A=False, B=False, C=True] -> False  (covers: A)
    Test 4: [A=True, B=False, C=False] -> False  (covers: B, C)
    Test 5: [A=True, B=False, C=True] -> True  (covers: C, A)
    Test 6: [A=True, B=True, C=False] -> True  (covers: B)
```

### Web UI

```bash
dotnet run --project src/McdcTool.Web
```

Open your browser to **http://localhost:5265** (or the URL shown in the console).

**Workflow:**
1. Enter a path to a `.sln`, `.csproj`, `.cs` file, or directory and click **Load**
2. For `.sln` files, a project tree is shown with checkboxes to select files
3. Select the files to analyze and click **Analyze Selected**
4. A **Coverage Summary** card shows statement and decision coverage percentages (with progress bars) for the selected files. Expand the card to see a per-file breakdown
5. View results for each decision: condition mappings, truth tables (with highlighted test rows), independence pairs, minimal test sets, and generated xUnit code. Each card shows a **Decision ✓** badge when the test set covers both true and false outcomes
6. Use **Copy to Clipboard** to copy generated test code, or **Export All Tests** to save test files to disk
7. Click **Generate Test Project** to create a self-contained `.csproj` you can build and run with `dotnet test`

## Coverage Analysis

### Statement Coverage
Statement coverage is estimated **statically** using Roslyn. The analyzer walks the syntax tree of each source file, counts all executable `StatementSyntax` nodes per method, and marks a method as "covered" when its span contains at least one analyzed boolean decision. The percentage shown in the UI is:

```
covered statements / total statements × 100
```

where "covered statements" are those in methods that contain an analyzed decision.

### Decision Coverage
A boolean decision is considered "covered" when the generated MC/DC minimal test set includes at least one test case with a `true` outcome **and** at least one with a `false` outcome. This is inherently satisfied for any decision that has valid independence pairs. The percentage is:

```
decisions with both T and F in test set / total decisions found × 100
```

> **Note:** These are static estimates computed at analysis time. For precise runtime coverage, use the generated test project with coverlet (see below).

## Generated Test Project

The **Generate Test Project** button (Web UI) creates a directory containing:

| File | Description |
|------|-------------|
| `McdcTests.csproj` | xUnit test project with xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, and coverlet.collector |
| `*_Line*_McdcTests.cs` | Generated xUnit `[Theory]` test files (one per analyzed decision) |
| `HOW_TO_RUN.txt` | Step-by-step instructions |

**Running the generated project:**

```bash
cd <output-directory>
dotnet restore
dotnet test
```

**Collecting runtime coverage with coverlet:**

```bash
dotnet test --collect:"XPlat Code Coverage"
# Results written to: TestResults/<guid>/coverage.cobertura.xml
```

**Generating an HTML coverage report:**

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

> The generated test method bodies contain `// TODO` placeholders. Replace them with actual calls to the code under test before running.

## How It Works

1. **Decision Extraction** — Roslyn's `CSharpSyntaxWalker` visits `if`, `while`, `for`, `do-while`, and ternary statements, extracting compound boolean conditions (those containing `&&`, `||`, or `!`)

2. **Expression Parsing** — Each Roslyn `ExpressionSyntax` is converted to an internal AST with `And`, `Or`, `Not`, and `Condition` nodes. Atomic conditions are labeled A, B, C, etc.

3. **Truth Table Generation** — All 2^N combinations are evaluated for N conditions

4. **Independence Pair Finding** — For each condition, pairs of truth table rows are identified where only that condition differs and the decision outcome changes (Unique-Cause MC/DC criterion)

5. **Minimal Test Set Selection** — A greedy set cover algorithm selects independence pairs that maximize row reuse, producing a minimal set of typically N+1 test cases

6. **Coverage Analysis** — A second Roslyn pass counts executable statements per method and checks whether each decision's test set exercises both true and false outcomes, producing statement and decision coverage percentages

7. **Test Code Generation** — xUnit `[Theory]` methods with `[InlineData]` attributes are generated for each decision

8. **Test Project Generation** — A self-contained `.csproj` with all necessary NuGet references is emitted alongside the test files so the suite can be built and run with `dotnet test`

## License

This project is provided as-is for educational and testing purposes.
