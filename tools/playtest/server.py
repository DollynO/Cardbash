#!/usr/bin/env python3
"""Local MCP stdio server for Cardbash playtests, with an optional Laya player."""
from __future__ import annotations

import argparse
import base64
from dataclasses import dataclass
import json
import os
from pathlib import Path
import secrets
import shutil
import signal
import socket
import subprocess
import sys
import time
import uuid

PROJECT = Path(__file__).resolve().parents[2]
PROTOCOL_VERSIONS = ("2024-11-05", "2025-03-26", "2025-06-18")
COMMANDS = ("ping", "state", "catalog", "tree", "events", "wait", "host", "join",
            "prepare_player", "start_match", "choose_card", "input", "click",
            "place_player", "screenshot", "leave")


def tool(name, description, properties, required=(), read_only=False):
    return {"name": name, "description": description,
            "inputSchema": {"type": "object", "properties": properties,
                            "required": list(required), "additionalProperties": False},
            "annotations": {"readOnlyHint": read_only, "destructiveHint": False, "openWorldHint": False}}


INSTANCE = {"type": "string", "description": "Instance ID returned by playtest_launch."}
TOOLS = [
    tool("playtest_launch", "Launch an isolated Godot 4.7 .NET development instance. Builds before the first launch. "
         "Each instance has separate saves, logs and shader cache. Rendered instances support screenshots. "
         "Use playtest_command host/join after launching. Processes stop when this MCP server exits.", {
             "headless": {"type": "boolean", "default": False},
             "build": {"type": "boolean", "default": True},
         }),
    tool("playtest_instances", "List managed instances and their log/artifact directories.", {}, read_only=True),
    tool("playtest_command", "Control a managed instance. Commands and arguments: "
         "state/ping/catalog/events/leave: {}; tree: {depth:0..12}; wait: {frames:1..600}; "
         "host/join: {port:18080,username:string} (join connects to localhost); "
         "prepare_player: {team:int,cards:{GUID:count}} (default Fireball deck, readies player); "
         "start_match: {cards_per_round:1..20}, host only, all players must be ready; "
         "choose_card: {guid:string} from state.hand; "
         "input: {actions:[MoveLeft|MoveRight|MoveUp|MoveDown|Ability1..4|other InputMap actions],frames:1..300,"
         "aim:{x,y},aim_space:viewport|world}; holds then releases actions, aim is optional; "
         "click: {position:{x,y}} in logical viewport pixels; "
         "place_player: {player_id:int,position:{x,y}}, host-only test setup; "
         "screenshot: {max_width:320..1920}, returns PNG image plus path (rendered instances only). "
         "Poll state on both peers to verify replication; remote cooldowns may be owner-only. "
         "Mutations return local state, not proof that all peers have caught up. Inputs/waits use physics frames.", {
             "instance_id": INSTANCE,
             "command": {"type": "string", "enum": list(COMMANDS)},
             "arguments": {"type": "object", "default": {}},
         }, ("instance_id", "command")),
    tool("playtest_logs", "Read the tail of an instance's engine output, including C# errors and warnings.", {
        "instance_id": INSTANCE, "lines": {"type": "integer", "minimum": 1, "maximum": 1000, "default": 100},
    }, ("instance_id",), read_only=True),
    tool("playtest_stop", "Stop one managed instance, or all managed instances if instance_id is omitted. "
         "Logs, screenshots and isolated saves remain available.", {"instance_id": INSTANCE}),
    tool("playtest_autoplay", "Use local Laya to select cards, move, aim and cast for one player in a started match. "
         "Requires the optional Laya Python package and downloaded model. Returns decisions and final state. "
         "Each call performs a bounded batch; call again to continue. Decisions are logged in the instance directory.", {
             "instance_id": INSTANCE,
             "steps": {"type": "integer", "minimum": 1, "maximum": 20, "default": 1},
             "frames": {"type": "integer", "minimum": 1, "maximum": 60, "default": 12},
         }, ("instance_id",)),
]


@dataclass
class Instance:
    id: str
    process: subprocess.Popen
    port: int
    token: str
    directory: Path
    headless: bool

    def info(self):
        return {"instance_id": self.id, "pid": self.process.pid, "port": self.port,
                "running": self.process.poll() is None, "exit_code": self.process.poll(),
                "headless": self.headless, "directory": str(self.directory),
                "log": str(self.directory / "output.log")}


class PlaytestManager:
    def __init__(self, project=PROJECT, godot=None):
        self.project = Path(project).resolve()
        self.godot = godot or os.environ.get("GODOT_BIN")
        self.instances: dict[str, Instance] = {}
        self.laya_policy = None

    def engine(self):
        if self.godot:
            candidate = shutil.which(self.godot) or self.godot
        else:
            candidate = next((shutil.which(n) for n in ("godot-mono", "godot4-mono", "godot", "godot4")
                              if shutil.which(n)), None)
            if candidate is None:
                candidate = next((str(p) for p in sorted((Path.home() / "Godot").glob("**/Godot_v4.7*mono*"))
                                  if p.is_file() and os.access(p, os.X_OK)), None)
        if not candidate:
            raise ValueError("Godot 4.7 .NET was not found. Set GODOT_BIN to its executable path.")
        version = subprocess.run([candidate, "--version"], capture_output=True, text=True, timeout=10, check=True).stdout.strip()
        if not version.startswith("4.7.") or ".mono." not in version:
            raise ValueError(f"This bridge requires Godot 4.7 .NET; found {version}.")
        self.godot = candidate
        return candidate

    def launch(self, headless=False, build=True):
        if not isinstance(headless, bool) or not isinstance(build, bool):
            raise ValueError("headless and build must be booleans.")
        if not sys.platform.startswith("linux"):
            raise ValueError("Save-directory isolation currently supports Linux only.")
        running = [i for i in self.instances.values() if i.process.poll() is None]
        if len(running) >= 4:
            raise ValueError("Stop an instance before launching more than four.")
        engine = self.engine()
        if build and not running:
            result = subprocess.run(["dotnet", "build", "CardBase.csproj", "--configuration", "Debug", "--verbosity", "minimal"],
                                    cwd=self.project, capture_output=True, text=True, timeout=90)
            if result.returncode:
                raise RuntimeError("Build failed:\n" + (result.stdout + result.stderr)[-16000:])
        elif not (self.project / ".godot/mono/temp/bin/Debug/CardBase.dll").is_file():
            raise ValueError("Build the Debug project before launching with build=false.")
        identifier = uuid.uuid4().hex[:12]
        directory = self.project / ".godot" / "playtest" / identifier
        directory.mkdir(parents=True, mode=0o700)
        with socket.socket() as reservation:
            reservation.bind(("127.0.0.1", 0))
            port = reservation.getsockname()[1]
        token = secrets.token_hex(32)
        env = os.environ.copy()
        env.update(CARDBASH_PLAYTEST_TOKEN=token, CARDBASH_PLAYTEST_ARTIFACTS=str(directory / "screenshots"),
                   XDG_DATA_HOME=str(directory / "data"), XDG_CONFIG_HOME=str(directory / "config"),
                   XDG_CACHE_HOME=str(directory / "cache"))
        args = [engine, "--path", str(self.project), "--audio-driver", "Dummy", "--max-fps", "60",
                "--log-file", str(directory / "engine.log")]
        if headless:
            args += ["--headless"]
        else:
            args += ["--resolution", "960x540", "--position", f"{80 + len(running) * 100},{80 + len(running) * 80}"]
        args += ["--", f"--playtest-port={port}"]
        with (directory / "output.log").open("w") as output:
            process = subprocess.Popen(args, cwd=self.project, env=env, stdout=output, stderr=subprocess.STDOUT)
        instance = Instance(identifier, process, port, token, directory, headless)
        self.instances[identifier] = instance
        deadline = time.monotonic() + 40
        while time.monotonic() < deadline and process.poll() is None:
            try:
                self.request(identifier, "ping", timeout=0.5)
                return instance.info()
            except (OSError, ValueError, RuntimeError):
                time.sleep(0.1)
        self.stop(identifier)
        raise RuntimeError("Bridge did not become ready. Confirm a Debug build and the SceneManager attachment.\n"
                           + self.logs(identifier)["text"])

    def get(self, identifier):
        if identifier not in self.instances:
            raise ValueError("Unknown instance ID. Use playtest_instances.")
        return self.instances[identifier]

    def request(self, identifier, command, arguments=None, timeout=15):
        instance = self.get(identifier)
        if instance.process.poll() is not None:
            raise ValueError(f"Instance exited ({instance.process.returncode}); inspect playtest_logs.")
        if command not in COMMANDS:
            raise ValueError(f"Unknown command: {command}")
        if arguments is not None and not isinstance(arguments, dict):
            raise ValueError("arguments must be an object.")
        message = json.dumps({"token": instance.token, "command": command, "arguments": arguments or {}}, allow_nan=False)
        encoded = (message + "\n").encode()
        if len(encoded) > 65536:
            raise ValueError("Command exceeds 64 KiB.")
        with socket.create_connection(("127.0.0.1", instance.port), timeout=timeout) as peer:
            peer.sendall(encoded)
            with peer.makefile("rb") as stream:
                reply = stream.readline(2 * 1024 * 1024)
        if not reply.endswith(b"\n"):
            raise RuntimeError("Bridge response was incomplete or too large.")
        response = json.loads(reply)
        if not response.get("ok"):
            raise RuntimeError(response.get("error", "Playtest command failed."))
        return response["result"]

    def logs(self, instance_id, lines=100):
        if type(lines) is not int or not 1 <= lines <= 1000:
            raise ValueError("lines must be between 1 and 1000.")
        path = self.get(instance_id).directory / "output.log"
        with path.open("rb") as stream:
            stream.seek(max(0, path.stat().st_size - 256 * 1024))
            text = stream.read().decode("utf-8", errors="replace")
        return {"path": str(path), "text": "\n".join(text.splitlines()[-lines:])}

    def stop(self, instance_id=None):
        instances = [self.get(instance_id)] if instance_id else list(self.instances.values())
        for instance in instances:
            if instance.process.poll() is None:
                instance.process.terminate()
                try:
                    instance.process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    instance.process.kill()
                    instance.process.wait(timeout=3)
        return {"stopped": [i.id for i in instances]}

    def autoplay(self, instance_id, steps=1, frames=12):
        if type(steps) is not int or not 1 <= steps <= 20:
            raise ValueError("steps must be between 1 and 20.")
        if type(frames) is not int or not 1 <= frames <= 60:
            raise ValueError("frames must be between 1 and 60.")
        instance = self.get(instance_id)
        state = self.request(instance_id, "state")
        if state["stage"] != "game":
            raise ValueError("Start a match before asking Laya to play.")
        if self.laya_policy is None:
            from laya_policy import LayaPolicy
            self.laya_policy = LayaPolicy()
        self.laya_policy.load()
        # Loading a cold model can take seconds; sample again before deciding.
        deadline = time.monotonic() + 30
        decisions = []
        path = instance.directory / "laya-decisions.jsonl"
        for _ in range(steps):
            state = self.request(instance_id, "state")
            if state["stage"] != "game":
                break
            started = time.monotonic()
            decision = self.laya_policy.decide(state, frames)
            decision["inference_ms"] = round((time.monotonic() - started) * 1000, 1)
            record = {"physics_frame": state["physics_frame"], **decision}
            state = self.request(instance_id, decision["command"], decision["arguments"])
            with path.open("a") as log:
                log.write(json.dumps(record) + "\n")
            decisions.append(record)
            if time.monotonic() >= deadline:
                break
        return {"decisions": decisions, "state": state, "log": str(path)}

    def call(self, name, arguments):
        images = []
        if name == "playtest_launch":
            result = self.launch(**arguments)
        elif name == "playtest_instances":
            if arguments:
                raise ValueError("playtest_instances takes no arguments.")
            result = [i.info() for i in self.instances.values()]
        elif name == "playtest_command":
            instance_id = arguments["instance_id"]
            result = self.request(instance_id, arguments["command"], arguments.get("arguments"))
            if arguments["command"] == "screenshot":
                path = Path(result["path"]).resolve()
                root = (self.get(instance_id).directory / "screenshots").resolve()
                if not path.is_relative_to(root) or path.stat().st_size > 16 * 1024 * 1024:
                    raise ValueError("Screenshot is outside the instance artifact directory or too large.")
                images.append({"type": "image", "mimeType": "image/png", "data": base64.b64encode(path.read_bytes()).decode()})
        elif name == "playtest_logs":
            result = self.logs(**arguments)
        elif name == "playtest_stop":
            result = self.stop(**arguments)
        elif name == "playtest_autoplay":
            result = self.autoplay(**arguments)
        else:
            raise ValueError(f"Unknown tool: {name}")
        return {"content": [{"type": "text", "text": json.dumps(result, ensure_ascii=False)}] + images}


class RpcError(Exception):
    def __init__(self, code, message):
        super().__init__(message)
        self.code = code


class McpServer:
    def __init__(self, manager):
        self.manager = manager
        self.initialized = False

    def handle(self, request):
        if not isinstance(request, dict) or request.get("jsonrpc") != "2.0" or not isinstance(request.get("method"), str):
            return {"jsonrpc": "2.0", "id": None, "error": {"code": -32600, "message": "Invalid request"}}
        if "id" not in request:
            return None
        request_id = request["id"]
        try:
            method = request["method"]
            params = request.get("params", {})
            if not isinstance(params, dict):
                raise RpcError(-32602, "params must be an object")
            if method == "initialize":
                version = params.get("protocolVersion")
                self.initialized = True
                result = {"protocolVersion": version if version in PROTOCOL_VERSIONS else PROTOCOL_VERSIONS[-1],
                          "capabilities": {"tools": {}}, "serverInfo": {"name": "cardbash-playtest", "version": "1.0.0"},
                          "instructions": "Launch instances, then host and join on the same local game port. Prepare every player, "
                          "start the match on the host, and choose cards on each peer. Verify state on both peers. "
                          "Use rendered instances for screenshots, headless for logic. Stop instances when done. "
                          "Saves are isolated. Logs and artifacts remain under .godot/playtest/."}
            elif method == "ping":
                result = {}
            elif not self.initialized:
                raise RpcError(-32000, "Initialize first")
            elif method == "tools/list":
                result = {"tools": TOOLS}
            elif method == "tools/call":
                if not isinstance(params.get("name"), str) or not isinstance(params.get("arguments", {}), dict):
                    raise RpcError(-32602, "tools/call requires a name and object arguments")
                try:
                    result = self.manager.call(params["name"], params.get("arguments", {}))
                except Exception as error:
                    result = {"isError": True, "content": [{"type": "text", "text": str(error)}]}
            else:
                raise RpcError(-32601, f"Method not found: {method}")
            return {"jsonrpc": "2.0", "id": request_id, "result": result}
        except RpcError as error:
            return {"jsonrpc": "2.0", "id": request_id, "error": {"code": error.code, "message": str(error)}}

    def serve(self, source=sys.stdin, output=sys.stdout):
        try:
            for line in source:
                try:
                    request = json.loads(line)
                    response = self.handle(request)
                except json.JSONDecodeError:
                    response = {"jsonrpc": "2.0", "id": None, "error": {"code": -32700, "message": "Parse error"}}
                if response is not None:
                    output.write(json.dumps(response, ensure_ascii=False) + "\n")
                    output.flush()
        finally:
            self.manager.stop()


if __name__ == "__main__":
    def terminate(signum, frame):
        raise KeyboardInterrupt

    signal.signal(signal.SIGTERM, terminate)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=PROJECT)
    parser.add_argument("--godot", help="Godot 4.7 .NET executable; otherwise uses GODOT_BIN or discovery.")
    options = parser.parse_args()
    try:
        McpServer(PlaytestManager(options.project, options.godot)).serve()
    except KeyboardInterrupt:
        pass
