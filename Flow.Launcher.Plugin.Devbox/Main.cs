using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Devbox.Models;
using Flow.Launcher.Plugin.Devbox.UI;

namespace Flow.Launcher.Plugin.Devbox
{

  public class Main : IAsyncPlugin, ISettingProvider, IResultUpdated
  {
    private PluginInitContext context;
    private Settings settings;

    const string ico = "Prompt.png";

    public event ResultUpdatedEventHandler ResultsUpdated;

    public async Task InitAsync(PluginInitContext context)
    {
      this.context = context;
      settings = context.API.LoadSettingJsonStorage<Settings>();
      // TODO - Preload repos
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
        return query.ActionKeyword switch
        {
          "c" => await Task.Run(() => VSCode.Query(query, settings, context)),
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
  }
}
