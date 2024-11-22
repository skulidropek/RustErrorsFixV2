using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RoslynLibrary.Services.Interfaces;

namespace RoslynLibrary.Services
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiagnosticAnalyzerService : DiagnosticAnalyzer
    {
        private Dictionary<string, DiagnosticDescriptor> _supportedDiagnostics = new Dictionary<string, DiagnosticDescriptor>();
        private IDiagnosticsAnalyzerConfigurationService _diagnosticsAnalyzerConfigurationService;

        public void Set(IDiagnosticsAnalyzerConfigurationService diagnosticsAnalyzerConfigurationService)
        {
            _diagnosticsAnalyzerConfigurationService = diagnosticsAnalyzerConfigurationService;
            _supportedDiagnostics = diagnosticsAnalyzerConfigurationService.AnalyzeBaseOverrideModels.ToDictionary(s => s.RegexPattern, s => new DiagnosticDescriptor(s.DiagnosticId, s.Title, s.MessageFormat, s.Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: s.Description));
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics.Values.ToImmutableArray();

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            string code = context.Node.ToFullString();

            foreach (var diagnosticAnalyzer in _diagnosticsAnalyzerConfigurationService.AnalyzeBaseOverrideModels)
            {
                if (Regex.IsMatch(code, diagnosticAnalyzer.RegexPattern))
                {
                    DiagnosticDescriptor rule;
                    if (!_supportedDiagnostics.ContainsKey(diagnosticAnalyzer.RegexPattern))
                    {
                        rule = new DiagnosticDescriptor(diagnosticAnalyzer.DiagnosticId, diagnosticAnalyzer.Title, diagnosticAnalyzer.MessageFormat, diagnosticAnalyzer.Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: diagnosticAnalyzer.Description);
                        _supportedDiagnostics.Add(diagnosticAnalyzer.RegexPattern, rule);
                    }
                    else
                    {
                        rule = _supportedDiagnostics[diagnosticAnalyzer.RegexPattern];
                    }

                    var diag = Diagnostic.Create(rule, context.Node.GetLocation());
                    context.ReportDiagnostic(diag);
                }
            }
        }
    }
}
