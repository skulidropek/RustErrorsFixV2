using RoslynLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Core.Services.Interfaces;
using WpfApp.Extensions;

namespace WpfApp.Core.Services
{
    class FileConfigurationService
    {
        private readonly Dictionary<string, AnalyzeConfigurationService> _analyzeConfigurationServices;
        private readonly Dictionary<string, DiagnosticsAnalyzerConfigurationService> _diagnosticsAnalyzeConfigurationServices;

        public FileConfigurationService()
        {
            _analyzeConfigurationServices = new Dictionary<string, AnalyzeConfigurationService>();
            _diagnosticsAnalyzeConfigurationServices = new Dictionary<string, DiagnosticsAnalyzerConfigurationService>();
            Deserialize();
        }

        public void Deserialize()
        {
            if (!Directory.Exists("Files"))
                Directory.CreateDirectory("Files");

            foreach (var file in Directory.GetFiles("Files"))
            {
                var fullPath = Path.GetFullPath(file);
                var configuration = JsonFileSerializer.Deserialize<AnalyzeConfigurationService>(fullPath);
                
                if(configuration == null || configuration.ConfigurationName == null || configuration.AnalyzeBaseModels == null)
                {
                    var diagnosticsConfiguration = JsonFileSerializer.Deserialize<DiagnosticsAnalyzerConfigurationService>(fullPath);

                    if (_diagnosticsAnalyzeConfigurationServices.ContainsKey(diagnosticsConfiguration.ConfigurationName))
                    {
                        foreach(var diagnostics in diagnosticsConfiguration.Diagnostics)
                            _diagnosticsAnalyzeConfigurationServices[configuration.ConfigurationName].Diagnostics.Add(diagnostics.Key, diagnostics.Value);
                    }

                    _diagnosticsAnalyzeConfigurationServices.Add(configuration.ConfigurationName, diagnosticsConfiguration);
                    continue;
                }

                if (_analyzeConfigurationServices.ContainsKey(configuration.ConfigurationName))
                {
                    _analyzeConfigurationServices[configuration.ConfigurationName].AnalyzeBaseModels.AddRange(configuration.AnalyzeBaseModels);
                }

                _analyzeConfigurationServices.Add(configuration.ConfigurationName, configuration);
            }
        }

        public (AnalyzeConfigurationService, DiagnosticsAnalyzerConfigurationService?)? Get(string name)
        {
            if (!_analyzeConfigurationServices.ContainsKey(name))
                return null;

            var analyzeConfigurationService = _analyzeConfigurationServices[name];

            DiagnosticsAnalyzerConfigurationService diagnosticsAnalyzerConfigurationService = null;
            if (_diagnosticsAnalyzeConfigurationServices.ContainsKey(name))
                diagnosticsAnalyzerConfigurationService = _diagnosticsAnalyzeConfigurationServices[name];

            return (analyzeConfigurationService, diagnosticsAnalyzerConfigurationService);
        }

        public IEnumerable<DiagnosticsAnalyzerConfigurationService> GetDiagnosticsAnalyzerConfigurationServices() => _diagnosticsAnalyzeConfigurationServices.Values.Cast<DiagnosticsAnalyzerConfigurationService>();
        public IEnumerable<AnalyzeConfigurationService> GetAnalyzeConfigurationServices() => _analyzeConfigurationServices.Values.Cast<AnalyzeConfigurationService>();
    }
}
