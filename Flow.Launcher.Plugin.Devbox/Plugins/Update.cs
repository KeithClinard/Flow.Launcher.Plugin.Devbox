using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class Update
{
  private static readonly string _pluginIcon = "Prompt.png";
  private static readonly string _ico = "GitHub.png";
  private static readonly string _thisRepo = "KeithClinard/Flow.Launcher.Plugin.Devbox";
  private static readonly string _zipAssetName = "Flow.Launcher.Plugin.Devbox.zip";
  private static Version _currentVersion = null;

  public static void LoadCurrentVersion(PluginInitContext context)
  {
    if (_currentVersion != null)
    {
      return;
    }
    try
    {
      using var stream = new StreamReader(context.CurrentPluginMetadata.PluginDirectory + "\\current.version");
      _currentVersion = new Version(stream.ReadToEnd().Trim());
    }
    catch (Exception)
    {
      _currentVersion = new Version("0.0.0");
    }
  }

  public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
  {
    LoadCurrentVersion(context);
    var list = new List<Result>();

    if (string.IsNullOrEmpty(settings.githubApiToken))
    {
      list.Add(new Result
      {
        Title = $"Current Version: {_currentVersion.ToString()}",
        SubTitle = "GitHub API token is required to check for updates",
        IcoPath = _pluginIcon
      });
      return list;
    }

    var releaseResults = GithubApi.QueryRepoForReleases(_thisRepo, settings);
    _ = releaseResults.OrderByDescending(r => r.published_at);
    var assets = GetDownloadableAssets(releaseResults);

    if (assets.Count == 0)
    {
      list.Add(new Result
      {
        Title = "No Results Found",
        IcoPath = _ico
      });
      return list;
    }

    foreach (var asset in assets)
    {
      var result = new Result
      {
        IcoPath = _ico
      };
      if (asset.IsLatestRelease)
      {
        result.SubTitle = $"Latest Release";
      }
      else if (asset.IsPreRelease)
      {
        result.SubTitle = $"Pre-Release";
      }

      if (asset.IsCurrent)
      {
        result.Title = $"Current Version: {asset.Version}";
        result.IcoPath = _pluginIcon;
      }
      else
      {
        result.Title = $"{asset.UpdateAction} to {asset.Version}";
        result.Action = (e) =>
        {
          UpdatePlugin(asset.Url, settings, context);
          return true;
        };

      }
      list.Add(result);
    }

    return list;
  }

  private static List<DownloadableAsset> GetDownloadableAssets(List<ApiResultRelease> releases)
  {
    var assets = new List<DownloadableAsset>();

    var foundLatestRelease = false;
    foreach (var release in releases)
    {
      foreach (var asset in release.assets)
      {
        var isLatestRelease = false;
        if (!foundLatestRelease && !release.prerelease)
        {
          isLatestRelease = true;
          foundLatestRelease = true;
        }
        var updateAction = "Downgrade";
        var releaseVersion = new Version(release.tag_name);
        if (releaseVersion > _currentVersion)
        {
          updateAction = "Upgrade";
        }
        else if (releaseVersion == _currentVersion)
        {
          updateAction = "Current";
        }

        var isCurrentVersion = release.tag_name.Equals(_currentVersion);
        if (asset.name == _zipAssetName)
        {
          assets.Add(new DownloadableAsset
          {
            Version = release.name,
            IsLatestRelease = isLatestRelease,
            IsPreRelease = release.prerelease,
            UpdateAction = updateAction,
            IsCurrent = releaseVersion == _currentVersion,
            Url = asset.url,
          });
        }
      }
    }
    return assets;
  }

  private static void UpdatePlugin(string releaseAssetUrl, Settings settings, PluginInitContext context)
  {
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var pluginsDirectory = $"{appData}\\FlowLauncher\\Plugins";
    var destinationZipFile = $"{pluginsDirectory}\\Flow.Launcher.Plugin.Devbox.zip";
    var destinationPluginDirectory = $"{pluginsDirectory}\\Flow.Launcher.Plugin.Devbox";

    if (!Directory.Exists(pluginsDirectory))
    {
      context.API.ShowMsg("Devbox plugin update failed", "Flow Launcher plugins directory not found", _ico);
      return;
    }

    if (File.Exists(destinationZipFile))
    {
      File.Delete(destinationZipFile);
    }

    GithubApi.DownloadReleaseAsset(releaseAssetUrl, destinationZipFile, settings);

    var shellArguments = $"Stop-Process -Name Flow.Launcher -Force;";
    shellArguments += $"Expand-Archive -Force -LiteralPath '{destinationZipFile}' -DestinationPath {destinationPluginDirectory};";
    shellArguments += $"Start-Process -FilePath \"$env:LOCALAPPDATA\\FlowLauncher\\Flow.Launcher.exe\";";

    var processInfo = new ProcessStartInfo
    {
      LoadUserProfile = true,
      FileName = "powershell.exe",
      Arguments = shellArguments,
      RedirectStandardOutput = false,
      UseShellExecute = true,
      CreateNoWindow = true,
      WindowStyle = ProcessWindowStyle.Hidden
    };

    _ = Process.Start(processInfo);
  }
}

internal class DownloadableAsset
{
  public string Version { get; set; }
  public bool IsLatestRelease { get; set; }
  public bool IsPreRelease { get; set; }
  public string UpdateAction { get; set; }
  public bool IsCurrent { get; set; }
  public string Url { get; set; }
}
