"""Policy and MCP integration checks using an SDK-shaped fake; no weights needed."""
import copy
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

from laya_policy import LayaPolicy
from server import PlaytestManager


def state():
    return {"stage": "game", "physics_frame": 100, "peer_id": 1,
            "card_selection_visible": False, "hand": [], "players": [
                {"id": 1, "team": 0, "position": {"x": 0, "y": 0}, "health": 100, "max_health": 100,
                 "targetable": True, "abilities": [
                     {"guid": "fire", "name": "Fireball", "slot": 1, "stacks": 1, "cooldown": 2},
                     {"guid": "ice", "name": "Ice", "slot": 2, "stacks": 0, "cooldown": 2}]},
                {"id": 2, "team": 1, "position": {"x": 100, "y": 0}, "health": 100, "targetable": True},
                {"id": 3, "team": 0, "position": {"x": 5, "y": 0}, "health": 100, "targetable": True},
            ]}


class Agent:
    def __init__(self, **choices):
        self.choices = choices
        self.questions = None

    def predict(self, observation, questions):
        print("Model diagnostic")
        self.questions = questions
        return {"answers": {name: {"choice": self.choices[name], "confidence": 0.8} for name in questions}}


class PolicyTests(unittest.TestCase):
    def test_aims_at_enemy_and_uses_charges_even_during_recharge(self):
        agent = Agent(movement="approach", ability="Ability1")
        output = io.StringIO()
        with patch("sys.stdout", output), patch("sys.stderr", io.StringIO()):
            decision = LayaPolicy(agent).decide(state())
        self.assertEqual(output.getvalue(), "")
        self.assertEqual(decision["arguments"]["actions"], ["MoveRight", "Ability1"])
        self.assertEqual(decision["arguments"]["aim"], {"x": 100, "y": 0})
        self.assertNotIn("Ability2", agent.questions["ability"]["criteria"])

    def test_card_selection_uses_actual_hand_guid(self):
        snapshot = state()
        snapshot.update(card_selection_visible=True, hand=[{"guid": "ice", "name": "Ice"},
                                                         {"guid": "fire", "name": "Fireball"}])
        decision = LayaPolicy(Agent(card="card1")).decide(snapshot)
        self.assertEqual(decision["command"], "choose_card")
        self.assertEqual(decision["arguments"], {"guid": "fire"})

    def test_dead_player_or_no_enemies_wait_without_model_call(self):
        for no_enemies in (False, True):
            snapshot = state()
            snapshot["players"][1 if no_enemies else 0]["targetable"] = False
            agent = Mock()
            decision = LayaPolicy(agent).decide(snapshot)
            self.assertEqual(decision["command"], "wait")
            agent.predict.assert_not_called()

    def test_arbitrary_actions_are_rejected(self):
        with self.assertRaisesRegex(RuntimeError, "invalid ability"):
            LayaPolicy(Agent(movement="stay", ability="Quit")).decide(state())

    def test_missing_dependency_is_actionable(self):
        with patch.dict("sys.modules", {"laya": None}):
            with self.assertRaisesRegex(RuntimeError, ".venv-playtest/bin/python"):
                LayaPolicy().load()

    def test_autoplay_reads_fresh_state_and_logs_decisions(self):
        with tempfile.TemporaryDirectory() as directory:
            manager = PlaytestManager()
            manager.instances["test"] = SimpleNamespace(directory=Path(directory))
            manager.laya_policy = LayaPolicy(Agent(movement="stay", ability="none"))
            snapshots = [state(), state(), state(), state(), state()]
            snapshots[3]["players"][0]["targetable"] = False
            manager.request = Mock(side_effect=snapshots)
            result = manager.autoplay("test", steps=2)
            self.assertEqual([d["command"] for d in result["decisions"]], ["input", "wait"])
            records = [json.loads(line) for line in Path(result["log"]).read_text().splitlines()]
            self.assertEqual(records, result["decisions"])
            self.assertEqual(manager.request.call_count, 5)

    def test_invalid_bounds_do_not_issue_commands(self):
        manager = PlaytestManager()
        manager.request = Mock()
        for arguments in ({"steps": 0}, {"steps": 21}, {"steps": True}, {"frames": 61}, {"frames": 0}):
            with self.assertRaises(ValueError):
                manager.autoplay("test", **arguments)
        manager.request.assert_not_called()

    def test_leaving_match_ends_batch(self):
        with tempfile.TemporaryDirectory() as directory:
            manager = PlaytestManager()
            manager.instances["test"] = SimpleNamespace(directory=Path(directory))
            manager.laya_policy = Mock()
            snapshot = copy.deepcopy(state())
            snapshot["stage"] = "menu"
            manager.request = Mock(side_effect=[state(), snapshot])
            self.assertEqual(manager.autoplay("test")["decisions"], [])
            manager.laya_policy.decide.assert_not_called()
