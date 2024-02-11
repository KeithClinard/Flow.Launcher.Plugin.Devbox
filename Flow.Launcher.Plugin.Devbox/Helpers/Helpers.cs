using System.Diagnostics;

namespace Flow.Launcher.Plugin.Devbox.Core;

internal static class Helpers
{
  public static void OpenUrl(string url)
  {
    _ = Process.Start(
      new ProcessStartInfo(url)
      {
        UseShellExecute = true,
      }
    );
  }
}
