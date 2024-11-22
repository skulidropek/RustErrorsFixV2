using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoslynLibrary.Extensions;
using WpfApp.Core.Services;
using WpfApp.Models;
using WpfApp.View;
using RoslynLibrary.Sections;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WpfApp.Core.Services.Interfaces;
using RoslynLibrary.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace WpfApp.Core
{
    internal class ServiceManager
    {
        public static IServiceProvider ServiceProvider { get; set; }

        static ServiceManager()
        {
            var services = new ServiceCollection();

            services.AddPluginAnalyzer();

            services.AddSingleton<PageService>();
            services.AddSingleton<LangService>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<FileConfigurationService>();
            services.AddSingleton<IAnalyzeConfigurationService, AnalyzeConfigurationServiceLastDevBlog>();
            //services.AddSingleton<IAnalyzeConfigurationService, AnalyzeConfigurationService236DevBlog>();
            services.AddScoped<RegistryService>();
            //services.AddSingleton<IDiagnosticsAnalyzerConfigurationService, DiagnosticsAnalyzerConfigurationServiceLastDevBlog>();
            services.PostConfigure<ManagedSection>(s=>
            {
                //var overrideSection = new ManagedSectionOverride();
                var configurationService = ServiceProvider.GetRequiredService<ConfigurationService>();
                var registry = ServiceProvider.GetRequiredService<RegistryService>();

                //var registryService = ServiceManager.ServiceProvider.GetRequiredService<RegistryService>();

                //if (string.IsNullOrEmpty(registryService.GetValue("RustReferensePath")))
                //{
                //    registryService.AddValue("RustReferensePath", value);
                //    return;
                //}

                //registryService.SetValue("RustReferensePath", value);

                s.Path = registry.GetValue(configurationService.AnalyzeConfiguration.ConfigurationName + "RustReferensePath") ?? "";   //overrideSection.Path;
            });
            
            services.AddTransient<ChoicePluginsUserControl>();
            services.AddTransient<RoslynUserControl>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
