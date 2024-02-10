#!/usr/bin/env bash

BUILD_DIR=dist
PLUGIN="Flow.Launcher.Plugin.Devbox"

_get_plugin_directory() {
  local appDataRoaming=$(wslpath $(powershell.exe -Command "echo \$env:AppData" | tr -d '\r'))
  echo "$appDataRoaming/FlowLauncher/Plugins/$PLUGIN"
}

_kill_flow_process() {
  if [[ ! -z $(tasklist.exe | grep 'Flow.Launcher') ]]; then
    powershell.exe -Command "Stop-Process -Name Flow.Launcher -Force"
    echo ""
  fi
}

_start_flow_process() {
  if [[ -z $(tasklist.exe | grep 'Flow.Launcher') ]]; then
    powershell.exe -Command "Start-Process -FilePath \"\$env:LOCALAPPDATA\\FlowLauncher\\Flow.Launcher.exe\""
    echo ""
  fi
}

echo "Building Project"
echo ""
dotnet build -c Release
echo ""
echo "Stopping Flow Launcher"
echo ""
_kill_flow_process
echo "Installing Plugin to Flow Launcher"
echo ""
PLUGIN_DIR=$(_get_plugin_directory)
if [[ ! -d $PLUGIN_DIR ]]; then
  mkdir $PLUGIN_DIR
fi
cp -v $BUILD_DIR/* $PLUGIN_DIR/
echo ""
echo "Starting Flow Launcher"
_start_flow_process
echo ""
