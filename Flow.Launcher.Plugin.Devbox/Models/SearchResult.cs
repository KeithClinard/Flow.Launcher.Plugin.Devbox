namespace Flow.Launcher.Plugin.Devbox.Core;

internal class SearchResult<T>
{
  public T Value { get; set; }
  public int MatchScore { get; set; }
  public bool IsMatch { get; set; }
}
