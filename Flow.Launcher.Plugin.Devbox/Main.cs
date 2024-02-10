using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Devbox
{

  public class Main : IAsyncPlugin, ISettingProvider, IResultUpdated
  {
    private PluginInitContext context;

    const string ico = "Prompt.png";

    public event ResultUpdatedEventHandler ResultsUpdated;


    public async Task InitAsync(PluginInitContext context)
    {
      this.context = context;
      // TODO - Settings
      await Task.CompletedTask;
    }

    public Control CreateSettingPanel()
    {
      return null; // TODO - Create settings panel
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
    {
      try
      {
        return query.ActionKeyword switch
        {
          // TODO - Attach action keywords to plugins
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
