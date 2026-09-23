#!/usr/bin/env python3
"""Exercise two real Godot peers through the same commands exposed over MCP."""
import argparse
import json
import math
import socket
import time

from server import PlaytestManager

FIREBALL = "EE277E3F-A8D2-4AE8-9DE4-01B8158DD000"


def wait_for(check, timeout=10):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        result = check()
        if result:
            return result
        time.sleep(0.1)
    raise AssertionError("Timed out waiting for expected state")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rendered", action="store_true", help="Also verify screenshot capture using the GPU.")
    parser.add_argument("--godot")
    args = parser.parse_args()
    manager = PlaytestManager(godot=args.godot)
    try:
        host = manager.launch(headless=not args.rendered)["instance_id"]
        client = manager.launch(headless=True)["instance_id"]
        print("Instances:", json.dumps([i.info() for i in manager.instances.values()]), flush=True)
        command = manager.request
        # The bridge is opt-in and authenticated, even on localhost.
        with socket.create_connection(("127.0.0.1", manager.get(host).port), timeout=3) as peer:
            peer.sendall(b'{"token":"wrong","command":"state"}\n')
            with peer.makefile("rb") as stream:
                reply = json.loads(stream.readline())
            assert not reply["ok"] and "token" in reply["error"]
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as reservation:
            reservation.bind(("127.0.0.1", 0))
            game_port = reservation.getsockname()[1]
        command(host, "host", {"port": game_port, "username": "Bridge host"})
        command(client, "join", {"port": game_port, "username": "Bridge client"})
        wait_for(lambda: len(command(host, "state")["lobby_players"]) == 2)
        command(host, "prepare_player", {"team": 0})
        command(client, "prepare_player", {"team": 1})
        wait_for(lambda: all(p["ready"] for p in command(host, "state")["lobby_players"]))
        command(host, "start_match", {"cards_per_round": 1})
        for instance in (host, client):
            def hand_ready():
                state = command(instance, "state")
                return state["card_selection_visible"] and any(c["guid"] == FIREBALL for c in state["hand"])
            wait_for(hand_ready)
            command(instance, "choose_card", {"guid": FIREBALL})
        wait_for(lambda: all(p["abilities"] for p in command(host, "state")["players"]))
        command(host, "wait", {"frames": 30})
        client_id = command(client, "state")["peer_id"]

        def player(instance, identifier):
            return next(p for p in command(instance, "state")["players"] if p["id"] == identifier)

        command(host, "place_player", {"player_id": client_id, "position": {"x": 2200, "y": 1000}})
        wait_for(lambda: abs(player(client, client_id)["position"]["x"] - 2200) < 5)
        start = player(client, client_id)["position"]
        command(client, "input", {"actions": ["MoveRight"], "frames": 30})
        end = player(client, client_id)["position"]
        assert end["x"] > start["x"] + 10, ("Movement input failed", start, end)
        wait_for(lambda: abs(player(host, client_id)["position"]["x"] - end["x"]) < 5)
        print("PASS: authentication, lobby, card selection, movement and replication", flush=True)

        command(host, "place_player", {"player_id": 1, "position": {"x": 2000, "y": 1000}})
        command(host, "place_player", {"player_id": client_id, "position": {"x": 2200, "y": 1000}})
        wait_for(lambda: math.dist(list(player(client, client_id)["position"].values()), [2200, 1000]) < 5)
        before = player(host, client_id)["health"]
        aim = {"x": 2200, "y": 1000}
        command(host, "input", {"aim": aim, "aim_space": "world", "frames": 30})
        assert player(host, 1)["aim"] == aim, "Virtual aim did not reach PlayerInput"
        command(host, "input", {"actions": ["Ability1"], "aim": aim, "aim_space": "world", "frames": 3})
        after = wait_for(lambda: (p if (p := player(host, client_id))["health"] < before else None))
        wait_for(lambda: abs(player(client, client_id)["health"] - after["health"]) < 0.01)
        print(f"PASS: Fireball damage replicated ({before} -> {after['health']})", flush=True)
        assert command(host, "events")
        assert command(host, "tree", {"depth": 2})
        assert any(c["guid"] == FIREBALL for c in command(host, "catalog"))
        if args.rendered:
            response = manager.call("playtest_command", {"instance_id": host, "command": "screenshot"})
            assert response["content"][1]["type"] == "image"
            print("PASS: screenshot", response["content"][0]["text"], flush=True)
        else:
            try:
                command(host, "screenshot")
                raise AssertionError("Headless screenshot should fail explicitly")
            except RuntimeError as error:
                assert "headless=false" in str(error)
        command(host, "leave")
        wait_for(lambda: command(client, "state")["stage"] == "menu")
        for instance in (host, client):
            errors = [line for line in manager.logs(instance, 1000)["text"].splitlines()
                      if "ERROR:" in line or "Exception:" in line]
            assert not errors, errors
        print("PASS: return to menu; no runtime errors", flush=True)
    finally:
        manager.stop()


if __name__ == "__main__":
    main()
