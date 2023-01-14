using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using Updater.GitHub.Interfaces;
using Updater.GitHub.Services;
using Updater.GitHub.Views;

namespace Updater.GitHub
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services
            containerRegistry.RegisterSingleton<IReleaseManager, ReleaseManager>();
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
                loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog-updater.config"));

            // Note: Do not upgrade DryIoc.Microsoft.DependencyInjection above Version="5.1.0" when using Prism.DryIoc Version="8.1.97"
            return new DryIocContainerExtension(new Container(CreateContainerRules()).WithDependencyInjectionAdapter(serviceCollection));
        }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindow>();
            return w;
        }
    }
}
