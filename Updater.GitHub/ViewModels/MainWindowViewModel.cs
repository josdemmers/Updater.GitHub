using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Updater.GitHub.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ILogger _logger;

        private string _windowTitle = "Updater.GitHub";

        // Start of Constructor region

        #region Constructor

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            // Init logger
            _logger = logger;

            // Init View commands
            LaunchUpdGHOnGitHubCommand = new DelegateCommand(LaunchUpdGHOnGitHubExecute);
        }

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand LaunchUpdGHOnGitHubCommand { get; }

        public string WindowTitle { get => _windowTitle; set => _windowTitle = value; }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Methods region

        #region Methods

        private void LaunchUpdGHOnGitHubExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/josdemmers/Updater.GitHub") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
