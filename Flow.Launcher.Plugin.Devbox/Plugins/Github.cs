using System;
using System.Collections.Generic;
using System.Diagnostics;
using Flow.Launcher.Plugin.Devbox.Core;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox
{
  static class Github
  {
    private static readonly string ico = "GitHub.png";

    public static List<Result> Query(Query query, Settings settings, PluginInitContext context)
    {
      return Query(query.Search, settings, context);
    }

    public static List<Result> Query(String query, Settings settings, PluginInitContext context)
    {
      List<Result> list = new List<Result>();

      if (query.Length == 0)
      {
        list.Add(new Result
        {
          Title = "Open Github",
          SubTitle = "...or keep typing to search for repositories",
          Action = (e) =>
          {
            Helpers.OpenUrl("https://github.com/dashboard");
            return true;
          },
          IcoPath = ico
        });
        return list;
      }

      ApiResult results = GithubApi.QueryGithub(query, settings);

      if (results.total_count > 0)
      {
        foreach (ApiResultRepo result in results.items)
        {
          list.Add(new Result
          {
            Title = result.full_name,
            SubTitle = result.description,
            IcoPath = ico,
            Action = (e) =>
            {
              Helpers.OpenUrl(result.html_url);
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
          IcoPath = ico
        });
      }

      return list;
    }
  }
}
