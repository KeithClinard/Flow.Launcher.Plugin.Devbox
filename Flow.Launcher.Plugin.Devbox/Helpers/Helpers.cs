using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.Devbox.Core;

internal static class Helpers
{
  public static void OpenUrl(string url)
  {
    _ = Process.Start(
      new ProcessStartInfo(url)
      {
        UseShellExecute = true,
      }
    );
  }
  
  public static SearchResult<T> GetResult<T>(string query, string key, T value)
  {
    var splitChars = query.Replace(" ", "").ToCharArray();
    var splitQuery = new List<string>();
    for (var i = 0; i < splitChars.Length; i++)
    {
      splitQuery.Add(Regex.Escape(splitChars[i].ToString()));
    }
    var regexQueryString = string.Join(".*", splitQuery);
    var searchRegex = new Regex(".*" + regexQueryString + ".*", RegexOptions.IgnoreCase);
    var isMatch = searchRegex.IsMatch(key);

    var matchScore = 1;
    var queryWords = query.ToLower().Split(" ");
    foreach (var word in queryWords)
    {
      if (key.ToLower().Contains(word))
      {
        matchScore++;
      }
    }
    var searchResult = new SearchResult<T>
    {
      Value = value,
      MatchScore = matchScore,
      IsMatch = isMatch
    };
    return searchResult;
  }
}
