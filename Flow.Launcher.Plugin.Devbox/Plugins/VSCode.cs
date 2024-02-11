using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class VSCode
{
  private static readonly string _ico = "VSCode.png";

  private static List<string> _subtitles = new()
  {
    "WSL", "Windows", "VSCode Workspace - WSL", "VSCode Workspace - Windows"
  };

  private static string getDisplayName(string fileName)
  {
    if (!fileName.EndsWith(".code-workspace"))
    {
      return fileName;
    }

    return fileName.Remove(fileName.LastIndexOf('.'));
  }

  private static string getWorktreeDisplayName(string filePath)
  {
    var splitPath = filePath.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    var worktreeFolderName = splitPath[splitPath.Length - 2];

    return $"{worktreeFolderName}/{getDisplayName(Path.GetFileName(filePath))}";
  }

  public static void openVSCode(string pathToFile, bool useWsl, Settings settings)
  {
    openVSCode(pathToFile, useWsl, false, settings);
  }

  private static void openVSCode(string pathToFile, bool useWsl, bool isGitWorktree, Settings settings)
  {
    var command = $"code {pathToFile}";
    if (useWsl)
    {
      var wslNetworkPath = $"\\\\wsl$\\{settings.wslDistroName}";
      var backslashedWslGitFolder = settings.wslGitFolder.Replace("/", "\\");
      var fileName = Path.GetFileName(pathToFile);
      var networkPathToParentFolder = $"{wslNetworkPath}{backslashedWslGitFolder}";
      if (isGitWorktree)
      {
        var backslashedWslGitWorktreesFolder = settings.wslGitWorktreesFolder.Replace("/", "\\");
        var splitPath = pathToFile.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var worktreeFolderName = splitPath[splitPath.Length - 2];
        networkPathToParentFolder = $"{wslNetworkPath}{backslashedWslGitWorktreesFolder}\\{worktreeFolderName}";
      }
      command = $"wsl --cd {networkPathToParentFolder} code {fileName}";
    }

    ProcessStartInfo info;
    var arguments = $"/c \"{command}\"";
    info = new ProcessStartInfo
    {
      FileName = "cmd.exe",
      Arguments = arguments,
      UseShellExecute = true,
      WindowStyle = ProcessWindowStyle.Hidden
    };

    _ = Process.Start(info);
  }

  public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
  {
    var list = new List<Result>();

    if (query.Search.Length == 0)
    {
      list.Add(new Result
      {
        Title = "Open VSCode",
        SubTitle = "...or keep typing to search for repositories",
        Action = (e) =>
        {
          openVSCode("", true, settings);
          return true;
        },
        IcoPath = _ico
      });
      return list;
    }

    var searchString = string.Join("*", query.Search.Replace(" ", "").ToCharArray());
    var worktreeSearchString = "";
    var isQueryingWorktrees = false;

    var searchStringTerms = searchString.Split('/');
    if (searchStringTerms.Length > 1)
    {
      isQueryingWorktrees = true;
      searchString = searchStringTerms[0];
      worktreeSearchString = string.Join("", searchStringTerms.Skip(1));
    }

    var wslDirectoryResults = Array.Empty<string>();
    var wslWorkspaceResults = Array.Empty<string>();
    var wslWorktreeResults = new List<string>();
    if (!string.IsNullOrEmpty(settings.wslDistroName))
    {
      var wslGitFolder = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitFolder}";
      var wslGitWorktreesFolder = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitWorktreesFolder}";
      if (isQueryingWorktrees)
      {
        if (Directory.Exists(wslGitWorktreesFolder))
        {
          foreach (var dir in Directory.GetDirectories($"{wslGitWorktreesFolder}", $"*{searchString}*", SearchOption.TopDirectoryOnly))
          {
            wslWorktreeResults.AddRange(Directory.GetDirectories(dir, $"*{worktreeSearchString}*", SearchOption.TopDirectoryOnly));
          }
        }
      }
      else
      {
        wslDirectoryResults = Directory.GetDirectories(wslGitFolder, $"*{searchString}*", SearchOption.TopDirectoryOnly);
        wslWorkspaceResults = Directory.GetFiles(wslGitFolder, $"*{searchString}*.code-workspace", SearchOption.TopDirectoryOnly);
      }
    }

    var localDirectoryResults = Array.Empty<string>();
    var localWorkspaceResults = Array.Empty<string>();
    var localWorktreeResults = new List<string>();
    if (Directory.Exists(settings.gitFolder))
    {
      if (isQueryingWorktrees)
      {
        if (Directory.Exists(settings.gitWorktreesFolder))
        {
          foreach (var dir in Directory.GetDirectories($"{settings.gitWorktreesFolder}", $"*{searchString}*", SearchOption.TopDirectoryOnly))
          {
            localWorktreeResults.AddRange(Directory.GetDirectories(dir, $"*{worktreeSearchString}*", SearchOption.TopDirectoryOnly));
          }
        }
      }
      else
      {
        localDirectoryResults = Directory.GetDirectories(settings.gitFolder, $"*{searchString}*", SearchOption.TopDirectoryOnly);
        localWorkspaceResults = Directory.GetFiles(settings.gitFolder, $"*{searchString}*.code-workspace", SearchOption.TopDirectoryOnly);
      }
    }

    var worktreeResultsList = new List<string[]>
    {
      wslWorktreeResults.ToArray(),
      localWorktreeResults.ToArray()
    };

    var resultsList = new List<string[]>
    {
      wslDirectoryResults,
      localDirectoryResults,
      wslWorkspaceResults,
      localWorkspaceResults
    };

    if (resultsList.Any(results => results.Length > 0))
    {
      for (var i = 0; i < resultsList.Count; i++)
      {
        var results = resultsList[i];
        var subtitle = _subtitles[i];
        var isWslResult = subtitle.ToLower().Contains("wsl");
        foreach (var result in results)
        {
          list.Add(new Result
          {
            Title = getDisplayName(Path.GetFileName(result)),
            SubTitle = subtitle,
            IcoPath = _ico,
            Action = (e) =>
            {
              openVSCode(result, isWslResult, settings);
              return true;
            }
          });
        }
      }
    }
    else if (worktreeResultsList.Any(results => results.Length > 0))
    {
      for (var i = 0; i < worktreeResultsList.Count; i++)
      {
        var results = worktreeResultsList[i];
        var subtitle = $"Git Worktree - {_subtitles[i]}";
        var isWslResult = subtitle.ToLower().Contains("wsl");
        foreach (var result in results)
        {
          list.Add(new Result
          {
            Title = getWorktreeDisplayName(result),
            SubTitle = subtitle,
            IcoPath = _ico,
            Action = (e) =>
            {
              openVSCode(result, isWslResult, true, settings);
              return true;
            }
          });
        }
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
