using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.Devbox.Core;

namespace Flow.Launcher.Plugin.Devbox.UI;

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
      if(Settings.githubApiToken != value) {
        Settings.githubApiToken = value;
        GithubApi.StartLoadReposTask(Settings);
      }
      OnPropertyChanged();
    }
  }

  public string githubUser
  {
    get => Settings.githubUser;
    set
    {
      if(Settings.githubUser != value) {
        Settings.githubUser = value;
        GithubApi.StartLoadReposTask(Settings);
      }
      OnPropertyChanged();
    }
  }

  public string githubOrganizations
  {
    get {
      return string.Join(", ", Settings.organizations);
    }
    set
    {
      var newOrganizations = (value ?? string.Empty)
        .Split(',')
        .Select(organization => organization.Trim())
        .Where(organization => organization.Length > 0)
        .ToList();
      if (!Settings.organizations.SequenceEqual(newOrganizations)) {
        Settings.organizations = newOrganizations;
        GithubApi.StartLoadReposTask(Settings);
      }
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
