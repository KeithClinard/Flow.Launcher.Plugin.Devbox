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
using Flow.Launcher.Plugin.Devbox.Core;


namespace Flow.Launcher.Plugin.Devbox.Core
{
  static class GithubApi
  {
    private static readonly string githubApiUrl = "https://api.github.com";
    private static readonly HttpClient httpClient = new HttpClient();
    private static Dictionary<String, ApiResultRepo> repoCache = new Dictionary<String, ApiResultRepo>();
    public static bool reposLoaded = false;
    public static Task loadReposTask = null;

    private static ApiResult QueryLocalCache(String query, Settings settings)
    {
      char[] splitChars = query.Replace(" ", "").ToCharArray();
      List<String> splitQuery = new List<string>();
      for (int i = 0; i < splitChars.Length; i++)
      {
        splitQuery.Add(Regex.Escape(splitChars[i].ToString()));
      }
      String regexQueryString = String.Join(".*", splitQuery);
      Regex searchRegex = new Regex(".*" + regexQueryString + ".*", RegexOptions.IgnoreCase);

      ApiResult response = new ApiResult();

      response.items = repoCache
        .Where(i => searchRegex.IsMatch(i.Value.name))
        .Select(i => i.Value)
        .OrderBy(i => i.archived)
          .ThenByDescending(i => i.owner.type == "Organization")
          .ThenByDescending(i => i.updated_at)
        .ToList();

      response.total_count = response.items.Count;
      response.incomplete_results = response.total_count == 0;

      return response;
    }
    
    private static void AddToLocalCache(ApiResult result)
    {
      foreach (ApiResultRepo repo in result.items)
      {
        if (!repoCache.ContainsKey(repo.full_name))
        {
          repoCache.Add(repo.full_name, repo);
        }
      }
    }
    
    public static void StartLoadReposTask(Settings settings)
    {
      loadReposTask = Task.Run(() => GithubApi.LoadReposToCache(settings));
    }

    public static void LoadReposToCache(Settings settings)
    {
      if (String.IsNullOrEmpty(settings.githubApiToken))
      {
        throw new Exception("API Token not set");
      }
      if(!string.IsNullOrEmpty(settings.githubUser))
      {
        LoadUserReposToCache(settings.githubUser, settings);
      }
      foreach (String organization in settings.organizations)
      {
        LoadOrgReposToCache(organization, settings);
      }
      reposLoaded = true;
    }
    
    public static void LoadUserReposToCache(string user, Settings settings)
    {
      bool moreResults = true;
      int page = 1;
      while (moreResults)
      {
        String url = $"{githubApiUrl}/users/{user}/repos?sort=created&direction=asc&per_page=100&page={page}";
        ApiResult result = _QueryGithubRepos(url, settings.githubApiToken);
        AddToLocalCache(result);
        moreResults = result.total_count == 100;
        page++;
      }
    }

    public static void LoadOrgReposToCache(string organization, Settings settings)
    {
      bool moreResults = true;
      int page = 1;
      while (moreResults)
      {
        String url = $"{githubApiUrl}/orgs/{organization}/repos?sort=created&direction=asc&per_page=100&page={page}";
        ApiResult result = _QueryGithubRepos(url, settings.githubApiToken);
        AddToLocalCache(result);
        moreResults = result.total_count == 100;
        page++;
      }
    }

    public static ApiResult QueryGithub(String query, Settings settings)
    {
      ApiResult localResult = QueryLocalCache(query, settings);
      if (!localResult.incomplete_results)
      {
        return localResult;
      }
      String orgQueryPrefix = "+org:";
      String orgQuery = orgQueryPrefix + String.Join(orgQueryPrefix, settings.organizations);
      String url = $"{githubApiUrl}/search/repositories?sort=updated&q={query}{orgQuery}";
      ApiResult result = _QueryGithub(url, settings.githubApiToken);
      AddToLocalCache(result);
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
