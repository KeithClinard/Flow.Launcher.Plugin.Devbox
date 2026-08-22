using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class CodeRemote
{
  private static readonly string _ico = "VSCode.png";

  public static List<Result> Query(Query query)
  {
    var results = CodeRemoteCache.Connections
      .Select(connection => Helpers.GetResult<CodeRemoteConnection>(
        query.Search,
        $"{connection.Host} {connection.Folder}",
        connection))
      .Where(result => result.IsMatch)
      .OrderByDescending(result => result.MatchScore)
      .Select(result => result.Value)
      .ToList();

    if (results.Count == 0)
    {
      return new List<Result>
      {
        new()
        {
          Title = "No Remote VS Code Folders Found",
          IcoPath = _ico
        }
      };
    }

    var score = 100;
    return results.Select(connection => new Result
    {
      Title = connection.Host,
      SubTitle = connection.Folder,
      Score = score--,
      IcoPath = _ico,
      Action = _ =>
      {
        Open(connection);
        return true;
      }
    }).ToList();
  }

  private static void Open(CodeRemoteConnection connection)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "code",
      UseShellExecute = true
    };
    startInfo.ArgumentList.Add("--remote");
    startInfo.ArgumentList.Add($"ssh-remote+{connection.Host}");
    startInfo.ArgumentList.Add(connection.Folder);
    _ = Process.Start(startInfo);
  }
}