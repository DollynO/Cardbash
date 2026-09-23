# Cardbash playtest bridge

The local MCP server lets Codex launch Godot instances, operate menus and gameplay,
inspect replicated state, read logs, and capture screenshots. It requires Python
3.10+, the .NET SDK, and **Godot 4.7 .NET on Linux**. Manual commands need no Python
packages or API key. Laya autoplay requires the optional installation below.
Import the project in Godot once before the first test.

## Install Laya and run a playtest

Laya runs locally in the Python bridge using `laya.load` and typed `choice`
questions. It reads the state exposed from `GameManager` by the C# bridge and
sends normal card-selection and input commands. Model inference runs outside
Godot; the game continues running while a decision is computed.

1. Install [uv](https://docs.astral.sh/uv/getting-started/installation/) if needed.
   These Linux commands also work when system Python has no pip:

   ```sh
   curl -LsSf https://astral.sh/uv/install.sh | sh
   export PATH="$HOME/.local/bin:$PATH"
   ```

2. From the project directory, create an isolated Python 3.12 environment and
   install the pinned [Laya SDK](https://pypi.org/project/laya/0.3.5/):

   ```sh
   cd /home/jhieble/Projects/Cardbash
   uv python install 3.12
   uv venv --python 3.12 .venv-playtest
   uv pip install --python .venv-playtest/bin/python -r tools/playtest/requirements-laya.txt
   ```

3. Download the model and check one real inference before launching a match:

   ```sh
   .venv-playtest/bin/python tools/playtest/run_laya.py --check
   ```

   The first run downloads `convaiinnovations/laya` from Hugging Face and can
   take several minutes. Subsequent runs reuse the cache. No API key is needed
   for this public checkpoint. The SDK selects the device; optionally set
   `CARDBASH_LAYA_DEVICE=cpu` or `CARDBASH_LAYA_DEVICE=cuda` before running.
   `CARDBASH_LAYA_MODEL` can select another compatible checkpoint or local model
   directory. Only one model is kept in memory per bridge process.

4. Launch a two-minute match with Laya controlling both players:

   ```sh
   .venv-playtest/bin/python tools/playtest/run_laya.py --seconds 120
   ```

   Add `--headless` to hide both windows, `--frames 12` to set how long each
   action is held, or `--godot /path/to/Godot_4.7_mono` if discovery fails.
   The launcher creates a host and client, prepares Fireball decks on opposing
   teams, starts the match, and alternates decisions between players. It stops
   both processes when time expires or you press Ctrl+C. Logs remain under
   `.godot/playtest/<instance_id>/laya-decisions.jsonl`.

To use Laya through the existing MCP connection after installation, change only
the `command` for `cardbash_playtest` in `.codex/config.toml` to the absolute
virtual-environment Python path, then restart the Codex session in Rider:

```toml
[mcp_servers.cardbash_playtest]
command = "/home/jhieble/Projects/Cardbash/.venv-playtest/bin/python"
args = ["/home/jhieble/Projects/Cardbash/tools/playtest/server.py"]
tool_timeout_sec = 120
```

Once a match is started, call `playtest_autoplay` with
`{"instance_id":"...","steps":10,"frames":12}`. Each call controls the selected
instance's local player for up to 20 decisions, with a 30-second budget checked
between decisions (model loading and a single inference may exceed that budget).
Call again to continue, alternating instances for two bots. The result includes
choices, probabilities/confidence from Laya, decision time, and final game state.
Manual bridge commands remain available without Laya installed.

The policy lets Laya choose a card from the visible hand, a movement relative to
the nearest living enemy, and one charged ability or no cast. Aim and movement
vectors are calculated by the adapter. It does not observe walls, projectiles,
or the shrinking arena, and does not yet plan paths or aim utility abilities at
allies. The base model is not trained on Cardbash: this is an experimental player
to evaluate and eventually fine-tune, not a guarantee of competent play. Decision
logs record outputs, not a labeled training dataset. Confidence is recorded for
inspection and is not treated as proof of a good gameplay decision.

## Connect Codex

The project's `.codex/config.toml` has a `cardbash_playtest` server entry alongside
Rider. Restart the Codex session in Rider to discover its six tools:

- `playtest_launch`: launch a rendered or headless instance.
- `playtest_instances`: list managed processes and artifact directories.
- `playtest_command`: send a gameplay or inspection command.
- `playtest_logs`: read recent engine output.
- `playtest_stop`: stop one instance or all managed instances.
- `playtest_autoplay`: let local Laya play a bounded batch of decisions.

For another checkout, add this entry with the appropriate absolute path:

```toml
[mcp_servers.cardbash_playtest]
command = "python3"
args = ["/absolute/path/to/Cardbash/tools/playtest/server.py"]
tool_timeout_sec = 120
```

The launcher looks for `GODOT_BIN`, a Godot executable on PATH, or a Godot 4.7
.NET binary beneath `~/Godot`. Set `GODOT_BIN` in the server's `env` configuration
or pass `--godot /path/to/executable` in `args` if needed.

## Example two-player evaluation

Ask Codex: “Use the playtest bridge to start a host and client, give both a
Fireball deck, select their cards, and verify that damage agrees on both peers.”

The underlying sequence is:

1. Launch two instances; retain their returned `instance_id` values.
2. Send `host` to the first and `join` to the second, using the same game `port`.
3. Send `prepare_player` with `team: 0` and `team: 1`, respectively. The default
   temporary deck contains Fireball. Neither instance writes your normal decks.
4. Wait until both players appear ready in the host's `state`.
5. Send `start_match` with `cards_per_round: 1` to the host.
6. Read each instance's `state.hand` and send `choose_card` with a listed `guid`.
7. Use `input`, `state`, `events`, `screenshot`, and `playtest_logs` to evaluate.
8. Use `leave` to return to the menu, or `playtest_stop` to end the processes.

Every command takes `instance_id`, `command`, and an optional `arguments` object.

| Command | Arguments / result |
| --- | --- |
| `state` | Players, peer ID, stage, positions, health, abilities, hand, viewport size |
| `catalog` | Available ability/item GUIDs and display names |
| `tree` | `depth` (default 5, max 12); up to 500 nodes, UI labels and rectangles |
| `events` | Last 200 gameplay event names and timestamps |
| `host`, `join` | `port` (default 18080), `username`; joins localhost only |
| `prepare_player` | `team`, optional `cards` mapping GUIDs to counts; readies the local player |
| `start_match` | Host only; `cards_per_round` (default 1) |
| `choose_card` | `guid` from the currently visible hand |
| `input` | `actions` array, `frames` (1–300), optional `aim: {x,y}`, `aim_space: "world"` or `"viewport"` |
| `click` | `position: {x,y}` in logical viewport coordinates |
| `place_player` | Host-only test setup: `player_id`, world `position: {x,y}` |
| `wait` | `frames` (1–600), then returns state |
| `screenshot` | `max_width` (default 1280); returns a PNG image and artifact path |
| `leave` | Leaves the current match/lobby |

Input actions include `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, and
`Ability1` through `Ability4`. Actions are held for the requested physics frames
and then released. An input with only `aim` moves the pointer without casting.
Screenshot pixels may be scaled; use the logical viewport dimensions from
`state.viewport` when clicking. Headless instances explicitly reject screenshots.
Aim uses a development-only virtual pointer so unfocused/headless clients do not
depend on the desktop mouse. It persists until the next aim command or leaving
the match; world-space aim stays fixed while the camera moves.

State is sampled locally. Wait and compare both peers before asserting network
agreement. Ability cooldown replication is owner-specific, so inspect the host
or the owning client for cooldowns. `card_selection_visible` describes the local
UI, not an authoritative match phase. `place_player` is a setup operation, not a
substitute for testing movement through `input`.

## Lifecycle and isolation

The C# bridge is compiled only under `TOOLS` (Godot's editor Debug configuration),
not ExportDebug or ExportRelease. Normal editor play does not activate it. The
launcher supplies an explicit `--playtest-port` flag and a per-instance random
token; the bridge listens only on loopback. Commands run on Godot's main thread.
There is no arbitrary code execution, property setter, or RPC invocation tool.

Each process receives its own save/config/cache directories under
`.godot/playtest/<instance_id>/`. Logs and screenshots remain there after stopping.
The launcher builds before the first instance; it skips builds while any managed
instance is running. Stop all instances before testing code changes. It manages
at most four processes and stops them on MCP stdin EOF or normal server shutdown.
Force-killing the MCP process may require manually stopping its Godot children.

## Verification

```sh
python3 -m unittest discover -s tools/playtest -v
python3 tools/playtest/smoke_test.py --rendered --godot /path/to/Godot_4.7_mono
dotnet build CardBase.csproj --configuration ExportRelease
```

The integration test checks authentication rejection, two-player lobby setup,
card selection, movement replication, Fireball damage replication, screenshots,
and returning to the menu. Omit `--rendered` for headless logic testing.

Protocol reference: [MCP stdio](https://modelcontextprotocol.io/specification/2025-06-18/basic/transports).
Codex configuration: [MCP servers](https://developers.openai.com/codex/mcp/).
