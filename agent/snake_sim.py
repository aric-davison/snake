"""Headless copy of the game's rules, for benchmarking policies without the MonoGame window.

Mirrors Snake.Core (GameEngine, Snake, Food) with no upgrades: 15x10 grid, a three-segment
snake starting at the centre heading right, one apple respawned at a random free cell.
`observation()` emits the same JSON message the game sends to the agent.
"""

import random
from typing import List, Optional, Tuple

from board import OPPOSITE, Point, step


class SnakeSim:
    def __init__(self, width: int = 15, height: int = 10, seed: Optional[int] = None):
        self.width = width
        self.height = height
        self.rng = random.Random(seed)
        self.reset()

    def reset(self):
        cx, cy = self.width // 2, self.height // 2
        self.snake: List[Point] = [(cx, cy), (cx - 1, cy), (cx - 2, cy)]
        self.direction = "Right"
        self.next_direction = "Right"
        self.growing = False
        self.apples_collected = 0
        self.steps = 0
        self.alive = True
        self.death_cause: Optional[str] = None
        self.food = self._spawn_food()

    def _spawn_food(self) -> Point:
        # Food.Spawn: 100 random attempts, then give up and use the last one.
        for _ in range(100):
            p = (self.rng.randrange(self.width), self.rng.randrange(self.height))
            if p not in self.snake:
                return p
        return p

    def set_direction(self, direction: str):
        if direction != OPPOSITE[self.direction]:
            self.next_direction = direction

    def tick(self) -> Tuple[bool, bool]:
        """Advance one step. Returns (alive, ate)."""
        self.direction = self.next_direction
        head = step(self.snake[0], self.direction)
        self.snake.insert(0, head)
        if self.growing:
            self.growing = False
        else:
            self.snake.pop()
        self.steps += 1

        if not (0 <= head[0] < self.width and 0 <= head[1] < self.height):
            self.alive, self.death_cause = False, "wall"
            return False, False
        if head in self.snake[1:]:
            self.alive, self.death_cause = False, "body"
            return False, False
        if head == self.food:
            self.apples_collected += 1
            self.growing = True
            self.food = self._spawn_food()
            return True, True
        return True, False

    def observation(self) -> dict:
        return {
            "type": "observe",
            "width": self.width,
            "height": self.height,
            "snake": [list(p) for p in self.snake],
            "direction": self.direction,
            "growing": self.growing,
            "apples": [{"x": self.food[0], "y": self.food[1], "kind": "apple", "value": 1}],
            "step": self.steps,
            "apples_collected": self.apples_collected,
        }
