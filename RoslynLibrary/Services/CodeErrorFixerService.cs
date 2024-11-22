using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using RoslynLibrary.Models;
using RoslynLibrary.Extensions;
using RoslynLibrary.Services;
using System.Text.RegularExpressions;
using RoslynLibrary.Services.Analyzer;
using System.Text;
using RoslynLibrary.Services.Interfaces;

public class CodeErrorFixerService : CSharpSyntaxRewriter
{
    private readonly PluginDiagnosticsAnalyzerService _pluginDiagnosticsAnalyzer;
    private readonly IEnumerable<IAnalyzer> _analyzers;
    private IEnumerable<AnalyzeBaseModel> _analyzeBaseModels;
    private IEnumerable<CompilationErrorModel> _compilationErrors;


    public CodeErrorFixerService(PluginDiagnosticsAnalyzerService pluginDiagnosticsAnalyzer, IEnumerable<IAnalyzer> analyzers)
    {
        _pluginDiagnosticsAnalyzer = pluginDiagnosticsAnalyzer;
        _analyzers = analyzers;
    }

    public async Task<SyntaxNode> VisitAndFixErrors(SyntaxNode node, IEnumerable<AnalyzeBaseModel> analyzeBaseModels, IDiagnosticsAnalyzerConfigurationService diagnosticsAnalyzerConfigurationService)
    {
        _compilationErrors = await _pluginDiagnosticsAnalyzer.AnalyzeCompilationAsync(node.SyntaxTree, diagnosticsAnalyzerConfigurationService);

        if (_compilationErrors.Count() == 0)
            return node;

        List<AnalyzeBaseModel> analyzeAllModels = new List<AnalyzeBaseModel>();
        List<AnalyzeBaseModel> analyzeBaseNotAllModels = new List<AnalyzeBaseModel>();

        foreach(var analyzeBaseModel in analyzeBaseModels)
        {
            if (analyzeBaseModel.AnalyzeType == AnalyzeType.All)
            {
                analyzeAllModels.Add(analyzeBaseModel);
                continue;
            }

            analyzeBaseNotAllModels.Add(analyzeBaseModel);
        }

        _analyzeBaseModels = analyzeAllModels;

        foreach(var analysisBaseModel in analyzeAllModels)
        {
            node = FixCompilationError(node, DeclarationType.All);
        }

        if(analyzeBaseNotAllModels == null || analyzeBaseNotAllModels.Count == 0)
            return node;

        _analyzeBaseModels = analyzeBaseNotAllModels;

        return base.Visit(node);
    }

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) => FixCompilationError(node, DeclarationType.Property);

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax method) => FixCompilationError(method, DeclarationType.Method);

    public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node) => FixCompilationError(node, DeclarationType.Field);

    public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node) => FixCompilationError(node, DeclarationType.Constructor);
    
    public SyntaxNode FixCompilationError(SyntaxNode node, DeclarationType declarationType)
    {
        var location = node.GetLocation().GetStartAndEndLines();

        if (!TryGetCompilationError(location, out List<AnalyzeCompilationErrorModel> errors))
            return node;

        var nodeText = node.ToFullString();

        foreach (var error in errors)
        {
            foreach (var analyze in error.AnalyzeBaseModels.Where(s =>
                                        s.DeclarationType == DeclarationType.All ||
                                        s.DeclarationType == declarationType)
                )
            {
                var regexPattern = analyze.RegexPattern;
                var regexReplacement = analyze.RegexReplacement;

                var match = Regex.Match(error.CompilationErrorModel.Text, analyze.ErrorText);

                for (int i = 0; i < match.Groups.Count; i++)
                {
                    regexPattern = regexPattern.Replace($"$errorGroup{i}", match.Groups[i].Value);
                    regexReplacement = regexReplacement.Replace($"$errorGroup{i}", match.Groups[i].Value);
                }

                IAnalyzer analyzer = _analyzers.First(a => a.CanHandle(analyze.AnalyzeType));
                nodeText = analyzer.Analyze(error.CompilationErrorModel, nodeText, regexPattern, regexReplacement);
            }
        }

        if (DeclarationType.All == declarationType)
            return ToSyntaxNodeAll(nodeText);

        return ToSyntaxNode(nodeText);
    }

    protected static SyntaxNode ToSyntaxNode(string code)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        var getRoot = syntaxTree.GetRoot().DescendantNodes().FirstOrDefault();
        return getRoot;
    }
    protected static SyntaxNode ToSyntaxNodeAll(string code)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        var getRoot = syntaxTree.GetRoot();
        return getRoot;
    }

    public bool TryGetCompilationError((int, int) location, out List<AnalyzeCompilationErrorModel> errorsOut)
    {
        errorsOut = new List<AnalyzeCompilationErrorModel>();

        foreach (var error in _compilationErrors)
        {
            var errorOut = new AnalyzeCompilationErrorModel()
            {
                CompilationErrorModel = error,
                AnalyzeBaseModels = new List<AnalyzeBaseModel>()
            };

            if (location.Item1 <= error.Line && error.Line <= location.Item2)
            {
                var analyzeBaseModel = _analyzeBaseModels.Where(s => Regex.IsMatch(error.Text, s.ErrorText));
                if (analyzeBaseModel != null)
                {
                    errorOut.AnalyzeBaseModels.AddRange(analyzeBaseModel);
                }
            }

            if(errorOut.AnalyzeBaseModels != null && errorOut.AnalyzeBaseModels.Count > 0)
            {
                errorsOut.Add(errorOut);
            }
        }

        return errorsOut.Count > 0;
    }
}
