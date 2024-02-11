using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Devbox.Core;
using Flow.Launcher.Plugin.Devbox.UI;

namespace Flow.Launcher.Plugin.Devbox
{

  public class Main : IAsyncPlugin, ISettingProvider, IResultUpdated
  {
    private PluginInitContext context;
    private Settings settings;

    const string ico = "Prompt.png";

    private static readonly List<string> requiresRepoCache = new List<string>()
    {
      "gh",
      "cl",
      "clone"
    };

    public event ResultUpdatedEventHandler ResultsUpdated;

    public async Task InitAsync(PluginInitContext context)
    {
      this.context = context;
      settings = context.API.LoadSettingJsonStorage<Settings>();
      GithubApi.StartLoadReposTask(settings);
      await Task.CompletedTask;
    }

    public Control CreateSettingPanel()
    {
      var _viewModel = new SettingsViewModel(context, settings);
      return new SettingsView(_viewModel);
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
    {
      try
      {
        if (requiresRepoCache.Contains(query.ActionKeyword))
        {
          var intermediateResultList = await HandleRepoCacheLoading(query);
          if (intermediateResultList != null && intermediateResultList.Count > 0)
          {
            return intermediateResultList;
          }
        }

        return query.ActionKeyword switch
        {
          "c" => await Task.Run(() => VSCode.Query(query, settings, context)),
          "gh" => await Task.Run(() => Github.Query(query, settings, context)),
          "cl" or "clone" => await Task.Run(() => CloneRepo.Query(query, settings, context)),
          "db" => await Task.Run(() => Update.Query(query, settings, context)),
          _ => await Task.Run(() => new List<Result>(){
            new Result()
            {
              Title = "Unknown action keyword: " + query.ActionKeyword,
              IcoPath = ico
            }
          }),
        };
      }
      catch (Exception e)
      {
        return await Task.Run(() => new List<Result>()
        {
          new Result()
          {
            Title = "Error: " + e.Message,
            IcoPath = ico
          }
        });
      }
    }

    private async Task<List<Result>> HandleRepoCacheLoading(Query query)
    {
      if (String.IsNullOrEmpty(settings.githubApiToken))
      {
        return await Task.Run(() => new List<Result>(){
              new Result()
              {
                Title = "GitHub API token not set - Press enter to open settings",
                SubTitle = "Settings > Plugins > Devbox > GitHub API Token",
                Action = (e) =>
                {
                  context.API.OpenSettingDialog();
                  return true;
                },
                IcoPath = ico
              }
            });
      }
      var hasUserOrOrgs = settings.githubUser != null || settings.organizations.Count > 0;
      if (!hasUserOrOrgs)
      {
        return await Task.Run(() => new List<Result>(){
              new Result()
              {
                Title = "GitHub user or organization not set - Press enter to open settings",
                SubTitle = "Settings > Plugins > Devbox > GitHub User",
                Action = (e) =>
                {
                  context.API.OpenSettingDialog();
                  return true;
                },
                IcoPath = ico
              }
            });
      }
      Task loadReposTask = GithubApi.loadReposTask;
      bool taskFinished = loadReposTask.Status == TaskStatus.RanToCompletion
       || loadReposTask.Status == TaskStatus.Faulted
       || loadReposTask.Status == TaskStatus.Canceled;
      if (!taskFinished)
      {
        ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
        {
          Results = new List<Result>()
              {
                new Result()
                {
                  Title = "Loading GitHub repos...",
                  IcoPath = ico
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
              new Result()
              {
                Title = "GitHub repos failed to load. Restart?",
                SubTitle = $"Error: {loadReposTask.Exception.InnerException.Message}",
                Action = (e) =>
                {
                  GithubApi.StartLoadReposTask(settings);
                  return true;
                },
                IcoPath = ico
              }
            });
      }
      return new List<Result>();
    }
  }
}
