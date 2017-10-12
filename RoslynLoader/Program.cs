using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var baseDirectory = currentDirectory.Substring(0, currentDirectory.IndexOf("RoslynCore", StringComparison.Ordinal) + 10);

                // Bug introduced by Visual Studio 15.3: https://github.com/Microsoft/msbuild/issues/2369
                Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional");
                Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");

                var props = new Dictionary<string, string>();
                props.Add("Platform", "AnyCPU");
                props.Add("Configuration", "Debug");
                props.Add("CheckForSystemRuntimeDependency", "true");
                props.Add("TargetFramework", "net462");

                var workspace = MSBuildWorkspace.Create(props);
                var solution = workspace.OpenSolutionAsync(Path.Combine(baseDirectory, "RoslynCore.sln")).Result;
                Console.WriteLine($"Found solution: {solution.FilePath}");

                var projectGraph = solution.GetProjectDependencyGraph();
                var projects = solution.Projects.ToArray();
                Console.WriteLine("Projects:");
                foreach (var project in projects)
                {
                    Console.WriteLine($"  {project.Name}");
                }

                foreach (var projectId in projectGraph.GetTopologicallySortedProjects())
                {
                    var project = solution.GetProject(projectId)
                        .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    var compilation = project.GetCompilationAsync().Result;
                    var errors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();

                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        var semanticModel = compilation.GetSemanticModel(syntaxTree);
                        Console.WriteLine(syntaxTree.GetText());
                    }
                }

                Console.WriteLine("Finished cleaning");
                Console.Read();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
