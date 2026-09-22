"""Benchmark a policy on the headless simulator.

    python agent/evaluate.py --games 20                 # Laya
    python agent/evaluate.py --games 20 --mask-fatal    # Laya, fatal moves removed first
    python agent/evaluate.py --games 200 --policy random
    python agent/evaluate.py --games 200 --policy greedy

`random` picks uniformly among moves that don't die immediately; `greedy` is a hand-written
rule (eat, else get closer, avoid dead ends). They bracket Laya: the floor and a reasonable
scripted player.
"""

import argparse
import random
import statistics
import time
import warnings

from board import Observation, analyse
from snake_sim import SnakeSim


def random_policy(rng):
    def decide(obs):
        safe = [m.direction for m in analyse(obs) if not m.fatal]
        return rng.choice(safe or ["Up", "Down", "Left", "Right"])
    return decide


def greedy_policy(obs):
    moves = analyse(obs)
    safe = [m for m in moves if not m.fatal]
    if not safe:
        return moves[0].direction
    roomy = [m for m in safe if m.space >= len(obs.snake)] or safe
    far = 10 ** 6
    best = min(roomy, key=lambda m: (not m.eats, not m.closer,
                                     far if m.dist_after is None else m.dist_after, -m.space))
    return best.direction


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--policy", choices=["laya", "random", "greedy"], default="laya")
    ap.add_argument("--games", type=int, default=20)
    ap.add_argument("--seed", type=int, default=1000, help="first game's seed; game i uses seed+i")
    ap.add_argument("--max-idle", type=int, default=300,
                    help="end a game after this many steps without eating (a loop)")
    ap.add_argument("--mask-fatal", action="store_true")
    ap.add_argument("--model", default=None)
    ap.add_argument("--subfolder", default=None)
    ap.add_argument("--device", default=None)
    args = ap.parse_args()

    agree = total = 0
    latencies = []
    if args.policy == "laya":
        warnings.filterwarnings("ignore")
        from laya_policy import DEFAULT_MODEL, LayaPolicy

        policy = LayaPolicy.load(args.model or DEFAULT_MODEL, args.subfolder, args.device, args.mask_fatal)
        print("laya on %s, mask_fatal=%s" % (policy.device, args.mask_fatal))

        def decide(obs):
            nonlocal agree, total
            d = policy.decide(obs)
            if not d.forced:
                latencies.append(d.latency_ms)
            agree += d.direction == greedy_policy(obs)
            total += 1
            return d.direction
    elif args.policy == "random":
        decide = random_policy(random.Random(args.seed))
    else:
        decide = greedy_policy

    results = []
    avoidable = 0  # deaths where a move that survived was available
    start = time.time()
    for i in range(args.games):
        sim = SnakeSim(seed=args.seed + i)
        idle = 0
        while sim.alive and idle < args.max_idle:
            obs = Observation.from_json(sim.observation())
            sim.set_direction(decide(obs))
            _, ate = sim.tick()
            idle = 0 if ate else idle + 1
        if not sim.alive and any(not m.fatal for m in analyse(obs)):
            avoidable += 1
        end = sim.death_cause or "loop"
        results.append((sim.apples_collected, sim.steps, end))
        print("game %3d: %3d apples  %4d steps  ended by %s" % (i + 1, sim.apples_collected, sim.steps, end),
              flush=True)

    apples = [r[0] for r in results]
    ends = {c: sum(1 for r in results if r[2] == c) for c in ("wall", "body", "loop")}
    print("\n%s over %d games: mean %.1f apples, median %.1f, best %d, worst %d"
          % (args.policy, len(apples), statistics.mean(apples), statistics.median(apples), max(apples), min(apples)))
    print("ended by: wall %(wall)d, body %(body)d, loop %(loop)d" % ends)
    print("avoidable deaths (a surviving move existed): %d" % avoidable)
    if total:
        print("agreed with greedy on %.1f%% of %d moves; median latency %.1f ms"
              % (100 * agree / total, total, statistics.median(latencies) if latencies else 0))
        print("Laya decided %.1f%% of moves (the rest had only one option left)"
              % (100 * len(latencies) / total))
    print("wall time %.1fs" % (time.time() - start))


if __name__ == "__main__":
    main()
