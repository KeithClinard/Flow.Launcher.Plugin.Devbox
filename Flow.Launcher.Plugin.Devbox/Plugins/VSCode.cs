using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox
{
  static class VSCode
  {
    private static readonly string ico = "VSCode.png";

    private static List<String> subtitles = new List<String>{
      "WSL", "Windows", "VSCode Workspace - WSL", "VSCode Workspace - Windows"
    };

    private static String getDisplayName(String fileName)
    {
      if (!fileName.EndsWith(".code-workspace"))
      {
        return fileName;
      }

      return fileName.Remove(fileName.LastIndexOf('.'));
    }

    private static string getWorktreeDisplayName(string filePath)
    {
      string[] splitPath = filePath.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      string worktreeFolderName = splitPath[splitPath.Length - 2];

      return $"{worktreeFolderName}/{getDisplayName(Path.GetFileName(filePath))}";
    }

    public static void openVSCode(String pathToFile, Boolean useWsl, Settings settings)
    {
      openVSCode(pathToFile, useWsl, false, settings);
    }

    private static void openVSCode(String pathToFile, Boolean useWsl, Boolean isGitWorktree, Settings settings)
    {
      String command = $"code {pathToFile}";
      if (useWsl)
      {
        String wslNetworkPath = $"\\\\wsl$\\{settings.wslDistroName}";
        String backslashedWslGitFolder = settings.wslGitFolder.Replace("/", "\\");
        String fileName = Path.GetFileName(pathToFile);
        String networkPathToParentFolder = $"{wslNetworkPath}{backslashedWslGitFolder}";
        if (isGitWorktree)
        {
          string backslashedWslGitWorktreesFolder = settings.wslGitWorktreesFolder.Replace("/", "\\");
          string[] splitPath = pathToFile.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
          string worktreeFolderName = splitPath[splitPath.Length - 2];
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

      Process.Start(info);
    }

    public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
    {
      List<Result> list = new List<Result>();

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
          IcoPath = ico
        });
        return list;
      }

      var searchString = String.Join("*", query.Search.Replace(" ", "").ToCharArray());
      var worktreeSearchString = "";
      var isQueryingWorktrees = false;

      var searchStringTerms = searchString.Split('/');
      if (searchStringTerms.Length > 1)
      {
        isQueryingWorktrees = true;
        searchString = searchStringTerms[0];
        worktreeSearchString = String.Join("", searchStringTerms.Skip(1));
      }

      string[] wslDirectoryResults = new string[0];
      string[] wslWorkspaceResults = new string[0];
      List<string> wslWorktreeResults = new List<string>();
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

      string[] localDirectoryResults = new string[0];
      string[] localWorkspaceResults = new string[0];
      List<string> localWorktreeResults = new List<string>();
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

      List<string[]> worktreeResultsList = new List<string[]>
      {
        wslWorktreeResults.ToArray(),
        localWorktreeResults.ToArray()
      };

      List<string[]> resultsList = new List<string[]>
      {
        wslDirectoryResults,
        localDirectoryResults,
        wslWorkspaceResults,
        localWorkspaceResults
      };

      if (resultsList.Any(results => results.Length > 0))
      {
        for (int i = 0; i < resultsList.Count; i++)
        {
          string[] results = resultsList[i];
          String subtitle = subtitles[i];
          Boolean isWslResult = subtitle.ToLower().Contains("wsl");
          foreach (string result in results)
          {
            list.Add(new Result
            {
              Title = getDisplayName(Path.GetFileName(result)),
              SubTitle = subtitle,
              IcoPath = ico,
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
        for (int i = 0; i < worktreeResultsList.Count; i++)
        {
          string[] results = worktreeResultsList[i];
          string subtitle = $"Git Worktree - {subtitles[i]}";
          Boolean isWslResult = subtitle.ToLower().Contains("wsl");
          foreach (string result in results)
          {
            list.Add(new Result
            {
              Title = getWorktreeDisplayName(result),
              SubTitle = subtitle,
              IcoPath = ico,
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
          IcoPath = ico
        });
      }

      return list;
    }
  }
}
