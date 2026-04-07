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
- **Two interfaces**:
  - **CLI** for batch analysis and scripting
  - **Blazor Web UI** for interactive analysis with visual truth tables and copy-to-clipboard
- **Solution/project-aware file selection** — load `.sln` or `.csproj` files to browse project structure, or select individual `.cs` files and directories

## Project Structure

```
McdcTool.sln
├── src/
│   ├── McdcTool.Core/          # Shared class library (models, parsing, analysis, codegen)
│   ├── McdcTool/               # Console application (CLI)
│   └── McdcTool.Web/           # Blazor Server web application
├── tests/
│   └── McdcTool.Tests/         # Unit tests (xUnit)
└── samples/
    └── SampleCode.cs           # Sample C# file for testing
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Dependencies

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| Microsoft.CodeAnalysis.CSharp | 4.12.0 | McdcTool.Core | Roslyn C# syntax parsing |
| System.CommandLine | 2.0.0-beta4 | McdcTool | CLI argument parsing |
| xunit | 2.5.3 | McdcTool.Tests | Unit test framework |
| Microsoft.NET.Test.Sdk | 17.8.0 | McdcTool.Tests | Test runner host |
| coverlet.collector | 6.0.0 | McdcTool.Tests | Code coverage collection |

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

All 25 unit tests cover the expression parser, truth table generator, MC/DC analyzer, and decision extractor.

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
4. View results for each decision: condition mappings, truth tables (with highlighted test rows), independence pairs, minimal test sets, and generated xUnit code
5. Use **Copy to Clipboard** to copy generated test code, or **Export All Tests** to save test files to disk

## How It Works

1. **Decision Extraction** — Roslyn's `CSharpSyntaxWalker` visits `if`, `while`, `for`, `do-while`, and ternary statements, extracting compound boolean conditions (those containing `&&`, `||`, or `!`)

2. **Expression Parsing** — Each Roslyn `ExpressionSyntax` is converted to an internal AST with `And`, `Or`, `Not`, and `Condition` nodes. Atomic conditions are labeled A, B, C, etc.

3. **Truth Table Generation** — All 2^N combinations are evaluated for N conditions

4. **Independence Pair Finding** — For each condition, pairs of truth table rows are identified where only that condition differs and the decision outcome changes (Unique-Cause MC/DC criterion)

5. **Minimal Test Set Selection** — A greedy set cover algorithm selects independence pairs that maximize row reuse, producing a minimal set of typically N+1 test cases

6. **Test Code Generation** — xUnit `[Theory]` methods with `[InlineData]` attributes are generated for each decision

## License

This project is provided as-is for educational and testing purposes.
