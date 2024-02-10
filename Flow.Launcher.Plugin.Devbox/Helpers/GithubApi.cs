using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin.Devbox.Models;


namespace Flow.Launcher.Plugin.Devbox.Helpers
{
  static class GithubApi
  {
    private static readonly string githubApiUrl = "https://api.github.com";
    private static readonly HttpClient httpClient = new HttpClient();
    
    private static readonly List<String> organizations = new List<String>{
      "KeithClinard"
    };

    public static ApiResult QueryGithub(String query, Settings settings)
    {
      // TODO: Return from cache
      String orgQueryPrefix = "+org:";
      String orgQuery = orgQueryPrefix + String.Join(orgQueryPrefix, organizations);
      String url = $"{githubApiUrl}/search/repositories?sort=updated&q={query}{orgQuery}";
      ApiResult result = _QueryGithub(url, settings.githubApiToken);
      // TODO: Cache results
      return result;
    }

    public static List<ApiResultRelease> QueryRepoForReleases(String repo, Settings settings)
    {
      String url = $"{githubApiUrl}/repos/{repo}/releases";
      return _QueryRepoForReleases(url, settings.githubApiToken);
    }

    public static void DownloadReleaseAsset(String assetUrl, String destination, Settings settings)
    {
      _DownloadFileTo(assetUrl, settings.githubApiToken, "application/octet-stream", destination);
    }

    private static String _QueryRepoForFile(string url, string apiToken)
    {
      try
      {
        return _QueryGithubRaw(url, apiToken, "application/vnd.github.raw");
      }
      catch (Exception)
      {
        return null;
      }
    }

    private static ApiResult _QueryGithub(string url, string apiToken)
    {
      String json = _QueryGithubRaw(url, apiToken);
      return JsonSerializer.Deserialize<ApiResult>(json);
    }

    private static ApiResult _QueryGithubRepos(string url, string apiToken)
    {
      String json = _QueryGithubRaw(url, apiToken);
      List<ApiResultRepo> repos = JsonSerializer.Deserialize<List<ApiResultRepo>>(json);
      ApiResult result = new ApiResult();
      result.total_count = repos.Count;
      result.items = repos;
      return result;
    }

    private static List<ApiResultRelease> _QueryRepoForReleases(string url, string apiToken)
    {
      String json = _QueryGithubRaw(url, apiToken);
      return JsonSerializer.Deserialize<List<ApiResultRelease>>(json);
    }

    private static string _QueryGithubRaw(string url, string apiToken, string accept = null)
    {
      using (Stream stream = _QueryGithubInternal(url, apiToken, accept))
      {
        StreamReader objReader = new StreamReader(stream);
        return objReader.ReadToEnd();
      }
    }

    private static void _DownloadFileTo(string url, string apiToken, string accept = null, string destination = null)
    {
      using Stream stream = _QueryGithubInternal(url, apiToken, accept);
      using FileStream fileStream = new FileStream(destination, FileMode.OpenOrCreate);
      stream.CopyTo(fileStream);
    }

    private static Stream _QueryGithubInternal(string url, string apiToken, string accept = null)
    {
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.UserAgent.TryParseAdd("Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
      if (accept != null)
      {
        request.Headers.Accept.TryParseAdd(accept);
      }
      HttpResponseMessage response = httpClient.Send(request);
      return response.Content.ReadAsStream();
    }

  }
}
