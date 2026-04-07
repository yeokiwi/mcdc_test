using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace McdcTool.Core.Parsing;

public class ProjectFileInfo
{
    public string Name { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public List<string> CSharpFiles { get; set; } = new();
}

public class SolutionInfo
{
    public string Name { get; set; } = "";
    public string SolutionPath { get; set; } = "";
    public List<ProjectFileInfo> Projects { get; set; } = new();
}

public static class SolutionParser
{
    private static readonly Regex ProjectLineRegex = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""",
        RegexOptions.Compiled);

    public static SolutionInfo ParseSolution(string slnPath)
    {
        var fullPath = Path.GetFullPath(slnPath);
        var slnDir = Path.GetDirectoryName(fullPath)!;
        var slnContent = File.ReadAllText(fullPath);

        var solution = new SolutionInfo
        {
            Name = Path.GetFileNameWithoutExtension(fullPath),
            SolutionPath = fullPath,
            Projects = new List<ProjectFileInfo>()
        };

        foreach (Match match in ProjectLineRegex.Matches(slnContent))
        {
            var projectName = match.Groups[1].Value;
            var projectRelativePath = match.Groups[2].Value.Replace('\\', Path.DirectorySeparatorChar);
            var projectFullPath = Path.GetFullPath(Path.Combine(slnDir, projectRelativePath));

            // Only include C# projects (.csproj)
            if (!projectFullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!File.Exists(projectFullPath))
                continue;

            var projectInfo = ParseProject(projectFullPath);
            solution.Projects.Add(projectInfo);
        }

        return solution;
    }

    public static ProjectFileInfo ParseProject(string csprojPath)
    {
        var fullPath = Path.GetFullPath(csprojPath);
        var projectDir = Path.GetDirectoryName(fullPath)!;

        var project = new ProjectFileInfo
        {
            Name = Path.GetFileNameWithoutExtension(fullPath),
            ProjectPath = fullPath
        };

        // For SDK-style projects, .cs files are auto-included from the project directory
        // Check if it's an SDK-style project
        try
        {
            var doc = XDocument.Load(fullPath);
            var sdkAttr = doc.Root?.Attribute("Sdk")?.Value;

            if (sdkAttr != null)
            {
                // SDK-style project: scan directory for .cs files
                // Exclude bin/, obj/, and common non-source directories
                project.CSharpFiles = ScanForCSharpFiles(projectDir);
            }
            else
            {
                // Legacy project: look for <Compile Include="..." /> elements
                var ns = doc.Root?.GetDefaultNamespace();
                var compileItems = doc.Descendants()
                    .Where(e => e.Name.LocalName == "Compile")
                    .Select(e => e.Attribute("Include")?.Value)
                    .Where(v => v != null && v.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    .Select(v => Path.GetFullPath(Path.Combine(projectDir, v!)))
                    .Where(File.Exists)
                    .ToList();

                project.CSharpFiles = compileItems;
            }
        }
        catch
        {
            // Fallback: scan directory
            project.CSharpFiles = ScanForCSharpFiles(projectDir);
        }

        return project;
    }

    public static List<string> GetCSharpFiles(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            var solution = ParseSolution(fullPath);
            return solution.Projects.SelectMany(p => p.CSharpFiles).Distinct().OrderBy(f => f).ToList();
        }

        if (fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            var project = ParseProject(fullPath);
            return project.CSharpFiles.OrderBy(f => f).ToList();
        }

        if (File.Exists(fullPath) && fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { fullPath };
        }

        if (Directory.Exists(fullPath))
        {
            return ScanForCSharpFiles(fullPath);
        }

        return new List<string>();
    }

    private static List<string> ScanForCSharpFiles(string directory)
    {
        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var relative = Path.GetRelativePath(directory, f);
                // Exclude common build/generated directories
                return !relative.StartsWith("bin" + Path.DirectorySeparatorChar) &&
                       !relative.StartsWith("obj" + Path.DirectorySeparatorChar) &&
                       !relative.StartsWith(".") &&
                       !relative.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                       !relative.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar);
            })
            .OrderBy(f => f)
            .ToList();
    }
}
