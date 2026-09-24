#!/usr/bin/env bash
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
project="$repo_dir/src/OregonTrailDotNet.csproj"
web_app="$repo_dir/src/wwwroot/app.js"
verify_port=${WEB_VERIFY_PORT:-18763}

case "$verify_port" in
  ''|*[!0-9]*)
    echo "WEB_VERIFY_PORT must be a numeric loopback port." >&2
    exit 2
    ;;
esac

if ((verify_port < 1 || verify_port > 65535)); then
  echo "WEB_VERIFY_PORT must be between 1 and 65535." >&2
  exit 2
fi

for command in dotnet curl grep sed; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Required command is unavailable: $command" >&2
    exit 2
  fi
done

verify_tmp=$(mktemp -d "${TMPDIR:-/tmp}/oregon-web-verify.XXXXXX")
host_pid=''

cleanup() {
  if [[ -n "$host_pid" ]] && kill -0 "$host_pid" 2>/dev/null; then
    kill "$host_pid" 2>/dev/null || true
    wait "$host_pid" 2>/dev/null || true
  fi
  rm -rf -- "$verify_tmp"
}
trap cleanup EXIT INT TERM

task_dotnet_home=${DOTNET_CLI_HOME:-"$verify_tmp/dotnet-home"}
# Keep restored packages outside the disposable CLI home. project.assets.json points at these
# paths after verification; deleting them would break the next build or publish --no-restore.
export NUGET_PACKAGES=${NUGET_PACKAGES:-"$HOME/.nuget/packages"}
mkdir -p "$task_dotnet_home"
export DOTNET_CLI_HOME="$task_dotnet_home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false

source_candidates=(
  "$repo_dir/src/Web"
  "$repo_dir/src/Program.cs"
  "$repo_dir/src/wwwroot/bridge.js"
  "$repo_dir/src/wwwroot/app.js"
  "$repo_dir/src/wwwroot/crypto.js"
  "$repo_dir/src/wwwroot/creator.js"
  "$repo_dir/src/wwwroot/creator.css"
  "$repo_dir/src/wwwroot/index.html"
  "$repo_dir/src/wwwroot/styles.css"
)

production_sources=()
for source_candidate in "${source_candidates[@]}"; do
  if [[ -e "$source_candidate" ]]; then
    production_sources+=("$source_candidate")
  fi
done

static_failures=0

reject_pattern() {
  local description=$1
  local pattern=$2
  local matches

  if matches=$(grep -R -n -E -- "$pattern" "${production_sources[@]}" 2>/dev/null); then
    echo "Legacy web compatibility found: $description" >&2
    echo "$matches" >&2
    static_failures=1
  fi
}

reject_pattern "terminal-frame cleanup" 'CleanScreen'
reject_pattern "terminal dirty-frame subscription" 'ScreenBufferDirtyEvent'
reject_pattern "legacy state/option/request DTO" '(^|[^[:alnum:]_])(GameStateDto|GameOptionDto|InputRequest)([^[:alnum:]_]|$)'
reject_pattern "raw screen or command string in a web DTO" 'string[[:space:]]+(Screen|Command)[[:space:]]*[,;)]'
reject_pattern "legacy character-input endpoint" '/api/game/input'
reject_pattern "legacy directional-control endpoint" '/api/game/control'
reject_pattern "preformatted terminal node" 'Vdom\.Node\.pre\b'
# Decorative ASCII scenes are intentional. Gameplay still uses semantic elements and advertised actions;
# never reintroduce rendered terminal frames as the browser's data or layout contract.
reject_pattern "horizontal gameplay overflow in production CSS" 'overflow-x[[:space:]]*:'

if ((static_failures != 0)); then
  exit 1
fi

if [[ ! -s "$web_app" ]]; then
  echo "Missing browser application: $web_app" >&2
  exit 1
fi

echo "Building the .NET web host..."
dotnet build "$project" --configuration Release --nologo ${WEB_VERIFY_NO_RESTORE:+--no-restore}

base_url="http://127.0.0.1:$verify_port"
host_log="$verify_tmp/host.log"
cookie_jar="$verify_tmp/cookies.txt"
other_cookie_jar="$verify_tmp/other-cookies.txt"

echo "Starting the web host on $base_url..."
dotnet run \
  --project "$project" \
  --configuration Release \
  --no-build \
  --urls "$base_url" >"$host_log" 2>&1 &
host_pid=$!

ready=0
for ((attempt = 0; attempt < 120; attempt++)); do
  if curl --silent --fail --max-time 5 --cookie "$cookie_jar" --cookie-jar "$cookie_jar" \
    "$base_url/api/game" >/dev/null 2>&1; then
    ready=1
    break
  fi

  if ! kill -0 "$host_pid" 2>/dev/null; then
    break
  fi

  sleep 0.25
done

if ((ready == 0)); then
  echo "The web host did not become ready." >&2
  sed -n '1,240p' "$host_log" >&2
  exit 1
fi

assert_contains() {
  local content=$1
  local expected=$2
  local description=$3

  if ! grep -F -q -- "$expected" <<<"$content"; then
    echo "Missing $description ($expected)." >&2
    exit 1
  fi
}

assert_snapshot_shape() {
  local json=$1
  local context=$2

  assert_contains "$json" '"revision":' "$context revision"
  assert_contains "$json" '"running":' "$context running flag"
  assert_contains "$json" '"hud":{' "$context HUD object"
  assert_contains "$json" '"party":[' "$context party array"
  assert_contains "$json" '"inventory":[' "$context inventory array"
  assert_contains "$json" '"progress":{' "$context progress object"
  assert_contains "$json" '"store":' "$context nullable store payload"
  assert_contains "$json" '"score":' "$context nullable score payload"
  assert_contains "$json" '"stops":[' "$context route stops"
  assert_contains "$json" '"screen":{' "$context semantic screen object"
  assert_contains "$json" '"actions":[' "$context semantic actions"

  if ! grep -E -q -- '"screen":\{"kind":"(setup|travel|dialog|choice|store|status|river|activity|crypto|creator|event|game-over)"' <<<"$json"; then
    echo "$context has an unknown or missing screen.kind." >&2
    exit 1
  fi

  if grep -E -q -- '"screen":"' <<<"$json"; then
    echo "$context contains a terminal-style screen string." >&2
    exit 1
  fi
}

echo "Checking semantic game state..."
state_json=$(curl --silent --show-error --fail --cookie "$cookie_jar" --cookie-jar "$cookie_jar" \
  "$base_url/api/game")
assert_snapshot_shape "$state_json" 'GET /api/game'

# A different browser cookie must own a different simulation. Capture its initial state now and verify below that mutating
# the first browser does not advance or otherwise alter it.
other_state_before=$(curl --silent --show-error --fail --cookie "$other_cookie_jar" \
  --cookie-jar "$other_cookie_jar" "$base_url/api/game")
assert_snapshot_shape "$other_state_before" 'second browser GET /api/game'

revision=$(sed -n 's/.*"revision":\([0-9][0-9]*\).*/\1/p' <<<"$state_json")
journey_id=$(sed -n 's/.*"journeyId":"\([^"]*\)".*/\1/p' <<<"$state_json")
action_id=$(sed -n 's/.*"actions":\[{"actionId":"\([^"]*\)".*/\1/p' <<<"$state_json")

if [[ -z "$revision" || -z "$action_id" ]]; then
  echo "Initial state must advertise at least one semantic action." >&2
  exit 1
fi

if [[ ! "$action_id" =~ ^[A-Za-z0-9._:-]+$ ]]; then
  echo "Action ID contains characters the dependency-free verifier cannot safely encode: $action_id" >&2
  exit 1
fi

printf -v action_body \
  '{"actionId":"%s","expectedRevision":%s,"expectedJourneyId":"%s","text":null,"value":null}' \
  "$action_id" \
  "$revision" \
  "$journey_id"

action_response_file="$verify_tmp/action-response.json"
action_status=$(curl \
  --silent \
  --show-error \
  --output "$action_response_file" \
  --write-out '%{http_code}' \
  --cookie "$cookie_jar" \
  --cookie-jar "$cookie_jar" \
  --header 'Content-Type: application/json' \
  --data "$action_body" \
  "$base_url/api/game/actions")
action_json=$(<"$action_response_file")

if [[ "$action_status" != '200' ]]; then
  echo "Semantic action returned HTTP $action_status instead of 200." >&2
  echo "$action_json" >&2
  exit 1
fi

assert_contains "$action_json" '"accepted":true' 'accepted action response'
assert_contains "$action_json" '"errorCode":' 'action errorCode field'
assert_contains "$action_json" '"message":' 'action message field'
assert_contains "$action_json" '"state":{' 'action state field'
assert_snapshot_shape "$action_json" 'POST /api/game/actions response state'

other_state_after=$(curl --silent --show-error --fail --cookie "$other_cookie_jar" \
  --cookie-jar "$other_cookie_jar" "$base_url/api/game")
if [[ "$other_state_after" != "$other_state_before" ]]; then
  echo "Mutating one browser changed another browser's game state." >&2
  exit 1
fi

next_revision=$(sed -n 's/.*"state":{"revision":\([0-9][0-9]*\).*/\1/p' <<<"$action_json")
if [[ -z "$next_revision" ]] || ((next_revision <= revision)); then
  echo "Accepted action did not advance the semantic revision." >&2
  exit 1
fi

stale_response_file="$verify_tmp/stale-response.json"
stale_status=$(curl \
  --silent \
  --show-error \
  --output "$stale_response_file" \
  --write-out '%{http_code}' \
  --cookie "$cookie_jar" \
  --cookie-jar "$cookie_jar" \
  --header 'Content-Type: application/json' \
  --data "$action_body" \
  "$base_url/api/game/actions")
stale_json=$(<"$stale_response_file")

if [[ "$stale_status" != '409' ]]; then
  echo "Replaying an old expectedRevision returned HTTP $stale_status instead of 409." >&2
  echo "$stale_json" >&2
  exit 1
fi

assert_contains "$stale_json" '"accepted":false' 'rejected stale action'
assert_contains "$stale_json" '"errorCode":"stale-revision"' 'stale revision error code'
assert_contains "$stale_json" '"state":{' 'fresh state in stale response'

echo "Checking browser shell and assets..."
index_html=$(curl --silent --show-error --fail "$base_url/")
assert_contains "$index_html" 'id="app"' 'application mount point'
assert_contains "$index_html" 'styles.css' 'stylesheet reference'
assert_contains "$index_html" 'app.js' 'browser application reference'
assert_contains "$index_html" 'creator.js' 'travel-channel application reference'
assert_contains "$index_html" 'creator.css' 'travel-channel stylesheet reference'

css_file="$verify_tmp/styles.css"
js_file="$verify_tmp/app.js"
scenes_file="$verify_tmp/scenes.js"
curl --silent --show-error --fail --output "$css_file" "$base_url/styles.css"
curl --silent --show-error --fail --output "$js_file" "$base_url/app.js"
curl --silent --show-error --fail --output "$scenes_file" "$base_url/scenes.js"
curl --silent --show-error --fail --output "$verify_tmp/creator.js" "$base_url/creator.js"
curl --silent --show-error --fail --output "$verify_tmp/creator.css" "$base_url/creator.css"

if [[ ! -s "$css_file" || ! -s "$js_file" || ! -s "$scenes_file" || ! -s "$verify_tmp/creator.js" || ! -s "$verify_tmp/creator.css" ]]; then
  echo "The root page did not serve non-empty CSS and browser assets." >&2
  exit 1
fi

echo "Web rewrite verification passed."
