using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Devbox.Core;

internal static class CodeRemoteCache
{
  private static readonly object _lock = new();
  private static List<CodeRemoteConnection> _connections = new();
  private static bool _hasLoaded;

  public static Task LoadTask { get; private set; } = Task.CompletedTask;

  public static IReadOnlyList<CodeRemoteConnection> Connections
  {
    get
    {
      lock (_lock)
      {
        return _connections.ToList();
      }
    }
  }

  public static void StartLoadTask()
  {
    _ = EnsureLoadedTask();
  }

  public static Task EnsureLoadedTask()
  {
    lock (_lock)
    {
      if (!_hasLoaded && LoadTask.IsCompleted)
      {
        LoadTask = Task.Run(Load);
      }

      return LoadTask;
    }
  }

  public static Task RefreshTask()
  {
    lock (_lock)
    {
      if (LoadTask.IsCompleted || LoadTask.IsCanceled || LoadTask.IsFaulted)
      {
        LoadTask = Task.Run(Load);
      }

      return LoadTask;
    }
  }

  public static bool HasMatch(string search)
  {
    lock (_lock)
    {
      return _connections.Any(connection => Helpers.GetResult(
        search,
        $"{connection.Host} {connection.Folder}",
        connection).IsMatch);
    }
  }

  private static void Load()
  {
    var connections = new List<CodeRemoteConnection>();
    var configuredHosts = GetConfiguredHosts();
    foreach (var workspaceFile in GetWorkspaceFiles())
    {
      try
      {
        using var document = JsonDocument.Parse(File.ReadAllText(workspaceFile));
        if (!document.RootElement.TryGetProperty("folder", out var folderElement)
          || !TryParseConnection(folderElement.GetString(), out var connection)
          || !configuredHosts.Contains(connection.Host)
          || connections.Any(i => i.Host == connection.Host && i.Folder == connection.Folder))
        {
          continue;
        }

        connections.Add(connection);
      }
      catch (Exception)
      {
        // A single deleted or malformed workspace record must not hide valid records.
      }
    }

    lock (_lock)
    {
      _connections = connections;
      _hasLoaded = true;
    }
  }

  private static HashSet<string> GetConfiguredHosts()
  {
    var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var configPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".ssh",
      "config");
    ReadConfigFile(configPath, hosts, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    return hosts;
  }

  private static void ReadConfigFile(string path, HashSet<string> hosts, HashSet<string> visitedFiles)
  {
    if (!File.Exists(path) || !visitedFiles.Add(Path.GetFullPath(path)))
    {
      return;
    }

    string[] lines;
    try
    {
      lines = File.ReadAllLines(path);
    }
    catch (Exception)
    {
      return;
    }

    foreach (var line in lines)
    {
      var tokens = line.Split('#')[0]
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
      if (tokens.Length < 2)
      {
        continue;
      }

      if (tokens[0].Equals("Host", StringComparison.OrdinalIgnoreCase))
      {
        foreach (var host in tokens.Skip(1))
        {
          if (!host.Contains('*') && !host.Contains('?') && !host.StartsWith('!'))
          {
            hosts.Add(host);
          }
        }
      }
      else if (tokens[0].Equals("Include", StringComparison.OrdinalIgnoreCase))
      {
        foreach (var include in tokens.Skip(1))
        {
          foreach (var includePath in ExpandConfigPath(include, Path.GetDirectoryName(path)))
          {
            ReadConfigFile(includePath, hosts, visitedFiles);
          }
        }
      }
    }
  }

  private static IEnumerable<string> ExpandConfigPath(string path, string configDirectory)
  {
    path = path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    path = path.Replace("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase);
    if (!Path.IsPathRooted(path))
    {
      path = Path.Combine(configDirectory ?? string.Empty, path);
    }

    var directory = Path.GetDirectoryName(path);
    var filePattern = Path.GetFileName(path);
    if (directory == null || filePattern == null || !Directory.Exists(directory))
    {
      yield break;
    }

    foreach (var file in Directory.GetFiles(directory, filePattern, SearchOption.TopDirectoryOnly))
    {
      yield return file;
    }
  }

  private static IEnumerable<string> GetWorkspaceFiles()
  {
    foreach (var root in GetWorkspaceStorageRoots())
    {
      if (!Directory.Exists(root))
      {
        continue;
      }

      string[] directories;
      try
      {
        directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
      }
      catch (Exception)
      {
        continue;
      }

      foreach (var directory in directories)
      {
        yield return Path.Combine(directory, "workspace.json");
      }
    }
  }

  private static IEnumerable<string> GetWorkspaceStorageRoots()
  {
    var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    return new[]
    {
      Path.Combine(applicationData, "Code", "User", "workspaceStorage"),
      Path.Combine(applicationData, "Code - Insiders", "User", "workspaceStorage")
    };
  }

  private static bool TryParseConnection(string folderUri, out CodeRemoteConnection connection)
  {
    connection = null;
    const string scheme = "vscode-remote://";
    if (string.IsNullOrEmpty(folderUri)
      || !folderUri.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var afterScheme = folderUri[scheme.Length..];
    var slashIndex = afterScheme.IndexOf('/');
    var authority = slashIndex >= 0 ? afterScheme[..slashIndex] : afterScheme;
    var path = slashIndex >= 0 ? afterScheme[slashIndex..] : "/";

    authority = Uri.UnescapeDataString(authority);
    const string hostPrefix = "ssh-remote+";
    if (!authority.StartsWith(hostPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var host = authority[hostPrefix.Length..];
    var folder = Uri.UnescapeDataString(path);
    if (string.IsNullOrEmpty(folder))
    {
      folder = "/";
    }

    connection = new CodeRemoteConnection(host, folder);
    return true;
  }
}

internal sealed class CodeRemoteConnection
{
  public CodeRemoteConnection(string host, string folder)
  {
    Host = host;
    Folder = folder;
  }

  public string Host { get; }
  public string Folder { get; }
}
