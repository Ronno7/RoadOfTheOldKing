"""Offline pacing-model regressions; no Unity, saves, network or generated art."""
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("pacing_sim", Path(__file__).resolve().parents[2] / "Pacing" / "pacing_sim.py")
sim = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(sim)


def export():
    return {"grid": {"origin": [0, 0], "cell": 1, "w": 40, "h": 5, "data": "0" * 200}, "items": []}


def beat(kind, label, x, **kwargs):
    return dict(kind=kind, label=label, pos=[x, 2], **kwargs)


def encounter(label, x, **kwargs):
    return beat("fight", label, x, synthetic=[dict(kind="scavenger", hp=20, cycle=2, dmg=10)], **kwargs)


class PacingTests(unittest.TestCase):
    def run_route(self, route, hp=100, flasks=3):
        self.state = dict(hp=hp, flasks=flasks)
        return sim.simulate(export(), route, "learning", self.state)

    def test_failed_retry_is_terminal_and_counts_both_deaths(self):
        route = [beat("rest", "fire", 0), encounter("wall", 10), beat("pickup", "unearned reward", 12)]
        with patch.object(sim, "fight", return_value=(2, 200, [1, 1], 0, 0, True)):
            tl, notes, deaths = self.run_route(route)
        self.assertEqual(deaths, 2)
        self.assertEqual(self.state["hp"], 0)
        self.assertFalse(self.state["completed"])
        self.assertEqual(self.state["blocked_at"], "wall")
        self.assertFalse(any(s["label"] == "unearned reward" for s in tl))
        self.assertIsNone(sim.metrics(tl)["completion_s"])
        self.assertEqual(sim.metrics(tl)["deaths"], 2)
        self.assertTrue(notes)

    def test_retry_rewalks_and_refights_without_repeating_puzzle_or_pickup(self):
        route = [beat("rest", "fire", 0), encounter("guard", 4), beat("puzzle", "sluice", 8, time=240),
                 beat("pickup", "shard", 9), encounter("test", 12)]
        calls = []
        def fight(group, tier, hp, flasks, tuning):
            calls.append((hp, flasks))
            return (2, 200 if len(calls) == 2 else 10, [1, 1], flasks, 0, len(calls) == 2)
        with patch.object(sim, "fight", side_effect=fight):
            tl, _, deaths = self.run_route(route)
        self.assertEqual(calls[2], (100, 3))
        self.assertEqual(sum(s["label"] == "guard" for s in tl), 2)
        self.assertEqual(sum(s["kind"] == "puzzle" for s in tl), 1)
        self.assertEqual(sum(s["kind"] == "pickup" for s in tl), 1)
        self.assertEqual(sum(s["distance"] for s in tl if s["kind"] == "travel" and s["retry"]), 12)
        self.assertEqual(sum(s["dur"] for s in tl if s["kind"] == "death"), sim.DEATH_SECONDS)
        self.assertEqual(deaths, 1)
        self.assertTrue(self.state["completed"])

    def test_latest_rest_shortens_return_and_restores_vitals(self):
        route = [beat("rest", "old fire", 0), encounter("old guard", 4), beat("rest", "new fire", 20), encounter("test", 24)]
        with patch.object(sim, "fight", side_effect=[(2, 10, [1], 2, 1, False), (2, 200, [1], 0, 0, True), (2, 10, [1], 3, 0, False)]) as fight:
            tl, _, _ = self.run_route(route)
        self.assertEqual(sum(s["label"] == "old guard" for s in tl), 1)
        self.assertEqual(sum(s["distance"] for s in tl if s["kind"] == "travel" and s["retry"]), 4)
        self.assertEqual(fight.call_args_list[-1].args[2:4], (100, 3))

    def test_checkpoint_survives_scene_boundary(self):
        legs = [dict(scene="a", name="Tutorial", beats=[beat("rest", "fire", 0), beat("exit", "exit", 5)]),
                dict(scene="b", name="Lowlands", beats=[beat("look", "arrival", 1), encounter("test", 8)])]
        state = dict(hp=100, flasks=3)
        with patch.object(sim, "fight", side_effect=[(2, 200, [1], 0, 0, True), (2, 10, [1], 3, 0, False)]):
            tl, _, _ = sim.simulate_journey({"a": export(), "b": export()}, legs, "learning", state)
        self.assertEqual([(s["leg"], s["distance"]) for s in tl if s["kind"] == "travel" and s["retry"]], [("Tutorial", 5), ("Lowlands", 7)])
        self.assertEqual(sum(s["kind"] == "exit" for s in tl), 1)

    def test_opening_scale_does_not_leak_to_next_encounter(self):
        with patch.object(sim, "fight", return_value=(2, 0, [1], 3, 0, False)) as fight:
            self.run_route([encounter("guardian", 0, opening_scale=1.5), encounter("later", 2)])
        self.assertEqual([c.args[4]["dmg_per_opening"] for c in fight.call_args_list], [30, 20])
        self.assertEqual(sim.TIERS["learning"]["dmg_per_opening"], 20)

    def test_real_impossible_encounter_cannot_earn_reward(self):
        impossible = beat("fight", "lethal", 3, synthetic=[dict(kind="guardian", hp=1000, cycle=2, dmg=1000)])
        tl, _, deaths = self.run_route([beat("rest", "fire", 0), impossible, beat("pickup", "reward", 4)])
        self.assertEqual(deaths, 2)
        self.assertEqual(len([s for s in tl if s["kind"] == "fight"]), 2)
        self.assertFalse(self.state["completed"])

    def test_depleted_arrival_can_succeed_after_full_respawn(self):
        tl, _, deaths = self.run_route([beat("look", "arrival", 0), encounter("guard", 4), beat("pickup", "reward", 5)], hp=1, flasks=0)
        self.assertEqual(deaths, 1)
        self.assertTrue(self.state["completed"])
        self.assertEqual(tl[-1]["label"], "reward")

    def test_persistent_fight_is_not_replayed(self):
        route = [beat("rest", "fire", 0), encounter("boss", 4, persistent=True), encounter("later", 8)]
        with patch.object(sim, "fight", side_effect=[(2, 10, [1], 3, 0, False), (2, 200, [1], 0, 0, True), (2, 10, [1], 3, 0, False)]):
            tl, _, _ = self.run_route(route)
        self.assertEqual(sum(s["label"] == "boss" for s in tl), 1)

    def test_missing_exported_enemy_is_not_silently_free(self):
        with self.assertRaisesRegex(ValueError, "Missing exported enemies"):
            self.run_route([beat("fight", "guard", 0, enemies=["missing guard"])])

    def test_optional_rest_is_not_checkpoint_for_other_tiers(self):
        route = [beat("rest", "fire", 0), beat("rest", "optional", 20, tiers=["newcomer"]), encounter("test", 25)]
        with patch.object(sim, "fight", side_effect=[(2, 200, [1], 0, 0, True), (2, 10, [1], 3, 0, False)]):
            tl, _, _ = self.run_route(route)
        self.assertEqual(sum(s["distance"] for s in tl if s["kind"] == "travel" and s["retry"]), 25)

    def test_leg_metrics_do_not_count_other_scenes_elapsed_time(self):
        tl = [dict(kind="travel", start=300, dur=10), dict(kind="fight", start=310, dur=5),
              dict(kind="travel", start=900, dur=4), dict(kind="fight", start=904, dur=2)]
        m = sim.metrics(tl)
        self.assertEqual(m["first_threat_s"], 10)
        self.assertEqual(m["longest_quiet_s"], 10)
        self.assertEqual(m["completion_s"], 21)

    def test_failed_first_leg_does_not_simulate_later_scene(self):
        legs = [dict(scene="a", name="Tutorial", beats=[encounter("wall", 0)]),
                dict(scene="b", name="Lowlands", beats=[beat("pickup", "reward", 0)])]
        state = dict(hp=100, flasks=3)
        with patch.object(sim, "fight", return_value=(2, 200, [1], 0, 0, True)):
            tl, _, deaths = sim.simulate_journey({"a": export(), "b": export()}, legs, "learning", state)
        self.assertEqual(deaths, 2)
        self.assertEqual({s["leg"] for s in tl}, {"Tutorial"})


if __name__ == "__main__":
    unittest.main()
