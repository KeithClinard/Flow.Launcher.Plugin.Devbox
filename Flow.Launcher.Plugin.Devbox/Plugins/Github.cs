using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox;

internal static class Github
{
  private static readonly string _ico = "GitHub.png";

  public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
  {
    return Query(query.Search, settings, context);
  }

  public static List<Result> Query(string query, Settings settings, PluginInitContext context)
  {
    var list = new List<Result>();

    if (query.Length == 0)
    {
      list.Add(new Result
      {
        Title = "Open Github Pull Requests",
        SubTitle = "...or keep typing to search for repositories",
        Action = (e) =>
        {
          var isPR = "is%3Apr+";
          var openPRs = "is%3Aopen+";
          var notWIP = "NOT+wip+";
          var notDraft = "draft%3Afalse+";
          var notDependabot = "-author%3Aapp%2Fdependabot+";
          var selectedOrgsOnly = string.Join("+", settings.organizations.Select(org => "+org%3A" + org));

          Helpers.OpenUrl("https://github.com/pulls?q=" + isPR + openPRs + notWIP + notDraft + notDependabot + selectedOrgsOnly);
          return true;
        },
        IcoPath = _ico
      });
      return list;
    }

    var results = GithubApi.QueryGithub(query, settings);

    if (results.total_count > 0)
    {
      var scoreCounter = 100;
      foreach (var result in results.items)
      {
        list.Add(new Result
        {
          Title = result.full_name,
          SubTitle = result.description,
          IcoPath = _ico,
          Score = scoreCounter,
          Action = (e) =>
          {
            Helpers.OpenUrl(result.html_url);
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
}
