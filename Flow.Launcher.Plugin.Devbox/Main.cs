using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Devbox.Core;
using Flow.Launcher.Plugin.Devbox.UI;

namespace Flow.Launcher.Plugin.Devbox;


public class Main : IAsyncPlugin, ISettingProvider, IResultUpdated
{
  private PluginInitContext _context;
  private Settings _settings;
  private const string _ico = "Prompt.png";

  private static readonly List<string> _requiresRepoCache = new()
  {
    "gh",
    "cl",
    "clone"
  };

  public event ResultUpdatedEventHandler ResultsUpdated;

  public async Task InitAsync(PluginInitContext context)
  {
    this._context = context;
    _settings = context.API.LoadSettingJsonStorage<Settings>();
    GithubApi.StartLoadReposTask(_settings);
    await Task.CompletedTask;
  }

  public Control CreateSettingPanel()
  {
    var _viewModel = new SettingsViewModel(_context, _settings);
    return new SettingsView(_viewModel);
  }

  public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
  {
    try
    {
      if (_requiresRepoCache.Contains(query.ActionKeyword))
      {
        var intermediateResultList = await HandleRepoCacheLoading(query);
        if (intermediateResultList != null && intermediateResultList.Count > 0)
        {
          return intermediateResultList;
        }
      }

      return query.ActionKeyword switch
      {
        "c" => await Task.Run(() => VSCode.Query(query, _settings, _context)),
        "gh" => await Task.Run(() => Github.Query(query, _settings, _context)),
        "cl" or "clone" => await Task.Run(() => CloneRepo.Query(query, _settings, _context)),
        "db" => await Task.Run(() => Update.Query(query, _settings, _context)),
        _ => await Task.Run(() => new List<Result>(){
          new()
          {
            Title = "Unknown action keyword: " + query.ActionKeyword,
            IcoPath = _ico
          }
        }),
      };
    }
    catch (Exception e)
    {
      return await Task.Run(() => new List<Result>()
      {
        new()
        {
          Title = "Error: " + e.Message,
          IcoPath = _ico
        }
      });
    }
  }

  private async Task<List<Result>> HandleRepoCacheLoading(Query query)
  {
    if (string.IsNullOrEmpty(_settings.githubApiToken))
    {
      return await Task.Run(() => new List<Result>(){
            new()
            {
              Title = "GitHub API token not set - Press enter to open settings",
              SubTitle = "Settings > Plugins > Devbox > GitHub API Token",
              Action = (e) =>
              {
                _context.API.OpenSettingDialog();
                return true;
              },
              IcoPath = _ico
            }
          });
    }
    var hasUserOrOrgs = _settings.githubUser != null || _settings.organizations.Count > 0;
    if (!hasUserOrOrgs)
    {
      return await Task.Run(() => new List<Result>(){
            new()
            {
              Title = "GitHub user or organization not set - Press enter to open settings",
              SubTitle = "Settings > Plugins > Devbox > GitHub User",
              Action = (e) =>
              {
                _context.API.OpenSettingDialog();
                return true;
              },
              IcoPath = _ico
            }
          });
    }
    var loadReposTask = GithubApi.loadReposTask;
    var taskFinished = loadReposTask.Status == TaskStatus.RanToCompletion
     || loadReposTask.Status == TaskStatus.Faulted
     || loadReposTask.Status == TaskStatus.Canceled;
    if (!taskFinished)
    {
      ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
      {
        Results = new List<Result>()
            {
              new()
              {
                Title = "Loading GitHub repos...",
                IcoPath = _ico
              }
            },
        Query = query
      });
      try
      {
        await loadReposTask;
      }
      catch (Exception)
      {
        // Do nothing - handled in the next block
      }
    }

    if (GithubApi.reposLoaded == false)
    {
      return await Task.Run(() => new List<Result>(){
            new()
            {
              Title = "GitHub repos failed to load. Restart?",
              SubTitle = $"Error: {loadReposTask.Exception.InnerException.Message}",
              Action = (e) =>
              {
                GithubApi.StartLoadReposTask(_settings);
                return true;
              },
              IcoPath = _ico
            }
          });
    }
    return new List<Result>();
  }
}
