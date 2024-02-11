using System;
using System.Diagnostics;

namespace Flow.Launcher.Plugin.Devbox.Core
{
  static class Helpers
  {
    public static void OpenUrl(String url)
    {
      _ = Process.Start(
        new ProcessStartInfo(url)
        {
          UseShellExecute = true,
        }
      );
    }
  }
}
