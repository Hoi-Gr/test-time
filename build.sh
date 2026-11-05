#!/usr/bin/env bash
# Builds dist/TestTime.exe (self-contained, single file, Windows x64).
# Everything (the .NET SDK, NuGet cache, build output) stays inside this project folder.
set -euo pipefail
cd "$(dirname "$0")"
ROOT="$PWD"

export DOTNET_ROOT="$ROOT/.tools/dotnet"
export DOTNET_CLI_HOME="$ROOT/.tools/dotnet-home"
export NUGET_PACKAGES="$ROOT/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [ ! -x "$DOTNET_ROOT/dotnet" ]; then
  echo "Installing .NET SDK into .tools/dotnet ..."
  mkdir -p "$ROOT/.tools"
  curl -sSL https://dot.net/v1/dotnet-install.sh -o "$ROOT/.tools/dotnet-install.sh"
  bash "$ROOT/.tools/dotnet-install.sh" --channel 10.0 --install-dir "$DOTNET_ROOT"
fi

"$DOTNET_ROOT/dotnet" publish "$ROOT/src/TestTime.csproj" -c Release -o "$ROOT/dist" \
  -p:BaseIntermediateOutputPath="$ROOT/.build/obj/" -p:BaseOutputPath="$ROOT/.build/bin/"

echo
echo "Built: $ROOT/dist/TestTime.exe"
