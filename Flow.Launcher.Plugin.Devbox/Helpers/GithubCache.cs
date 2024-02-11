using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Flow.Launcher.Plugin.Devbox.Core;

internal static class GithubCache
{
  private static Dictionary<string, ApiResultRepo> _repoCache = new();
  public static bool reposLoaded = false;
  public static ApiResult Query(string query, Settings settings)
  {
    var response = new ApiResult
    {
      items = _repoCache
      .Select(i => Helpers.GetResult<ApiResultRepo>(query, i.Key, i.Value))
      .Where(i => i.IsMatch)
      .OrderBy(i => i.Value.archived)
        .ThenByDescending(i => i.MatchScore)
        .ThenByDescending(i => i.Value.owner.type == "Organization")
        .ThenByDescending(i => i.Value.updated_at)
      .Select(i => i.Value)
      .ToList()
    };

    response.total_count = response.items.Count;
    response.incomplete_results = response.total_count == 0;

    return response;
  }

  public static void AddToLocalCache(ApiResult result)
  {
    foreach (var repo in result.items)
    {
      if (!_repoCache.ContainsKey(repo.full_name))
      {
        _repoCache.Add(repo.full_name, repo);
      }
    }
  }

  
}

