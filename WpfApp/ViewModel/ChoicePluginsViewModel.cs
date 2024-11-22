using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using RoslynLibrary.Sections;
using RoslynLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp.Core;
using WpfApp.Core.Services;
using WpfApp.Core.Services.Interfaces;
using WpfApp.Models;

namespace WpfApp.ViewModel
{
    internal class ChoicePluginsViewModel : ViewModelBase
    {
        private readonly LangService _langService;
        private readonly PageService _pageService;
        private readonly ConfigurationService _configurationService;
        private readonly FileConfigurationService _fileConfigurationService;
        //private bool _roslynReferenseHave => !string.IsNullOrEmpty(_managedSection.Path);

        //  public string RoslynButtonText => _roslynReferenseHave ? _langService.GetLang("Select") : _langService.GetLang("SelectFolderManaged");

        public string SelectPluginText => _langService.GetLang("SelectPlugin");

        public ObservableCollection<ButtonModel> ButtonModels { get; } = new ObservableCollection<ButtonModel>();

        public ICommand RoslynPageOpenCommand { get; private set; }
        public ICommand ResetManagedFolderCommand { get; private set; }

        public ChoicePluginsViewModel()
        {
            //_managedSection = ServiceManager.ServiceProvider.GetRequiredService<IOptions<ManagedSection>>().Value;
            _pageService = ServiceManager.ServiceProvider.GetRequiredService<PageService>();
            _langService = ServiceManager.ServiceProvider.GetRequiredService<LangService>();
            _configurationService = ServiceManager.ServiceProvider.GetRequiredService<ConfigurationService>();
            _fileConfigurationService = ServiceManager.ServiceProvider.GetRequiredService<FileConfigurationService>();

            var registryService = ServiceManager.ServiceProvider.GetRequiredService<RegistryService>();

            //SelectFolderManaged

            _fileConfigurationService.GetAnalyzeConfigurationServices().Select(s => new ButtonModel(s)).ToList().ForEach(ButtonModels.Add);

            RoslynPageOpenCommand = new RelayCommand(RoslynPageOpenCommandExecute);
            ResetManagedFolderCommand = new RelayCommand(ResetManagedFolderCommandExecute);//,// (obj) => !string.IsNullOrEmpty(_managedSection.Path));

            _langService.Subscribe(OnLangChanged);

            _langService.OnLangChangedInvoke();
        }

        // <Image Source="/wwwroot/Images/reset.png"/>
        private void ResetManagedFolderCommandExecute(object obj)
        {
            if (obj is not ButtonModel)
                return;

            var button = obj as ButtonModel;

            var registryService = ServiceManager.ServiceProvider.GetRequiredService<RegistryService>();

            var key = button.AnalyzeConfigurationService.ConfigurationName + "RustReferensePath";

            if (string.IsNullOrEmpty(registryService.GetValue(key)))
            {
                registryService.AddValue(key, "");
                return;
            }

            registryService.SetValue(key, "");
            button.UpdateUI();
        }

        ~ChoicePluginsViewModel()
        {
            _langService.UnSubscribe(OnLangChanged);
        }

        public void OnLangChanged(bool en)
        {
            foreach(var button in ButtonModels)
            {
                button.UpdateUI();
            }
        }

        public void RoslynPageOpenCommandExecute(object obj)
        {
            if(obj is not ButtonModel)
                return;

            var button = obj as ButtonModel;


            if (button.ButtonResetVisibility == Visibility.Hidden)
            {
                var path = GetPathOpenFolderDialog();

                if(string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("Need select Managed folder");
                    return;
                }

                //var overrideSection = new ManagedSectionOverride();
                //overrideSection.Path = path;
                //_managedSection.Path = path;

                var registryService = ServiceManager.ServiceProvider.GetRequiredService<RegistryService>();

                var key = button.AnalyzeConfigurationService.ConfigurationName + "RustReferensePath";

                if (string.IsNullOrEmpty(registryService.GetValue(key)))
                {
                    registryService.AddValue(key, path);
                    button.UpdateUI();
                    return;
                }
                registryService.SetValue(key, path);
                button.UpdateUI();
                return;
            }

            var configurationService = ServiceManager.ServiceProvider.GetRequiredService<ConfigurationService>();
            configurationService.AnalyzeConfiguration = button.AnalyzeConfigurationService;
            configurationService.DiagnosticsAnalyzerConfiguration = new DiagnosticsAnalyzerConfigurationServiceLastDevBlog(_fileConfigurationService.Get(button.AnalyzeConfigurationService.ConfigurationName).Value.Item2);
            //configurationService.DiagnosticsAnalyzerConfiguration = _diagnosticsAnalyzerConfigurationService;//
            _pageService.OpenRoslyn();
        }

        private string GetPathOpenFolderDialog()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if(folderDialog.ShowDialog() == false)
                return "";

            return folderDialog.SelectedPath;
        }
    }
}
