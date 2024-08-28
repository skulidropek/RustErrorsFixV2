﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RoslynLibrary.Models;
using RoslynLibrary.Sections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;

namespace RoslynLibrary.Services
{
    public class PluginDiagnosticsAnalyzerService
    {
        private readonly ManagedSection _managedSection;
        private readonly DiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly AssemblyDataPoolService _assemblyDataPoolService;
        public PluginDiagnosticsAnalyzerService(IOptions<ManagedSection> managedSection, DiagnosticAnalyzerService diagnosticAnalyzerService, AssemblyDataPoolService assemblyDataPoolService)
        {
            _managedSection = managedSection.Value;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
            _assemblyDataPoolService = assemblyDataPoolService;
        }

        public async Task<List<CompilationErrorModel>> AnalyzeCompilationAsync(string plugin)
        {

            var tree = CSharpSyntaxTree.ParseText(plugin);
            return await AnalyzeCompilationAsync(tree);
        }

        public async Task<List<CompilationErrorModel>> AnalyzeCompilationAsync(SyntaxTree syntaxTree)
        {
            var errors = new List<CompilationErrorModel>();

            foreach (var diagnostic in await GetAnalysisResultsAsync(CreateAnalyzer(syntaxTree, "Plugin", _managedSection.Path)))
            {
                if (diagnostic.DefaultSeverity == DiagnosticSeverity.Error)
                {
                    errors.Add(new CompilationErrorModel()
                    {
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line,
                        Symbol = diagnostic.Location.GetLineSpan().StartLinePosition.Character,
                        Text = diagnostic.GetMessage(),
                        Location = diagnostic.Location
                    });
                }
            }

            return errors;
        }

        private CSharpCompilation CreateAnalyzer(SyntaxTree source, string compilationName, string managedFolder)
        {
            var references = Directory.GetFiles(managedFolder)
                                     .Where(f => !f.Contains("Newtonsoft.Json.dll"))
                                     .Select(path => MetadataReference.CreateFromFile(path.Replace("\n", "").Replace("\r", "")))
                                     .ToList();

            return CSharpCompilation.Create(compilationName,
                                            syntaxTrees: new[] { source },
                                            references: references,
                                            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalysisResultsAsync(CSharpCompilation compilation)
        {
            List<DiagnosticAnalyzer> diagnosticAnalyzers = new List<DiagnosticAnalyzer>
            {
                _diagnosticAnalyzerService
            };

            var analyzers = diagnosticAnalyzers.ToImmutableArray();
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();

            diagnostics = diagnostics.AddRange(compilation.GetDiagnostics());
            return diagnostics;
        }
    }
}
