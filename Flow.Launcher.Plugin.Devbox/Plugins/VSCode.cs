using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class VSCode
{
  private static readonly string _ico = "VSCode.png";

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
      command = $"wsl --cd {networkPathToParentFolder} --distribution {settings.wslDistroName} --exec zsh -lc 'code {fileName}'";
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

    var searchString = query.Search;
    var isQueryingWorktrees = false;

    var searchStringTerms = searchString.Split('/');
    if (searchStringTerms.Length > 1)
    {
      isQueryingWorktrees = true;
      searchString = searchString.Replace("/", " ");
    }

    var openableDirectories = GetOpenableDirectories(settings, isQueryingWorktrees);

    var resultsList = openableDirectories
      .Select(i => Helpers.GetResult<OpenableDirectory>(searchString, i.Name, i))
      .Where(i => i.IsMatch)
      .OrderByDescending(i => i.MatchScore)
      .Select(i => i.Value)
      .ToList();

    if (resultsList.Count > 0)
    {
      var scoreCounter = 100;
      foreach (var result in resultsList)
      {
        list.Add(new Result
        {
          Title = result.Name,
          SubTitle = result.Subtitle,
          IcoPath = _ico,
          Score = scoreCounter,
          Action = (e) =>
          {
            openVSCode(result.Path, result.IsWslResult, result.IsGitWorktree, settings);
            return true;
          }
        });
        scoreCounter--;
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

  private static List<OpenableDirectory> GetOpenableDirectories(Settings settings, bool isQueryingWorktrees)
  {
    var openableDirectories = new List<OpenableDirectory>();

    if (!string.IsNullOrEmpty(settings.wslDistroName))
    {
      var wslGitFolder = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitFolder}";
      var wslGitWorktreesFolder = $"\\\\wsl$\\{settings.wslDistroName}{settings.wslGitWorktreesFolder}";
      if (isQueryingWorktrees)
      {
        if (Directory.Exists(wslGitWorktreesFolder))
        {
          var wslWorktrees = Directory.GetDirectories($"{wslGitWorktreesFolder}", "*", SearchOption.TopDirectoryOnly);
          foreach (var worktree in wslWorktrees)
          {
            var repoWorktrees = Directory.GetDirectories(worktree, "*", SearchOption.TopDirectoryOnly);
            foreach (var directory in repoWorktrees)
            {
              openableDirectories.Add(new OpenableDirectory
              {
                Name = getWorktreeDisplayName(directory),
                Path = directory,
                Subtitle = "WSL",
                IsWslResult = true,
                IsGitWorktree = true
              });
            }
          }
        }
      }
      else
      {
        var wslDirectories = Directory.GetDirectories(wslGitFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (var directory in wslDirectories)
        {
          openableDirectories.Add(new OpenableDirectory
          {
            Name = getDisplayName(Path.GetFileName(directory)),
            Path = directory,
            Subtitle = "WSL",
            IsWslResult = true,
            IsGitWorktree = false
          });
        }
        var wslWorkspaces = Directory.GetFiles(wslGitFolder, "*.code-workspace", SearchOption.TopDirectoryOnly);
        foreach (var workspace in wslWorkspaces)
        {
          openableDirectories.Add(new OpenableDirectory
          {
            Name = getDisplayName(Path.GetFileName(workspace)),
            Path = workspace,
            Subtitle = "VSCode Workspace - WSL",
            IsWslResult = true,
            IsGitWorktree = false
          });
        }
      }
    }

    if (Directory.Exists(settings.gitFolder))
    {
      if (isQueryingWorktrees)
      {
        if (Directory.Exists(settings.gitWorktreesFolder))
        {
          var windowsWorktrees = Directory.GetDirectories($"{settings.gitWorktreesFolder}", "*", SearchOption.TopDirectoryOnly);
          foreach (var worktree in windowsWorktrees)
          {
            var repoWorktrees = Directory.GetDirectories(worktree, "*", SearchOption.TopDirectoryOnly);
            foreach (var directory in repoWorktrees)
            {
              openableDirectories.Add(new OpenableDirectory
              {
                Name = getWorktreeDisplayName(directory),
                Path = directory,
                Subtitle = "Windows",
                IsWslResult = false,
                IsGitWorktree = true
              });
            }
          }
        }
      }
      else
      {
        var localDirectories = Directory.GetDirectories(settings.gitFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (var directory in localDirectories)
        {
          openableDirectories.Add(new OpenableDirectory
          {
            Name = getDisplayName(Path.GetFileName(directory)),
            Path = directory,
            Subtitle = "Windows",
            IsWslResult = false,
            IsGitWorktree = false
          });
        }
        var localWorkspaces = Directory.GetFiles(settings.gitFolder, "*.code-workspace", SearchOption.TopDirectoryOnly);
        foreach (var workspace in localWorkspaces)
        {
          openableDirectories.Add(new OpenableDirectory
          {
            Name = getDisplayName(Path.GetFileName(workspace)),
            Path = workspace,
            Subtitle = "VSCode Workspace - Windows",
            IsWslResult = false,
            IsGitWorktree = false
          });
        }
      }
    }
    return openableDirectories;
  }
}

internal class OpenableDirectory
{
  public string Name { get; set; }
  public string Path { get; set; }
  public string Subtitle { get; set; }
  public bool IsWslResult { get; set; }
  public bool IsGitWorktree { get; set; }
}
