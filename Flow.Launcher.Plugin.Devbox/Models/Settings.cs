using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Devbox.Core
{
  public class Settings
  {
    public string githubApiToken { get; set; } = null;  
    public string githubUser { get; set; } = null;
    public List<string> organizations { get; set; } = new List<string>();
    public string gitFolder { get; set; } = "C:\\git";
    public string gitWorktreesFolder { get; set; } = "C:\\git\\worktrees";
    public string wslGitFolder { get; set; } = "/git";
    public string wslGitWorktreesFolder { get; set; } = "/git/worktrees";
    public string wslDistroName { get; set; } = "Ubuntu-22.04";
  }
}
