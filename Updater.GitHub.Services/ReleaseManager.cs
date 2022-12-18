using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.XPath;
using Updater.GitHub.Entities;
using Updater.GitHub.Interfaces;
using static Updater.GitHub.Events.ReleaseEvents;

namespace Updater.GitHub.Services
{
    public class ReleaseManager : IReleaseManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;

        private List<Release> _releases = new List<Release>();

        // Start of Constructor region

        #region Constructor

        public ReleaseManager(IEventAggregator eventAggregator, ILogger<ReleaseManager> logger, HttpClientHandler httpClientHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _httpClientHandler = httpClientHandler;
        }

        #endregion

        // Start of Properties region

        #region Properties

        public List<Release> Releases { get => _releases; set => _releases = value; }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Methods region

        #region Methods

        public async void DownloadRelease(string uri)
        {
            await _httpClientHandler.DownloadZip(uri);
        }

        public async void ExtractRelease(string fileName)
        {
            ZipFile.ExtractToDirectory(fileName, "./", true);
            _eventAggregator.GetEvent<ReleaseExtractedEvent>().Publish();
        }

        public async void UpdateAvailableReleases(string repo)
        {
            string uri = $"https://api.github.com/repos/{repo}/releases";
            string json = await _httpClientHandler.GetRequest(uri);
            if (!string.IsNullOrWhiteSpace(json))
            {
                Releases.Clear();
                Releases = JsonSerializer.Deserialize<List<Release>>(json) ?? new List<Release>();
            }
            else
            {
                _logger.LogWarning($"Invalid response. uri: {uri}");
            }
            _eventAggregator.GetEvent<ReleaseInfoUpdatedEvent>().Publish();
        }

        #endregion
    }
}
