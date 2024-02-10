using Flow.Launcher.Plugin.Devbox.Models;

namespace Flow.Launcher.Plugin.Devbox.UI
{
  public class SettingsViewModel : BaseModel
  {
    public readonly PluginInitContext Context;
    public Settings Settings { get; init; }

    public SettingsViewModel(PluginInitContext context, Settings settings)
    {
      Settings = settings;
      Context = context;
    }

    public string githubApiToken
    {
      get => Settings.githubApiToken;
      set
      {
        Settings.githubApiToken = value;
        // TODO - Preload repos
        OnPropertyChanged();
      }
    }

    public string gitFolder
    {
      get => Settings.gitFolder;
      set
      {
        Settings.gitFolder = value;
        OnPropertyChanged();
      }
    }

    public string gitWorktreesFolder
    {
      get => Settings.gitWorktreesFolder;
      set
      {
        Settings.gitWorktreesFolder = value;
        OnPropertyChanged();
      }
    }

    public string wslGitFolder
    {
      get => Settings.wslGitFolder;
      set
      {
        Settings.wslGitFolder = value;
        OnPropertyChanged();
      }
    }

    public string wslGitWorktreesFolder
    {
      get => Settings.wslGitWorktreesFolder;
      set
      {
        Settings.wslGitWorktreesFolder = value;
        OnPropertyChanged();
      }
    }

    public string wslDistroName
    {
      get => Settings.wslDistroName;
      set
      {
        Settings.wslDistroName = value;
        OnPropertyChanged();
      }
    }
  }
}
