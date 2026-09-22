# Laya plays Snake

[Laya](https://github.com/NandhaKishorM/laya) is a non-autoregressive decision model: it answers
typed questions (`choice`, `score`, `noul`) about a piece of text in a single forward pass,
without generating anything. This directory hooks it up to the desktop build of the game so it
can play and collect apples.

| file | role |
|---|---|
| `laya_player.py` | the sidecar process the game starts with `--laya`; JSON lines on stdin/stdout |
| `board.py` | reads the board: what each legal move would do |
| `laya_policy.py` | turns those outcomes into Laya questions and picks the move |
| `snake_sim.py` | headless copy of the game's rules, for benchmarking |
| `evaluate.py` | benchmark a policy over many simulated games |
| `tests/` | unit tests (no GPU or model download needed) |

On the game side: `Snake.Core/Agent/` (the per-frame controller and the message format) and
`Snake.DesktopGL/LayaSidecar.cs` (starts the Python process).

## Setup

Python 3.10 or newer. With conda (this also provides the `ffmpeg` the game's content build needs):

```bash
conda create -n laya -c conda-forge python=3.12 ffmpeg
conda activate laya
pip install -r agent/requirements.txt
```

You also need the .NET 9 SDK; see [Building](../README.md#building). Then, from the repo root:

```bash
LAYA_PYTHON=$(which python) ./play_with_laya.sh
```

or, without the script:

```bash
dotnet run --project Snake/Snake.DesktopGL -c Release -- --laya --laya-python "$(which python)"
```

The first launch downloads the checkpoint (~800 MB) to the Hugging Face cache. After that,
loading takes about 30 s. The menu waits until Laya is ready, then starts the game. After a game
over, the next game starts two seconds later.

- The window title shows Laya's latest decision and confidence, e.g. `Snake - LAYA: UP 97%`, or
  `LAYA: OFFLINE` if the agent process has stopped.
- The terminal shows the agent's log, including each game's apples and a session summary.
- The keyboard still works. Arrow keys override Laya while held, Space/P pauses, and Esc saves and
  quits. If you press arrow keys on the menu or game-over screen, the agent stops auto-starting on
  that screen, so you can go to the shop or slots yourself.
- Apples Laya collects go into your normal save, just like your own, and are written when you quit with Esc.

### Options

Arguments after `--laya` (or passed to `play_with_laya.sh`) go to `laya_player.py`:

| option | effect |
|---|---|
| `--verbose` | log every decision (head, apple, choice, confidence, latency) |
| `--mask-fatal` | remove moves that kill the snake before asking Laya (see results below) |
| `--device cpu` | force CPU. It works, but at ~200-500 ms per decision the game runs slower |
| `--model`, `--subfolder` | another checkpoint, e.g. `--subfolder typed-decisions` |

The game itself reads two more: `--laya-python PATH` (default `$LAYA_PYTHON`, then `python3`) and
`--laya-agent PATH` (default: `agent/laya_player.py`, found by searching up from the executable).

## How it decides

```
 game (C#)                                      agent (Python)
 AgentController ── observe {snake, apples} ──▶ board.py        what each legal move does
   holds the snake                               laya_policy.py  one yes/no question per move,
   until the reply  ◀── decision {Up, 0.97} ──                   all in one Laya forward pass
```

1. After every snake move, the game sends the board.
2. `board.py` works out what each of the three legal moves does. It checks whether the move
   crashes into a wall or the body, and whether it reaches the apple or gets closer by path
   length. It also checks whether the move leads into a dead end, meaning a region smaller than
   the snake. The path search is tail-aware: a body cell counts as open once the tail will have
   moved off it by the time the head gets there.
3. `laya_policy.py` asks Laya one `noul` (yes/no) question per move, for example:

   > If the snake moves up, it stays alive and gets closer to the apple, 4 steps away. Does moving
   > up keep the snake alive and bring it closer to the apple?

   All the questions go through in a single forward pass (~40 ms on an RTX 4070 Laptop GPU),
   and the snake takes the move Laya gives the highest P(yes).
4. If the snake is due to move before the answer arrives, the game holds that frame. A slow
   agent slows the game down; it never acts on a stale board.

So the code handles perception and Laya makes the decision. Laya can't read a raw grid. Its own
README says the base checkpoints are near chance on zero-shot typed decisions. What it does
reliably is judge whether a stated outcome answers a yes/no question. By default Laya is also
offered the moves that kill the snake, so staying alive is its call too.

### Prompt format matters

On 200 labelled mid-game states (states where some moves were clearly better than others), here
is how often each question format picked a good move:

| question format | good move |
|---|---|
| one `score` question per move ("How good is this move?") | 54% |
| one `choice` question, outcomes in the state | 62% |
| one `choice` question, outcomes as the option descriptions | 78% |
| one `noul` question per move (above) | 97% |
| same, with the outcome wording below | 100% |

The wording of the outcomes mattered as much as the format. With the first phrasing
("eats the apple", "crashes into the wall and the snake dies"), Laya would sometimes score
crashing above eating: 0.71 against 0.41 in one case. Every game ended with the snake hitting a
wall, at 6.1 apples a game. Reusing the question's own words fixed it ("stays alive and reaches
the apple", "dies because it crashes into the wall"), taking it to 25.5. Laya checks a statement
against the question rather than reasoning about it. Making the path search tail-aware then took
it to 35.7: before that, a long snake's own body often made the apple look unreachable.

## How well it plays

Headless simulator, 15 games on the same seeds (1000-1014), no upgrades, RTX 4070 Laptop GPU:

| policy | mean apples | median | best | worst | avoidable deaths |
|---|---|---|---|---|---|
| random (among moves that survive), 200 games | 2.3 | 2 | 9 | 0 | – |
| **Laya (default)** | **35.7** | 34 | 52 | 21 | 14 of 15 |
| Laya `--mask-fatal` | 36.2 | 38 | 53 | 21 | 0 (by construction) |
| hand-written greedy rule, 200 games | 63.1 | 54 | 142 | 19 | 0 |

An "avoidable death" is one where Laya chose a fatal move while a move that survived was
available. In those states, every surviving move was also a "no" to the question: it got farther
from the apple, couldn't reach it, or led into a dead end. So Laya was comparing low scores
against each other. `--mask-fatal` removes the crash from the options. It barely changes the
score, because by then the snake is usually boxed in and dies a few moves later anyway. With
masking, Laya still makes 93.5% of the decisions; the rest had only one option left.

Laya agrees with the greedy rule on about 92% of moves. The gap in apples is probably due to
something the question doesn't ask about. When two moves both get closer, Laya has no reason to
prefer the one with more room, whereas the greedy rule does, so the snake likely coils into
itself sooner. I haven't measured this yet. (In 97 of its 200 games the greedy rule ended up circling
without reaching the apple; the simulator stops a game after 300 steps without eating. Laya
never did this in these runs.)

In the real game with default settings, the two complete games I watched ended at 43 and 49
apples. Inside the game the median decision took ~72 ms, well within the 170 ms step.

Reproduce:

```bash
cd agent
python evaluate.py --games 15              # Laya
python evaluate.py --games 15 --mask-fatal
python evaluate.py --games 200 --policy greedy
python -m pytest tests
```

## Protocol

One JSON object per line. The game writes to the agent's stdin and reads its stdout. The
agent's stderr is its log.

```jsonc
// agent -> game, once the model has loaded
{"type": "ready", "model": "convaiinnovations/laya", "device": "cuda", "mask_fatal": false}

// game -> agent, after every snake move (snake is head first; directions are Up/Down/Left/Right)
{"type": "observe", "id": 41, "run": 3, "step": 40, "width": 15, "height": 10,
 "snake": [[7,5],[6,5],[5,5]], "direction": "Right", "growing": false,
 "apples": [{"x": 3, "y": 4, "kind": "apple", "value": 1}],   // also "bonus", "golden" (+ time_left)
 "apples_collected": 6, "step_interval": 0.17}

// agent -> game
{"type": "decision", "id": 41, "direction": "Up", "confidence": 0.97,
 "scores": {"Up": 0.97, "Down": 0.21, "Right": 0.33}, "forced": false, "latency_ms": 44.1}
{"type": "error", "id": 41, "message": "..."}   // the snake keeps its heading

// game -> agent, when a game ends
{"type": "game_over", "run": 3, "steps": 212, "apples": 24, "length": 27}
```

Any process that speaks this protocol can play. `IAgentLink` is the seam on the C# side.

## Next steps

- **Fine-tuning.** The Laya repo has a Kaggle notebook for RLCD fine-tuning.
  `snake_sim.py` can generate as many labelled states as needed.
- **Longer-range planning.** The dead-end check only looks one move ahead. A better perception
  layer, such as tail reachability, would give Laya better facts to judge.
- **Slots and upgrades.** The agent does nothing outside the Menu, Playing and Game Over
  screens. The same pattern would work there: describe the options, then ask Laya.
