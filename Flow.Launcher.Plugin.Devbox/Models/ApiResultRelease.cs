using System.Collections.Generic;
namespace Flow.Launcher.Plugin.Devbox.Core;

public class ApiResultRelease
{
  public string name { get; set; }
  public string html_url { get; set; }
  public string tag_name { get; set; }
  public string target_commitish { get; set; }
  public bool prerelease { get; set; }
  public string published_at { get; set; }
  public List<ApiResultAsset> assets { get; set; }
}
