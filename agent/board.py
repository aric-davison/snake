"""Board analysis for the Snake agent.

Turns a raw game observation (the JSON the game sends every step) into per-move facts that a
text model can read: whether a move is fatal, whether it brings the snake closer to an apple,
and how much open space it leaves. None of this chooses a move; it only describes the options.
"""

from collections import deque
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple

Point = Tuple[int, int]

DIRECTIONS: Dict[str, Point] = {
    "Up": (0, -1),
    "Down": (0, 1),
    "Left": (-1, 0),
    "Right": (1, 0),
}

OPPOSITE = {"Up": "Down", "Down": "Up", "Left": "Right", "Right": "Left"}


@dataclass
class Apple:
    pos: Point
    kind: str = "apple"
    value: int = 1
    time_left: Optional[float] = None


@dataclass
class Observation:
    width: int
    height: int
    snake: List[Point]  # head first
    direction: str
    growing: bool = False
    apples: List[Apple] = field(default_factory=list)

    @property
    def head(self) -> Point:
        return self.snake[0]

    @classmethod
    def from_json(cls, msg: dict) -> "Observation":
        return cls(
            width=int(msg["width"]),
            height=int(msg["height"]),
            snake=[(int(x), int(y)) for x, y in msg["snake"]],
            direction=msg["direction"],
            growing=bool(msg.get("growing", False)),
            apples=[
                Apple(
                    pos=(int(a["x"]), int(a["y"])),
                    kind=a.get("kind", "apple"),
                    value=int(a.get("value", 1)),
                    time_left=a.get("time_left"),
                )
                for a in msg.get("apples", [])
            ],
        )


@dataclass
class MoveInfo:
    direction: str
    fatal: bool
    cause: Optional[str] = None       # "wall" or "body" when fatal
    eats: bool = False
    dist_before: Optional[int] = None  # path length to the target apple from the current head
    dist_after: Optional[int] = None   # path length after this move (None = unreachable)
    space: int = 0                     # cells reachable after this move, counting ones the tail vacates

    @property
    def closer(self) -> bool:
        if self.eats:
            return True
        return (self.dist_before is not None and self.dist_after is not None
                and self.dist_after < self.dist_before)


def step(p: Point, direction: str) -> Point:
    dx, dy = DIRECTIONS[direction]
    return p[0] + dx, p[1] + dy


def candidate_directions(obs: Observation) -> List[str]:
    """The three directions the game will accept (it ignores a 180-degree reversal)."""
    return [d for d in DIRECTIONS if d != OPPOSITE[obs.direction]]


def _in_bounds(obs: Observation, p: Point) -> bool:
    return 0 <= p[0] < obs.width and 0 <= p[1] < obs.height


def _body_after_move(obs: Observation, new_head: Point) -> List[Point]:
    # Mirrors Snake.Move(): the tail stays put only on the step right after eating.
    kept = obs.snake if obs.growing else obs.snake[:-1]
    return [new_head] + kept


def _free_at(body: List[Point], growing: bool) -> Dict[Point, int]:
    """For each body cell, the number of moves after which the tail has left it.

    body[0] is the head. Segment i is vacated after len(body) - i moves, one later if the
    snake is about to grow. The head's own cell is never needed (the snake never re-enters it
    in one move), so it is left out.
    """
    n = len(body)
    return {p: n - i + (1 if growing else 0) for i, p in enumerate(body) if i > 0}


def _bfs(obs: Observation, start: Point, free_at: Dict[Point, int]) -> Dict[Point, int]:
    """Path lengths from start. A body cell can be entered once the tail has moved off it."""
    dist = {start: 0}
    queue = deque([start])
    while queue:
        p = queue.popleft()
        t = dist[p] + 1
        for d in DIRECTIONS:
            n = step(p, d)
            if n in dist or not _in_bounds(obs, n) or free_at.get(n, 0) > t:
                continue
            dist[n] = t
            queue.append(n)
    return dist


def target_apple(obs: Observation) -> Optional[Apple]:
    """The apple the snake should go for: the nearest one by path, ties broken by value."""
    if not obs.apples:
        return None
    dist = _bfs(obs, obs.head, _free_at(obs.snake, obs.growing))
    reachable = [a for a in obs.apples if a.pos in dist]
    pool = reachable or obs.apples
    manhattan = lambda a: abs(a.pos[0] - obs.head[0]) + abs(a.pos[1] - obs.head[1])
    return min(pool, key=lambda a: (dist.get(a.pos, manhattan(a)), -a.value))


def analyse(obs: Observation) -> List[MoveInfo]:
    """Describe the outcome of each legal move."""
    target = target_apple(obs)
    before = None
    if target is not None:
        before = _bfs(obs, obs.head, _free_at(obs.snake, obs.growing)).get(target.pos)

    moves = []
    for d in candidate_directions(obs):
        new_head = step(obs.head, d)
        if not _in_bounds(obs, new_head):
            moves.append(MoveInfo(d, fatal=True, cause="wall"))
            continue
        body = _body_after_move(obs, new_head)
        if new_head in body[1:]:
            moves.append(MoveInfo(d, fatal=True, cause="body"))
            continue

        grows = any(a.pos == new_head for a in obs.apples)
        reach = _bfs(obs, new_head, _free_at(body, grows))
        info = MoveInfo(d, fatal=False, space=len(reach), dist_before=before)
        if target is not None:
            info.eats = new_head == target.pos
            info.dist_after = 0 if info.eats else reach.get(target.pos)
        moves.append(info)
    return moves


def apple_offset_text(obs: Observation, apple: Apple) -> str:
    """'3 cells left and 2 cells up' -- the target's position relative to the head."""
    dx = apple.pos[0] - obs.head[0]
    dy = apple.pos[1] - obs.head[1]
    parts = []
    if dx:
        parts.append("%d cell%s %s" % (abs(dx), "" if abs(dx) == 1 else "s", "right" if dx > 0 else "left"))
    if dy:
        parts.append("%d cell%s %s" % (abs(dy), "" if abs(dy) == 1 else "s", "down" if dy > 0 else "up"))
    return " and ".join(parts) or "at the head"
