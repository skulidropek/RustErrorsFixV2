using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Ookii.Dialogs.Wpf;
using RoslynLibrary.Models;
using RoslynLibrary.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp.Core;
using WpfApp.Core.Services;
using WpfApp.Core.Services.Interfaces;
using WpfApp.Models;
using WpfApp.View;

namespace WpfApp.ViewModel
{
    internal class RoslynViewModel : ViewModelBase
    {
        private readonly LangService _langService;
        private readonly PageService _pageService;
        private readonly PluginFixService _pluginFixService;
        private readonly PluginDiagnosticsAnalyzerService _pluginDiagnosticsAnalyzerService;
        private readonly ConfigurationService _configurationService;
        private Microsoft.CodeAnalysis.SyntaxTree _syntaxTree;

        public IEnumerable<AnalyzeBaseOverrideModel> YourItems => _configurationService.AnalyzeConfiguration.AnalyzeBaseModels.OrderByDescending(s => s.IsActive);

        private string _pluginPath; 
        public string ChoiceButtonText => _langService.GetLang("Fix");
        public string ErrorsPluginText => _langService.GetLang("ErrorsPlugin");
        public string BackText => _langService.GetLang("Back");
        public string FixSelectionText => _langService.GetLang("FixSelection");

        public ICommand ChoicePluginCommand { get; private set; }
        public ICommand RoslynPageOpenCommand { get; private set; }
        public ICommand BackCommand { get; private set; }

        private ObservableCollection<string> _errors = new ObservableCollection<string>();

        public ObservableCollection<string> Errors
        {
            get { return _errors; }
            set
            {
                if (_errors != value)
                {
                    _errors = value;
                    OnPropertyChanged("Errors");
                }
            }
        }

        public RoslynViewModel()
        {
            _pageService = ServiceManager.ServiceProvider.GetRequiredService<PageService>();
            _langService = ServiceManager.ServiceProvider.GetRequiredService<LangService>();
            _pluginFixService = ServiceManager.ServiceProvider.GetRequiredService<PluginFixService>();
            _pluginDiagnosticsAnalyzerService = ServiceManager.ServiceProvider.GetRequiredService<PluginDiagnosticsAnalyzerService>();
            _configurationService = ServiceManager.ServiceProvider.GetRequiredService<ConfigurationService>();

            ChoicePluginCommand = new RelayCommand(ChoicePluginCommandExecute);
            RoslynPageOpenCommand = new RelayCommand(RoslynPageOpenCommandExecute);
            BackCommand = new RelayCommand(BackCommandExecute);

            _langService.Subscribe(OnLangChanged);

            _langService.OnLangChangedInvoke();

            string path = GetPathOpenFileDialog();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show(_langService.GetLang("NotSelectFile"));
                Task.Run(async () =>
                {
                    await Task.Delay(10);
                    TheardForm.Call(() => _pageService.OpenChocePlugins());
                });
                return;
            }

            _pluginPath = path;

            var plugin = System.IO.File.ReadAllText(path);

            _syntaxTree = CSharpSyntaxTree.ParseText(plugin);

            var errors = _pluginDiagnosticsAnalyzerService.AnalyzeCompilationAsync(_syntaxTree, _configurationService.DiagnosticsAnalyzerConfiguration).GetAwaiter().GetResult();

            Errors = new ObservableCollection<string>(errors.Select(s => $"[{s.Line}:{s.Symbol}] " + s.Text));

            foreach (var error in Errors)
            {
                foreach (var roslynError in _configurationService.AnalyzeConfiguration.AnalyzeBaseModels)
                {
                    if (Regex.IsMatch(error, roslynError.ErrorText))
                        roslynError.IsActive = true;
                }
            }
        }

        ~RoslynViewModel()
        {
            _langService.UnSubscribe(OnLangChanged);
        }

        public void OnLangChanged(bool en)
        {
            OnPropertyChanged("ChoiceButtonText");
            OnPropertyChanged("ErrorsPluginText");
            OnPropertyChanged("BackText");
            OnPropertyChanged("FixSelectionText");
        }

        public async void ChoicePluginCommandExecute(object obj)
        {
            var plugin = (await _pluginFixService.Fix(_syntaxTree, _configurationService.AnalyzeConfiguration.AnalyzeBaseModels, _configurationService.DiagnosticsAnalyzerConfiguration)).ToFullString();

            _syntaxTree = CSharpSyntaxTree.ParseText(plugin);

            var errors = _pluginDiagnosticsAnalyzerService.AnalyzeCompilationAsync(_syntaxTree, _configurationService.DiagnosticsAnalyzerConfiguration).GetAwaiter().GetResult();

            Errors = new ObservableCollection<string>(errors.Select(s => $"[{s.Line}:{s.Symbol}] " + s.Text));

            plugin = Regex.Replace(plugin, @"(\[Info\("".*"", "").*("", "".*""\)\])", "/*ПЛАГИН БЫЛ ПОФИКШЕН С ПОМОЩЬЮ ПРОГРАММЫ СКАЧАНОЙ С https://discord.gg/dNGbxafuJn */ $1https://discord.gg/dNGbxafuJn$2");

            plugin += "\n/* Boosty - https://boosty.to/skulidropek \n" +
                    "Discord - https://discord.gg/k3hXsVua7Q \n" +
                    "Discord The Rust Bay - https://discord.gg/Zq3TVjxKWk  */";

            foreach (var roslynError in _configurationService.AnalyzeConfiguration.AnalyzeBaseModels)
                roslynError.IsActive = false;

            foreach (var error in Errors)
            {
                foreach (var roslynError in _configurationService.AnalyzeConfiguration.AnalyzeBaseModels)
                {
                    if (Regex.IsMatch(error, roslynError.ErrorText))
                        roslynError.IsActive = true;
                }
            }

            OnPropertyChanged(nameof(YourItems));

            System.IO.File.WriteAllText(_pluginPath + "FIX.cs", plugin);
        }

        public void RoslynPageOpenCommandExecute(object obj)
        {
            _pageService.OpenRoslyn();
        }

        private void BackCommandExecute(object obj)
        {
            _pageService.OpenChocePlugins();
        }

        private string GetPathOpenFileDialog()
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();

            openFileDialog.Filter = "Плагины раст (*.cs)|*.cs";

            if (openFileDialog.ShowDialog() == false)
            {
                return "";
            }

            return openFileDialog.FileName;
        }
    }
}
