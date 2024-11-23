using Newtonsoft.Json;
using RoslynLibrary.Models;
using RoslynLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Core.Services
{
    internal class DiagnosticsAnalyzerConfigurationServiceLastDevBlog : IDiagnosticsAnalyzerConfigurationService
    {
        private List<DiagnosticAnalyzerModel> _diagnosticAnalyzerModels = new List<DiagnosticAnalyzerModel>()
        {
            //new DiagnosticAnalyzerModel()
            //{
            //    DiagnosticId = "OnDoorKnockedError",
            //    Description = "The code should not contain 'OnDoorKnocked'",
            //    Category = "Syntax",
            //    MessageFormat = "Данный код устарел! Замените 'OnDoorKnocked(DoorKnocker,BasePlayer)' на 'OnItemCraftCancelled(ItemCraftTask task, ItemCrafter itemCrafter)'",
            //    Title = "OnDoorKnocked detected",
            //    RegexPattern = @"OnDoorKnocked\(\s*DoorKnocker\s[\d\w]+,\sBasePlayer\s[\d\w]+\)"
            //}
        };

        public List<DiagnosticAnalyzerModel> AnalyzeBaseOverrideModels => _diagnosticAnalyzerModels;
    
        public DiagnosticsAnalyzerConfigurationServiceLastDevBlog(DiagnosticsAnalyzerConfigurationService configurationService)//DiagnosticsAnalyzerConfigurationService configurationService)
        {
            _diagnosticAnalyzerModels.AddRange(
                configurationService.Diagnostics.Select(s =>
                {
                    var hookName = s.Key.Split("(").First();
                    var parameters = s.Key.Split("(").Last().Replace(")", "").Split(",");

                    return new DiagnosticAnalyzerModel()
                    {
                        AnalyzeSource = s,
                        DiagnosticId = $"{hookName}Error",
                        Description = $"The code should not contain '{hookName}'",
                        Category = "Syntax",
                        MessageFormat = $"Данный хук с более старой версии Rust! Замените '{s.Key}' на {s.Value}",
                        Title = $"{hookName} detected",
                        RegexPattern = @$"{hookName}\({string.Join(',', parameters.Select(parametr => @$"\s*{parametr}\s[\d\w]+"))}\)"
                    };
                })
            );
        }
    }
}
