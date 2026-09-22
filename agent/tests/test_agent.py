"""Tests for board analysis, prompt building and the policy's decision rule.

Laya itself is replaced by a fake, so these run in milliseconds without a GPU or a download.
"""

from board import Observation, analyse, candidate_directions
from laya_policy import LayaPolicy, build_questions, describe_outcome
from snake_sim import SnakeSim


def obs(snake, direction="Right", apple=(0, 0), growing=False, width=15, height=10):
    return Observation.from_json({
        "width": width, "height": height, "snake": [list(p) for p in snake],
        "direction": direction, "growing": growing,
        "apples": [{"x": apple[0], "y": apple[1], "kind": "apple", "value": 1}],
    })


def moves_by_direction(o):
    return {m.direction: m for m in analyse(o)}


def test_parses_the_games_observe_message():
    o = Observation.from_json({
        "type": "observe", "id": 3, "run": 2, "step": 10, "width": 15, "height": 10,
        "snake": [[7, 5], [6, 5], [5, 5]], "direction": "Right", "growing": True,
        "apples": [{"x": 3, "y": 4, "kind": "apple", "value": 1},
                   {"x": 9, "y": 1, "kind": "golden", "value": 25, "time_left": 6.5}],
        "apples_collected": 4, "step_interval": 0.17,
    })
    assert o.head == (7, 5) and o.growing
    assert [a.kind for a in o.apples] == ["apple", "golden"]
    assert o.apples[1].time_left == 6.5


def test_reversing_is_not_offered():
    assert candidate_directions(obs([(7, 5), (6, 5), (5, 5)], "Right")) == ["Up", "Down", "Right"]


def test_wall_is_fatal():
    m = moves_by_direction(obs([(14, 5), (13, 5), (12, 5)], "Right"))
    assert m["Right"].fatal and m["Right"].cause == "wall"
    assert not m["Up"].fatal and not m["Down"].fatal


def test_body_is_fatal():
    # Head at (5,5) heading up, body curls round to its right: moving right bites it.
    m = moves_by_direction(obs([(5, 5), (5, 6), (6, 6), (6, 5), (6, 4)], "Up"))
    assert m["Right"].fatal and m["Right"].cause == "body"


def test_tail_cell_is_safe_unless_growing():
    # A 2x2 loop: the head can step onto the tail's cell because the tail moves away...
    loop = [(5, 5), (5, 6), (6, 6), (6, 5)]
    assert not moves_by_direction(obs(loop, "Up"))["Right"].fatal
    # ...except on the step right after eating, when the tail stays put.
    assert moves_by_direction(obs(loop, "Up", growing=True))["Right"].fatal


def test_distance_and_eating():
    m = moves_by_direction(obs([(7, 5), (6, 5), (5, 5)], "Right", apple=(9, 5)))
    assert m["Right"].closer and m["Right"].dist_after == 1
    assert not m["Up"].closer and m["Up"].dist_after == 3
    m = moves_by_direction(obs([(7, 5), (6, 5), (5, 5)], "Right", apple=(8, 5)))
    assert m["Right"].eats and m["Right"].closer


def test_dead_end_is_described():
    # Head at (1,0) heading left along the top wall. The body walls off the corner cell (0,0)
    # and the tail is too far away to free a way out in time.
    o = obs([(1, 0), (2, 0), (2, 1), (1, 1), (0, 1), (0, 2), (0, 3), (1, 3), (2, 3)], "Left",
            apple=(10, 8))
    m = moves_by_direction(o)
    assert m["Left"].space == 1
    assert "dead end" in describe_outcome(m["Left"], len(o.snake))


def test_chasing_the_tail_is_not_a_dead_end():
    # Same corner, but the tail is right behind (0,0) and moves out of the way.
    o = obs([(1, 0), (2, 0), (2, 1), (1, 1), (0, 1), (0, 2)], "Left", apple=(10, 8))
    m = moves_by_direction(o)
    assert m["Left"].space > len(o.snake)
    assert "dead end" not in describe_outcome(m["Left"], len(o.snake))


def test_apple_behind_the_body_is_reachable_once_the_tail_moves():
    # A U-shaped snake whose body separates the head from the apple at (3,5); the only way in
    # is through cells the tail is about to leave.
    snake = [(4, 4), (4, 3), (3, 3), (2, 3), (1, 3), (1, 4), (1, 5), (1, 6), (2, 6), (3, 6), (4, 6), (4, 5)]
    o = obs(snake, "Down", apple=(3, 5), width=5, height=7)
    m = moves_by_direction(o)
    assert any(x.dist_after is not None for x in m.values() if not x.fatal)


def test_outcomes_use_the_questions_terms():
    o = obs([(14, 5), (13, 5), (12, 5)], "Right", apple=(14, 4))
    m = moves_by_direction(o)
    assert describe_outcome(m["Right"], 3).startswith("dies")
    assert describe_outcome(m["Up"], 3) == "stays alive and reaches the apple"
    assert "farther" in describe_outcome(m["Down"], 3)


def test_one_noul_question_per_move():
    o = obs([(7, 5), (6, 5), (5, 5)], "Right", apple=(7, 2))
    qs = build_questions(o, analyse(o))
    assert set(qs) == {"Up", "Down", "Right"}
    assert all(q["type"] == "noul" for q in qs.values())
    assert "If the snake moves up, it stays alive and gets closer to the apple" in qs["Up"]["instructions"]


class FakeAgent:
    """Stands in for laya.Agent: answers each noul question with a preset P(yes)."""

    device = "cpu"

    def __init__(self, p_yes):
        self.p_yes = p_yes
        self.calls = []

    def predict(self, state, questions):
        self.calls.append(questions)
        return {"answers": {k: {"type": "noul", "noul": self.p_yes[k]} for k in questions}}


def test_policy_takes_the_move_laya_rates_highest():
    o = obs([(7, 5), (6, 5), (5, 5)], "Right", apple=(7, 2))
    d = LayaPolicy(FakeAgent({"Up": 0.2, "Down": 0.9, "Right": 0.4})).decide(o)
    assert d.direction == "Down" and d.confidence == 0.9 and not d.forced


def test_unmasked_policy_can_choose_a_fatal_move():
    o = obs([(14, 5), (13, 5), (12, 5)], "Right")
    d = LayaPolicy(FakeAgent({"Up": 0.1, "Down": 0.1, "Right": 0.8})).decide(o)
    assert d.direction == "Right"


def test_mask_fatal_hides_fatal_moves_from_laya():
    o = obs([(14, 5), (13, 5), (12, 5)], "Right")
    agent = FakeAgent({"Up": 0.1, "Down": 0.3, "Right": 0.8})
    d = LayaPolicy(agent, mask_fatal=True).decide(o)
    assert set(agent.calls[0]) == {"Up", "Down"}
    assert d.direction == "Down"


def test_single_option_skips_the_model():
    # Top-right corner heading right: Up and Right are walls, so Down is the only survivor.
    o = obs([(14, 0), (13, 0), (12, 0)], "Right")
    agent = FakeAgent({})
    d = LayaPolicy(agent, mask_fatal=True).decide(o)
    assert agent.calls == [] and d.forced and d.direction == "Down"


def test_sim_matches_game_rules():
    sim = SnakeSim(seed=0)
    assert sim.snake == [(7, 5), (6, 5), (5, 5)] and sim.direction == "Right"
    sim.food = (8, 5)
    alive, ate = sim.tick()
    assert alive and ate and sim.growing and len(sim.snake) == 3
    sim.food = (0, 0)
    sim.tick()
    assert len(sim.snake) == 4  # grows on the move after eating, like Snake.Grow()
    sim.set_direction("Left")   # reversal is ignored
    sim.tick()
    assert sim.direction == "Right"
