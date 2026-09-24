#!/usr/bin/env bash
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

for asset in index.html bridge.js scenes.js crypto.js creator.js creator.css app.js styles.css; do
  if [[ ! -s "$repo_dir/src/wwwroot/$asset" ]]; then
    echo "Missing browser asset: src/wwwroot/$asset" >&2
    exit 1
  fi
done

if command -v node >/dev/null 2>&1; then
  node --check "$repo_dir/src/wwwroot/bridge.js"
  node --check "$repo_dir/src/wwwroot/scenes.js"
  node --check "$repo_dir/src/wwwroot/crypto.js"
  node --check "$repo_dir/src/wwwroot/creator.js"
  node --check "$repo_dir/src/wwwroot/app.js"
fi

echo "Browser assets are ready in src/wwwroot; no compilation is required."
