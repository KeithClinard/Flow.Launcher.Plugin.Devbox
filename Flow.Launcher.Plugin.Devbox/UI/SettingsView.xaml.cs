using System.Windows.Controls;

namespace Flow.Launcher.Plugin.Devbox.UI
{
  public partial class SettingsView : UserControl
  {
    private readonly SettingsViewModel viewModel;
    public SettingsView(SettingsViewModel viewModel)
    {
      DataContext = this.viewModel = viewModel;
      InitializeComponent();
    }
  }
}
