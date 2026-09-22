"""Laya as a Snake policy.

Each step the board analysis describes what every legal move would do ("it eats the apple",
"it crashes into the wall and the snake dies", ...). Laya then answers one yes/no (`noul`)
question per move -- "does this move keep the snake alive and bring it closer to the apple?" --
and all of them are scored in a single forward pass. The snake takes the move Laya is most
confident about.

In a prompt-format comparison on 200 labelled mid-game states, this per-move `noul` format
chose a good move 97% of the time, and 100% with the wording in describe_outcome. A single
`choice` question over the same descriptions managed 78%, and a `score` question 54%
(see agent/README.md).
"""

import time
from dataclasses import dataclass, field
from typing import Dict, List, Optional

from board import MoveInfo, Observation, analyse, apple_offset_text, target_apple

DEFAULT_MODEL = "convaiinnovations/laya"

QUESTION = ("If the snake moves {d}, it {outcome}. "
            "Does moving {d} keep the snake alive and bring it closer to the apple?")


def describe_outcome(move: MoveInfo, length: int) -> str:
    """One clause saying what the move does, phrased to follow 'If the snake moves up, it ...'.

    The wording deliberately reuses the question's own terms ("stays alive", "closer to the
    apple"). Laya matches the statement against the question, and looser phrasing such as
    "eats the apple" or "crashes into the wall and the snake dies" led it to rate crashing
    above eating.
    """
    if move.fatal:
        if move.cause == "wall":
            return "dies because it crashes into the wall"
        return "dies because it runs into its own body"
    if move.eats:
        text = "stays alive and reaches the apple"
    elif move.dist_after is None:
        text = "stays alive but cannot reach the apple"
    elif move.closer:
        text = "stays alive and gets closer to the apple, %d steps away" % move.dist_after
    else:
        text = "stays alive but gets farther from the apple, %d steps away" % move.dist_after
    if move.space < length:
        text += ", into a dead end"
    return text


def build_state(obs: Observation) -> dict:
    state = {"game": "snake", "snake_length": len(obs.snake), "heading": obs.direction.lower()}
    target = target_apple(obs)
    if target is not None:
        state["apple"] = apple_offset_text(obs, target) + " of the snake's head"
    return state


def build_questions(obs: Observation, moves: List[MoveInfo]) -> Dict[str, dict]:
    length = len(obs.snake)
    return {
        m.direction: {
            "type": "noul",
            "instructions": QUESTION.format(d=m.direction.lower(), outcome=describe_outcome(m, length)),
        }
        for m in moves
    }


@dataclass
class Decision:
    direction: str
    confidence: float
    scores: Dict[str, float] = field(default_factory=dict)  # P(yes) per direction
    forced: bool = False       # only one option was offered, so Laya was not consulted
    latency_ms: float = 0.0


class LayaPolicy:
    """Chooses a direction by asking Laya about every legal move.

    mask_fatal=False (default) offers Laya every move the game accepts, including ones that
    kill the snake, so survival is Laya's call too. mask_fatal=True removes fatal moves before
    asking, the usual action-masking setup in game-playing agents.
    """

    def __init__(self, agent, mask_fatal: bool = False):
        self.agent = agent
        self.mask_fatal = mask_fatal

    @classmethod
    def load(cls, model: str = DEFAULT_MODEL, subfolder: Optional[str] = None,
             device: Optional[str] = None, mask_fatal: bool = False) -> "LayaPolicy":
        import laya

        policy = cls(laya.load(model, device=device, subfolder=subfolder), mask_fatal=mask_fatal)
        policy.warm_up()
        return policy

    @property
    def device(self) -> str:
        return str(self.agent.device)

    def warm_up(self):
        """The first forward pass pays CUDA start-up (~2 s); keep that out of the first move."""
        from snake_sim import SnakeSim

        self.decide(Observation.from_json(SnakeSim(seed=0).observation()))

    def options(self, obs: Observation) -> List[MoveInfo]:
        moves = analyse(obs)
        if self.mask_fatal:
            return [m for m in moves if not m.fatal] or moves
        return moves

    def decide(self, obs: Observation) -> Decision:
        moves = self.options(obs)
        if len(moves) == 1:
            return Decision(moves[0].direction, 1.0, {moves[0].direction: 1.0}, forced=True)

        start = time.perf_counter()
        answers = self.agent.predict(build_state(obs), build_questions(obs, moves))["answers"]
        latency = (time.perf_counter() - start) * 1000

        scores = {d: a["noul"] for d, a in answers.items()}
        best = max(scores, key=scores.get)
        return Decision(best, scores[best], scores, latency_ms=latency)
