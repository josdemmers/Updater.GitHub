using Updater.GitHub.Entities;

namespace Updater.GitHub.Interfaces
{
    public interface IReleaseManager
    {
        List<Release> Releases { get; }

        void DownloadRelease(string uri);
        void ExtractRelease(string uri);
        void UpdateAvailableReleases(string repo);
    }
}
