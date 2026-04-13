using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class SolutionParserTests
{
    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void GetCSharpFiles_SingleCsFile_ReturnsThatFile()
    {
        var dir = CreateTempDir();
        var csFile = Path.Combine(dir, "Test.cs");
        File.WriteAllText(csFile, "class C {}");

        try
        {
            var files = SolutionParser.GetCSharpFiles(csFile);
            Assert.Single(files);
            Assert.Equal(Path.GetFullPath(csFile), files[0]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetCSharpFiles_Directory_ScansRecursively()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "A.cs"), "class A {}");
        var sub = Path.Combine(dir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "B.cs"), "class B {}");

        try
        {
            var files = SolutionParser.GetCSharpFiles(dir);
            Assert.Equal(2, files.Count);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetCSharpFiles_Directory_ExcludesBinObj()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "A.cs"), "class A {}");
        var binDir = Path.Combine(dir, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "B.cs"), "class B {}");
        var objDir = Path.Combine(dir, "obj");
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(objDir, "C.cs"), "class C {}");

        try
        {
            var files = SolutionParser.GetCSharpFiles(dir);
            Assert.Single(files);
            Assert.Contains("A.cs", files[0]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetCSharpFiles_NonExistentPath_ReturnsEmpty()
    {
        var files = SolutionParser.GetCSharpFiles("/nonexistent/path/to/nowhere");
        Assert.Empty(files);
    }

    [Fact]
    public void ParseProject_SdkStyleProject_FindsCsFiles()
    {
        var dir = CreateTempDir();
        var csproj = Path.Combine(dir, "Test.csproj");
        File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");
        File.WriteAllText(Path.Combine(dir, "Class1.cs"), "class C1 {}");
        File.WriteAllText(Path.Combine(dir, "Class2.cs"), "class C2 {}");

        try
        {
            var project = SolutionParser.ParseProject(csproj);

            Assert.Equal("Test", project.Name);
            Assert.Equal(2, project.CSharpFiles.Count);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ParseProject_LegacyProject_FindsCompileItems()
    {
        var dir = CreateTempDir();
        var csFile = Path.Combine(dir, "MyClass.cs");
        File.WriteAllText(csFile, "class MyClass {}");

        var csproj = Path.Combine(dir, "Test.csproj");
        File.WriteAllText(csproj, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <Compile Include=""MyClass.cs"" />
  </ItemGroup>
</Project>");

        try
        {
            var project = SolutionParser.ParseProject(csproj);

            Assert.Equal("Test", project.Name);
            Assert.Single(project.CSharpFiles);
            Assert.Contains("MyClass.cs", project.CSharpFiles[0]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ParseProject_InvalidXml_FallsBackToScan()
    {
        var dir = CreateTempDir();
        var csproj = Path.Combine(dir, "Test.csproj");
        File.WriteAllText(csproj, "this is not valid xml");
        File.WriteAllText(Path.Combine(dir, "Fallback.cs"), "class F {}");

        try
        {
            var project = SolutionParser.ParseProject(csproj);

            // Should fallback to directory scan
            Assert.True(project.CSharpFiles.Count >= 1);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ParseSolution_ValidSln_FindsProjects()
    {
        var dir = CreateTempDir();
        var projDir = Path.Combine(dir, "MyProject");
        Directory.CreateDirectory(projDir);

        var csproj = Path.Combine(projDir, "MyProject.csproj");
        File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
</Project>");
        File.WriteAllText(Path.Combine(projDir, "Program.cs"), "class Program {}");

        var slnPath = Path.Combine(dir, "Test.sln");
        File.WriteAllText(slnPath, $@"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""MyProject"", ""MyProject{Path.DirectorySeparatorChar}MyProject.csproj"", ""{{12345678-1234-1234-1234-123456789012}}""
EndProject
");

        try
        {
            var solution = SolutionParser.ParseSolution(slnPath);

            Assert.Equal("Test", solution.Name);
            Assert.Single(solution.Projects);
            Assert.Equal("MyProject", solution.Projects[0].Name);
            Assert.NotEmpty(solution.Projects[0].CSharpFiles);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ParseSolution_SkipsNonCsprojProjects()
    {
        var dir = CreateTempDir();
        var slnPath = Path.Combine(dir, "Test.sln");
        File.WriteAllText(slnPath, @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""SolutionFolder"", ""SolutionFolder"", ""{GUID}""
EndProject
");

        try
        {
            var solution = SolutionParser.ParseSolution(slnPath);
            Assert.Empty(solution.Projects);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ParseSolution_SkipsMissingCsprojFiles()
    {
        var dir = CreateTempDir();
        var slnPath = Path.Combine(dir, "Test.sln");
        File.WriteAllText(slnPath, @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Missing"", ""Missing\Missing.csproj"", ""{GUID}""
EndProject
");

        try
        {
            var solution = SolutionParser.ParseSolution(slnPath);
            Assert.Empty(solution.Projects);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetCSharpFiles_CsprojPath_DelegatesToParseProject()
    {
        var dir = CreateTempDir();
        var csproj = Path.Combine(dir, "Test.csproj");
        File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
</Project>");
        File.WriteAllText(Path.Combine(dir, "Code.cs"), "class C {}");

        try
        {
            var files = SolutionParser.GetCSharpFiles(csproj);
            Assert.Single(files);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetCSharpFiles_SlnPath_DelegatesToParseSolution()
    {
        var dir = CreateTempDir();
        var projDir = Path.Combine(dir, "Proj");
        Directory.CreateDirectory(projDir);
        File.WriteAllText(Path.Combine(projDir, "Proj.csproj"),
            @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(projDir, "A.cs"), "class A {}");

        var slnPath = Path.Combine(dir, "S.sln");
        File.WriteAllText(slnPath, $@"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""Proj"", ""Proj{Path.DirectorySeparatorChar}Proj.csproj"", ""{{GUID}}""
EndProject
");

        try
        {
            var files = SolutionParser.GetCSharpFiles(slnPath);
            Assert.NotEmpty(files);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ProjectFileInfo_DefaultProperties()
    {
        var info = new ProjectFileInfo();
        Assert.Equal("", info.Name);
        Assert.Equal("", info.ProjectPath);
        Assert.Empty(info.CSharpFiles);
    }

    [Fact]
    public void SolutionInfo_DefaultProperties()
    {
        var info = new SolutionInfo();
        Assert.Equal("", info.Name);
        Assert.Equal("", info.SolutionPath);
        Assert.Empty(info.Projects);
    }
}
