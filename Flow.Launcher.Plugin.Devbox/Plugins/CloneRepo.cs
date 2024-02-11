using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class CloneRepo
{
  private static readonly string _ico = "github.png";

  public static void clone(ApiResultRepo result, bool useWsl, Settings settings)
  {
    var cloneUrl = $"git@github.com:{result.owner.login}/{result.name}.git";
    clone(cloneUrl, result.name, useWsl, settings);
  }

  public static void clone(string cloneUrl, string repoName, bool useWsl, Settings settings)
  {
    var cloneDirectory = settings.gitFolder;
    if (useWsl)
    {
      cloneDirectory = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitFolder}";
    }
    var targetDirectory = $"{cloneDirectory}\\{repoName}";
    if (Directory.Exists(targetDirectory))
    {
      return;
    }
    var command = $"git clone {cloneUrl}";
    if (useWsl)
    {
      command = $"wsl --cd {settings.wslGitFolder} {command}";
    }
    ProcessStartInfo info;
    var arguments = $"/c \"{command}\"";
    info = new ProcessStartInfo
    {
      FileName = "cmd.exe",
      Arguments = arguments,
      UseShellExecute = true,
      WindowStyle = ProcessWindowStyle.Hidden,
      WorkingDirectory = settings.gitFolder
    };

    _ = Process.Start(info);
  }

  public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
  {
    var list = new List<Result>();

    var searchQuery = query.Search;
    var useWsl = true;
    var cloneMessage = "Clone repository into WSL";
    if (searchQuery == "win" || searchQuery.StartsWith("win "))
    {
      cloneMessage = "Clone repository into Windows";
      useWsl = false;
      searchQuery = searchQuery.Remove(0, 3).Trim();
    }

    if (searchQuery.Length == 0)
    {
      list.Add(new Result
      {
        Title = cloneMessage,
        SubTitle = "Keep typing to search for repositories",
        IcoPath = _ico
      });
      return list;
    }

    var folder = settings.gitFolder;
    if (useWsl)
    {
      folder = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitFolder}";
    }

    if (searchQuery.StartsWith("https://") || searchQuery.StartsWith("git@"))
    {
      var lastSlash = searchQuery.LastIndexOf('/');
      var gitExtension = searchQuery.LastIndexOf(".git");
      var repoName = searchQuery.Substring(lastSlash + 1, gitExtension - lastSlash - 1);
      list.Add(new Result
      {
        Title = repoName,
        SubTitle = cloneMessage,
        IcoPath = _ico,
        Action = (e) =>
        {
          clone(searchQuery, repoName, useWsl, settings);
          VSCode.openVSCode(folder + "\\" + repoName, useWsl, settings);
          return true;
        }
      });
      return list;
    }

    var results = GithubApi.QueryGithub(searchQuery, settings);

    if (results.total_count > 0)
    {
      foreach (var result in results.items)
      {
        list.Add(new Result
        {
          Title = result.full_name,
          SubTitle = cloneMessage,
          IcoPath = _ico,
          Action = (e) =>
          {
            clone(result, useWsl, settings);
            VSCode.openVSCode(folder + "\\" + result.name, useWsl, settings);
            return true;
          }
        });
      }
    }
    else
    {
      list.Add(new Result
      {
        Title = "No Results Found",
        IcoPath = _ico
      });
    }

    return list;
  }
}
