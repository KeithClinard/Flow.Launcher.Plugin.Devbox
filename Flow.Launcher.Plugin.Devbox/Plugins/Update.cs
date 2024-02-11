using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Flow.Launcher.Plugin.Devbox.Core;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox
{
  static class Update
  {
    private static readonly string pluginIcon = "Prompt.png";
    private static readonly string ico = "GitHub.png";
    private static readonly string thisRepo = "KeithClinard/Flow.Launcher.Plugin.Devbox";
    private static readonly string zipAssetName = "Flow.Launcher.Plugin.Devbox.zip";
    private static Version currentVersion = null;

    public static void LoadCurrentVersion(PluginInitContext context)
    {
      if (currentVersion != null)
      {
        return;
      }
      try
      {
        using (StreamReader stream = new StreamReader(context.CurrentPluginMetadata.PluginDirectory + "\\current.version"))
        {
          currentVersion = new Version(stream.ReadToEnd().Trim());
        }
      }
      catch (Exception)
      {
        currentVersion = new Version("0.0.0");
      }
    }

    public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
    {
      LoadCurrentVersion(context);
      List<Result> list = new List<Result>();

      if (String.IsNullOrEmpty(settings.githubApiToken))
      {
        list.Add(new Result
        {
          Title = $"Current Version: {currentVersion.ToString()}",
          SubTitle = "GitHub API token is required to check for updates",
          IcoPath = pluginIcon
        });
        return list;
      }

      List<ApiResultRelease> releaseResults = GithubApi.QueryRepoForReleases(thisRepo, settings);
      releaseResults.OrderByDescending(r => r.published_at);
      List<DownloadableAsset> assets = GetDownloadableAssets(releaseResults);

      if (assets.Count == 0)
      {
        list.Add(new Result
        {
          Title = "No Results Found",
          IcoPath = ico
        });
        return list;
      }

      foreach (DownloadableAsset asset in assets)
      {
        Result result = new Result
        {
          IcoPath = ico
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
          result.IcoPath = pluginIcon;
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
      List<DownloadableAsset> assets = new List<DownloadableAsset>();

      Boolean foundLatestRelease = false;
      foreach (ApiResultRelease release in releases)
      {
        foreach (ApiResultAsset asset in release.assets)
        {
          bool isLatestRelease = false;
          if (!foundLatestRelease && !release.prerelease)
          {
            isLatestRelease = true;
            foundLatestRelease = true;
          }
          string updateAction = "Downgrade";
          Version releaseVersion = new Version(release.tag_name);
          if (releaseVersion > currentVersion)
          {
            updateAction = "Upgrade";
          }
          else if (releaseVersion == currentVersion)
          {
            updateAction = "Current";
          }

          var isCurrentVersion = release.tag_name.Equals(currentVersion);
          if (asset.name == zipAssetName)
          {
            assets.Add(new DownloadableAsset
            {
              Version = release.name,
              IsLatestRelease = isLatestRelease,
              IsPreRelease = release.prerelease,
              UpdateAction = updateAction,
              IsCurrent = releaseVersion == currentVersion,
              Url = asset.url,
            });
          }
        }
      }
      return assets;
    }

    private static void UpdatePlugin(String releaseAssetUrl, Settings settings, PluginInitContext context)
    {
      String appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      String pluginsDirectory = $"{appData}\\FlowLauncher\\Plugins";
      String destinationZipFile = $"{pluginsDirectory}\\Flow.Launcher.Plugin.Devbox.zip";
      String destinationPluginDirectory = $"{pluginsDirectory}\\Flow.Launcher.Plugin.Devbox";

      if (!Directory.Exists(pluginsDirectory))
      {
        context.API.ShowMsg("Devbox plugin update failed", "Flow Launcher plugins directory not found", ico);
        return;
      }

      if (File.Exists(destinationZipFile))
      {
        File.Delete(destinationZipFile);
      }

      GithubApi.DownloadReleaseAsset(releaseAssetUrl, destinationZipFile, settings);

      String shellArguments = $"Stop-Process -Name Flow.Launcher -Force;";
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

      Process.Start(processInfo);
    }
  }

  class DownloadableAsset
  {
    public string Version { get; set; }
    public bool IsLatestRelease { get; set; }
    public bool IsPreRelease { get; set; }
    public string UpdateAction { get; set; }
    public bool IsCurrent { get; set; }
    public string Url { get; set; }
  }
}
