#!/usr/bin/env python3
"""Launch two isolated players and let Laya play both sides of a Cardbash match."""
import argparse
import json
import socket
import time

from laya_policy import LayaPolicy, question
from server import PlaytestManager


def wait_for(check):
    deadline = time.monotonic() + 15
    while time.monotonic() < deadline:
        if check():
            return
        time.sleep(0.1)
    raise RuntimeError("Timed out preparing the match; inspect .godot/playtest/*/output.log.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", help="Path to Godot 4.7 .NET.")
    parser.add_argument("--headless", action="store_true", help="Run without game windows.")
    parser.add_argument("--seconds", type=int, default=120, help="Play duration after setup (default 120).")
    parser.add_argument("--frames", type=int, default=12, help="Physics frames per action (1–60).")
    parser.add_argument("--check", action="store_true", help="Download/load Laya and test inference without launching Godot.")
    args = parser.parse_args()
    if args.seconds <= 0 or not 1 <= args.frames <= 60:
        parser.error("seconds must be positive and frames must be between 1 and 60")
    policy = LayaPolicy()
    # Preload before starting the real-time match, also checking the SDK response shape.
    answer = policy.choose({"health": 10, "enemy_health": 100}, {
        "movement": question("Choose a combat movement.", {"approach": "Move closer", "retreat": "Move away"})})
    print("Laya ready:", json.dumps(answer), flush=True)
    if args.check:
        return
    manager = PlaytestManager(godot=args.godot)
    manager.laya_policy = policy
    try:
        host = manager.launch(headless=args.headless)["instance_id"]
        client = manager.launch(headless=args.headless)["instance_id"]
        print("Instances:", json.dumps([i.info() for i in manager.instances.values()]), flush=True)
        command = manager.request
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as reservation:
            reservation.bind(("127.0.0.1", 0))
            port = reservation.getsockname()[1]
        command(host, "host", {"port": port, "username": "Laya host"})
        command(client, "join", {"port": port, "username": "Laya client"})
        wait_for(lambda: len(command(host, "state")["lobby_players"]) == 2)
        for team, instance in enumerate((host, client)):
            command(instance, "prepare_player", {"team": team})
        wait_for(lambda: all(p["ready"] for p in command(host, "state")["lobby_players"]))
        command(host, "start_match", {"cards_per_round": 1})
        wait_for(lambda: command(client, "state")["stage"] == "game")
        deadline = time.monotonic() + args.seconds
        while time.monotonic() < deadline:
            for instance in (host, client):
                if command(instance, "state")["stage"] != "game":
                    return
                result = manager.autoplay(instance, frames=args.frames)
                print(json.dumps({"instance_id": instance, "decisions": result["decisions"]}), flush=True)
                if time.monotonic() >= deadline:
                    break
    finally:
        manager.stop()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
