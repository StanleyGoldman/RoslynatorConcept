using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynatorConcept
{
    public class FromFileLoader : IAnalyzerAssemblyLoader
    {
        public static FromFileLoader Instance = new FromFileLoader();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            var slnDir = GetSolutionPath();

            var analyzerPaths = GetAnalyzers(slnDir, "packages\\Roslynator.Analyzers.1.9.0\\analyzers\\dotnet\\cs");

            var sampleSlnPath = Path.Combine(slnDir, "Sample", "TestConsoleApp1.sln");
            var openSolutionAsync = await MSBuildWorkspace.Create().OpenSolutionAsync(sampleSlnPath);

            var projects = openSolutionAsync.Projects.ToList();
            foreach (var project in projects)
            {
                var customAnalyzedProject = project;
                foreach (var analyzer in analyzerPaths)
                {
                    customAnalyzedProject =
                        customAnalyzedProject.AddAnalyzerReference(new AnalyzerFileReference(analyzer,
                            FromFileLoader.Instance));
                }

                var compilation = await customAnalyzedProject.GetCompilationAsync();
                foreach (var diagnostic in compilation.GetDiagnostics())
                {
                    Console.WriteLine($"{diagnostic.Id} - {diagnostic.GetMessage()}");
                }
            }

            Console.ReadLine();
        }

        private static string[] GetAnalyzers(string solutionPath, string packagesRoslynatorAnalyzersAnalyzersDotnetCs)
        {
            var analyzerDir = Path.Combine(solutionPath, packagesRoslynatorAnalyzersAnalyzersDotnetCs);
            return new DirectoryInfo(analyzerDir).GetFiles("*.dll").Select(info => info.FullName).ToArray();
        }

        private static string GetSolutionPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            var localPath = new System.Uri(path).LocalPath;
            return new DirectoryInfo(localPath).Parent.Parent.Parent.FullName;
        }
    }
}
