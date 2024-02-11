using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Devbox.Core
{
  public class ApiResult
  {
    public int total_count { get; set; }
    public bool incomplete_results { get; set; }
    public List<ApiResultRepo> items { get; set; }
  }
}
