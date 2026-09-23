"""Laya decisions translated into the existing, constrained playtest commands."""
from __future__ import annotations

from contextlib import redirect_stdout
import math
import os
import sys


class LayaPolicy:
    def __init__(self, agent=None):
        self.agent = agent

    def load(self):
        if self.agent is None:
            # The MCP transport owns stdout. Model download/import diagnostics use stderr.
            os.environ.setdefault("USE_TF", "0")
            with redirect_stdout(sys.stderr):
                try:
                    import laya
                except ImportError as error:
                    raise RuntimeError(
                        "Laya is not installed in this Python environment. Follow tools/playtest/README.md "
                        "and run the bridge with .venv-playtest/bin/python.") from error
                options = {}
                if device := os.environ.get("CARDBASH_LAYA_DEVICE"):
                    options["device"] = device
                self.agent = laya.load(os.environ.get("CARDBASH_LAYA_MODEL", "convaiinnovations/laya"), **options)
        return self.agent

    def choose(self, state, questions):
        with redirect_stdout(sys.stderr):
            result = self.load().predict(state, questions)
        answers = result.get("answers", {})
        for name, question in questions.items():
            answer = answers.get(name, {})
            if answer.get("choice") not in question["criteria"]:
                raise RuntimeError(f"Laya returned an invalid {name} choice: {answer!r}")
        return answers

    def decide(self, state, frames=12):
        if state["stage"] != "game":
            raise ValueError("Start a match before asking Laya to play.")
        players = state.get("players") or []
        player = next((p for p in players if p["id"] == state["peer_id"]), None)
        wait = {"command": "wait", "arguments": {"frames": frames}, "answers": {}}
        if state.get("card_selection_visible"):
            # Duplicate copies of the same card share one legal choice.
            hand = {c["guid"]: c for c in state.get("hand", [])}
            if not hand:
                return wait
            cards = list(hand.values())
            choices = {f"card{i}": c["name"] for i, c in enumerate(cards)}
            observation = {"health": player.get("health") if player else None,
                           "equipped": [a.get("name", a["guid"]) for a in player.get("abilities", [])] if player else []}
            answers = self.choose(observation, {"card": question("Choose a card to improve combat survival and damage.", choices)})
            card = cards[list(choices).index(answers["card"]["choice"])]
            return {"command": "choose_card", "arguments": {"guid": card["guid"]}, "answers": answers}
        if not player or not player.get("targetable"):
            return wait
        enemies = [p for p in players if p["id"] != player["id"] and p["team"] != player["team"] and p.get("targetable")]
        if not enemies:
            return wait
        position = player["position"]
        enemy = min(enemies, key=lambda p: distance(position, p["position"]))
        dx, dy = enemy["position"]["x"] - position["x"], enemy["position"]["y"] - position["y"]
        abilities = player.get("abilities", [])
        # A charge can be ready while the next charge is cooling down.
        available = {f"Ability{a['slot']}": a.get("name", a["guid"]) for a in abilities
                     if a.get("stacks", 0) > 0 and 1 <= a["slot"] <= 4}
        observation = {"health": player["health"], "max_health": player["max_health"],
                       "enemy_health": enemy["health"], "distance": round(math.hypot(dx, dy)),
                       "ready_abilities": list(available.values())}
        answers = self.choose(observation, {
            "movement": question("Choose movement to survive and attack the nearest enemy.", {
                "approach": "Move closer to attack", "retreat": "Move away to avoid damage",
                "strafe_left": "Circle the enemy to evade attacks", "strafe_right": "Circle in the opposite direction",
                "stay": "Hold position"}),
            "ability": question("Choose a ready ability to use against the enemy, or wait.",
                                {"none": "Save abilities and wait", **available}),
        })
        direction = {"approach": (dx, dy), "retreat": (-dx, -dy),
                     "strafe_left": (dy, -dx), "strafe_right": (-dy, dx), "stay": (0, 0)}[answers["movement"]["choice"]]
        actions = []
        length = math.hypot(*direction)
        if length > 1:
            x, y = (v / length for v in direction)
            if abs(x) > 0.35:
                actions.append("MoveRight" if x > 0 else "MoveLeft")
            if abs(y) > 0.35:
                actions.append("MoveDown" if y > 0 else "MoveUp")
        if answers["ability"]["choice"] != "none":
            actions.append(answers["ability"]["choice"])
        return {"command": "input", "arguments": {"actions": actions, "frames": frames,
                "aim": enemy["position"], "aim_space": "world"}, "answers": answers}


def question(instructions, criteria):
    return {"type": "choice", "instructions": instructions, "criteria": criteria}


def distance(a, b):
    return math.hypot(a["x"] - b["x"], a["y"] - b["y"])
