using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Devbox.Core;

internal static class GithubApi
{
  private static readonly string _githubApiUrl = "https://api.github.com";
  private static readonly HttpClient _httpClient = new();
  public static Task loadReposTask = null;  

  public static void StartLoadReposTask(Settings settings)
  {
    loadReposTask = Task.Run(() => LoadReposToCache(settings));
  }

  public static void LoadReposToCache(Settings settings)
  {
    if (string.IsNullOrEmpty(settings.githubApiToken))
    {
      throw new Exception("API Token not set");
    }
    if(!string.IsNullOrEmpty(settings.githubUser))
    {
      LoadUserReposToCache(settings.githubUser, settings);
    }
    foreach (var organization in settings.organizations)
    {
      LoadOrgReposToCache(organization, settings);
    }
    GithubCache.reposLoaded = true;
  }
  
  public static void LoadUserReposToCache(string user, Settings settings)
  {
    var moreResults = true;
    var page = 1;
    while (moreResults)
    {
      var url = $"{_githubApiUrl}/users/{user}/repos?sort=created&direction=asc&per_page=100&page={page}";
      var result = _QueryGithubRepos(url, settings.githubApiToken);
      GithubCache.AddToLocalCache(result);
      moreResults = result.total_count == 100;
      page++;
    }
  }

  public static void LoadOrgReposToCache(string organization, Settings settings)
  {
    var moreResults = true;
    var page = 1;
    while (moreResults)
    {
      var url = $"{_githubApiUrl}/orgs/{organization}/repos?sort=created&direction=asc&per_page=100&page={page}";
      var result = _QueryGithubRepos(url, settings.githubApiToken);
      GithubCache.AddToLocalCache(result);
      moreResults = result.total_count == 100;
      page++;
    }
  }

  public static ApiResult QueryGithub(string query, Settings settings)
  {
    var localResult = GithubCache.Query(query, settings);
    if (!localResult.incomplete_results)
    {
      return localResult;
    }
    var orgQueryPrefix = "+org:";
    var orgQuery = orgQueryPrefix + string.Join(orgQueryPrefix, settings.organizations);
    var url = $"{_githubApiUrl}/search/repositories?sort=updated&q={query}{orgQuery}";
    var result = _QueryGithub(url, settings.githubApiToken);
    GithubCache.AddToLocalCache(result);
    return result;
  }

  public static List<ApiResultRelease> QueryRepoForReleases(string repo, Settings settings)
  {
    var url = $"{_githubApiUrl}/repos/{repo}/releases";
    return _QueryRepoForReleases(url, settings.githubApiToken);
  }

  public static void DownloadReleaseAsset(string assetUrl, string destination, Settings settings)
  {
    _DownloadFileTo(assetUrl, settings.githubApiToken, "application/octet-stream", destination);
  }

  private static ApiResult _QueryGithub(string url, string apiToken)
  {
    var json = _QueryGithubRaw(url, apiToken);
    return JsonSerializer.Deserialize<ApiResult>(json);
  }

  private static ApiResult _QueryGithubRepos(string url, string apiToken)
  {
    var json = _QueryGithubRaw(url, apiToken);
    var repos = JsonSerializer.Deserialize<List<ApiResultRepo>>(json);
    var result = new ApiResult
    {
      total_count = repos.Count,
      items = repos
    };
    return result;
  }

  private static List<ApiResultRelease> _QueryRepoForReleases(string url, string apiToken)
  {
    var json = _QueryGithubRaw(url, apiToken);
    return JsonSerializer.Deserialize<List<ApiResultRelease>>(json);
  }

  private static string _QueryGithubRaw(string url, string apiToken, string accept = null)
  {
    using var stream = _QueryGithubInternal(url, apiToken, accept);
    var objReader = new StreamReader(stream);
    return objReader.ReadToEnd();
  }

  private static void _DownloadFileTo(string url, string apiToken, string accept = null, string destination = null)
  {
    using var stream = _QueryGithubInternal(url, apiToken, accept);
    using var fileStream = new FileStream(destination, FileMode.OpenOrCreate);
    stream.CopyTo(fileStream);
  }

  private static Stream _QueryGithubInternal(string url, string apiToken, string accept = null)
  {
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    _ = request.Headers.UserAgent.TryParseAdd("Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    if (accept != null)
    {
      _ = request.Headers.Accept.TryParseAdd(accept);
    }
    var response = _httpClient.Send(request);
    return response.Content.ReadAsStream();
  }
}
