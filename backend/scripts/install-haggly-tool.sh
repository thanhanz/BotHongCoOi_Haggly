#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
backend_dir="$(cd -- "$script_dir/.." && pwd)"
package_dir="$backend_dir/.artifacts/tools"

dotnet pack "$backend_dir/tools/Haggly.DataImport/Haggly.DataImport.csproj" \
  --configuration Release \
  --output "$package_dir"

dotnet tool restore \
  --tool-manifest "$backend_dir/.config/dotnet-tools.json" \
  --add-source "$package_dir" \
  --ignore-failed-sources

echo "Installed. From backend/, run: dotnet haggly --help"
