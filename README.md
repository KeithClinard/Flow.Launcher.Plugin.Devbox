# Flow.Launcher.Plugin.Devbox

## Usage

Open Flow Launcher and use one of the following keywords:

- `db`: Install a new version of this plugin
- `gh`: Open a repo in GitHub
- `c`: Open a repo in VSCode
- `cl`: Clone a repo from GitHub to WSL
- `cl win`: Clone a repo from GitHub to Windows

## Installation

### Initial Installation
 
Download the plugin from [GitHub](https://github.com/KeithClinard/Flow.Launcher.Plugin.Devbox).
Click Latest Release under 'Releases' > Assets > `Flow.Launcher.Plugin.Devbox.zip`.
 
Create a folder called `Flow.Launcher.Plugin.Devbox` in the `~\AppData\Roaming\FlowLauncher\Plugins\` directory by running the following from PowerShell:
 
```sh
ni -ItemType Directory ~\AppData\Roaming\FlowLauncher\Plugins\Flow.Launcher.Plugin.Devbox
```
 
> **Note**: AppData is a hidden directory located in `C:\Users\<your_username>`.
> If using the File Explorer,ensure that you have it set to where you can see hidden items.  
 
Open Flow by pressing `Alt + space` and run the **Restart Flow Launcher** command.
 
The plugin should now be available in Flow.
If installed correctly, typing `db` into Flow will display the current version of the plugin.

### Configuration

The following settings should be configured in the plugin settings before using this plugin (Settings > Plugins > Devbox):

* Github API Token
  * This Personal Access Token is used to authenticate with the GitHub API. It will need the `repo` scope.
* WSL Distro
  * Defaults to `Ubuntu-22.04` if not set.
* GitHub Username
  * Required to access your personal repositories
* GitHub Organizations
  * Required to access your organization's repositories
  * This should be a comma separated list

### Updating the Plugin

To update the plugin, use the `db` keyword in Flow Launcher to install the latest version.

### Development Workflow

When ready to test changes in Flow Launcher, you can run `make` to build and install
the plugin to Flow Launcher. This will stop the Flow Launcher application before
installing and will restart Flow Launcher after the install is complete.

> Note: If you modify the ActionKeywords in plugin.json, the ID will need to be changed for Flow Launcher to pick up the new keywords
