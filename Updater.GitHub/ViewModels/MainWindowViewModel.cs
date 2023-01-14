using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Updater.GitHub.Entities;
using Updater.GitHub.Interfaces;
using static Updater.GitHub.Events.HttpClientEvents;
using static Updater.GitHub.Events.ReleaseEvents;

namespace Updater.GitHub.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IReleaseManager _releaseManager;
        private readonly ILogger _logger;

        private ObservableCollection<Release> _releases = new ObservableCollection<Release>();

        private DispatcherTimer _applicationTimer = new();

        private Dictionary<string, string> _arguments = new Dictionary<string, string>();
        private int _downloadProgress;
        private long _downloadProgressBytes;
        private string _notification = "Waiting for GitHub release info.";
        private bool _showNotification = true;
        private string _statusText = string.Empty;
        private string _windowTitle = "Updater.GitHub";        

        // Start of Constructor region

        #region Constructor

        public MainWindowViewModel(IEventAggregator eventAggregator, ILogger<MainWindowViewModel> logger, IReleaseManager releaseManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ReleaseInfoUpdatedEvent>().Subscribe(HandleReleaseInfoUpdatedEvent, ThreadOption.UIThread);
            _eventAggregator.GetEvent<DownloadProgressUpdatedEvent>().Subscribe(HandleDownloadProgressUpdatedEvent, ThreadOption.UIThread);
            _eventAggregator.GetEvent<DownloadCompletedEvent>().Subscribe(HandleDownloadCompletedEvent, ThreadOption.UIThread);
            _eventAggregator.GetEvent<ReleaseExtractedEvent>().Subscribe(HandleReleaseExtractedEvent, ThreadOption.UIThread);

            // Init logger
            _logger = logger;

            // Init services
            _releaseManager= releaseManager;

            // Init View commands
            LaunchUpdGHOnGitHubCommand = new DelegateCommand(LaunchUpdGHOnGitHubExecute);

            // Read command line arguments
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int index = 1; index < args.Length; index += 2)
                {
                    string arg = args[index].Replace("--", "");
                    _arguments.Add(arg, args[index + 1]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Invalid arguments.");
            }            

            Task.Factory.StartNew(() =>
            {
                bool valid = _arguments.TryGetValue("repo", out string? repo);
                valid = _arguments.TryGetValue("app", out string? app) && valid;
                if (valid && !string.IsNullOrWhiteSpace(repo) && !string.IsNullOrWhiteSpace(app)) 
                {
                    WindowTitle = $"Updater.GitHub {CurrentVersion} - {app}";
                    _releaseManager.UpdateAvailableReleases(repo);
                }
                else
                {
                    _logger.LogWarning($"Repo argument missing.");
                }
            });

            // Start timer to check if the to be updated app is closed.
            _applicationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100),
                IsEnabled = true
            };
            _applicationTimer.Tick += ApplicationTimer_Tick;

        }

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand LaunchUpdGHOnGitHubCommand { get; }

        public ObservableCollection<Release> Releases { get => _releases; set => _releases = value; }

        public string CurrentVersion
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? string.Empty;
            }
        }

        public int DownloadProgress 
        {
            get => _downloadProgress; 
            set
            {
                SetProperty(ref _downloadProgress, value, () => { RaisePropertyChanged(nameof(DownloadProgress)); });
            }
        }

        public long DownloadProgressBytes
        { 
            get => _downloadProgressBytes; 
            set
            {
                SetProperty(ref _downloadProgressBytes, value, () => { RaisePropertyChanged(nameof(DownloadProgressBytes)); });
            }
        }

        public string Notification 
        { 
            get => _notification; 
            set
            {
                SetProperty(ref _notification, value, () => { RaisePropertyChanged(nameof(Notification)); });
            }
        }
        public bool ShowNotification
        {
            get => _showNotification;
            set
            {
                SetProperty(ref _showNotification, value, () => { RaisePropertyChanged(nameof(ShowNotification)); });
            }
        }

        public string StatusText 
        {
            get => _statusText; 
            set 
            { 
                SetProperty(ref _statusText, value, () => { RaisePropertyChanged(nameof(StatusText)); });
            }
        }

        public string WindowTitle
        { 
            get => _windowTitle; 
            set
            {
                SetProperty(ref _windowTitle, value, () => { RaisePropertyChanged(nameof(WindowTitle)); });
            }
        }

        #endregion

        // Start of Events region

        #region Events

        private void ApplicationTimer_Tick(object? sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();

            // Check if application is running
            _arguments.TryGetValue("app", out string? app);
            if (!string.IsNullOrWhiteSpace(app))
            {
                app = Path.GetFileNameWithoutExtension(app);
                Process[] proc = Process.GetProcessesByName(app);
                if (proc.Length == 0)
                {
                    // Not running
                    ShowNotification = false;
                    if (Releases.Any())
                    {
                        DownloadRelease();
                    }
                }
                else
                {
                    // Running
                    ShowNotification = true;
                    Notification = $"Close {app} first.";
                    (sender as DispatcherTimer)?.Start();
                }
            }
        }

        private void HandleDownloadProgressUpdatedEvent(HttpProgress httpProgress)
        {
            DownloadProgress = httpProgress.Progress;
            DownloadProgressBytes = httpProgress.Bytes;
            StatusText = $"Downloading: {DownloadProgressBytes} ({DownloadProgress}%)";
        }

        private void HandleReleaseInfoUpdatedEvent()
        {
            Releases.Clear();
            Releases.AddRange(_releaseManager.Releases);

            StatusText = $"Found {Releases.Count} on GitHub";

            if (!ShowNotification)
            {
                DownloadRelease();
            }
        }

        private void HandleDownloadCompletedEvent(string fileName)
        {
            StatusText = $"Finished downloading: {fileName}";
            Task.Factory.StartNew(() =>
            {
                _releaseManager.ExtractRelease(fileName);
            });
            StatusText = $"Extracting: {fileName}";
        }

        private void HandleReleaseExtractedEvent()
        {
            StatusText = $"Extracted";

            _arguments.TryGetValue("app", out string? app);
            if (!string.IsNullOrWhiteSpace(app))
            {
                _logger.LogInformation($"Launching: {app}");
                Process.Start(app);
                _logger.LogInformation($"Shutting down: Updater.GitHub");
                Application.Current.Shutdown();
            }
            else
            {
                StatusText = $"Not able to launch extracted application.";
            }         
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void DownloadRelease()
        {
            // Download latest release
            if (Releases.Count > 0)
            {
                string uri = Releases[0].Assets.FirstOrDefault(a => a.ContentType.Equals("application/x-zip-compressed"))?.BrowserDownloadUrl ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    Task.Factory.StartNew(() =>
                    {
                        _releaseManager.DownloadRelease(uri);
                    });
                }
            }
        }

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
