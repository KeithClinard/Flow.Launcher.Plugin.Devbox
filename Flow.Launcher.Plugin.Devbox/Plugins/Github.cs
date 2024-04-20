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
          var orgsQueryString = string.Join("+", settings.organizations.Select(org => "+org%3A" + org));
          Helpers.OpenUrl("https://github.com/pulls?q=is%3Aopen+is%3Apr+NOT+wip+draft%3Afalse+" + orgsQueryString);
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
