"""Laya sidecar for the Snake game.

The game starts this script when run with --laya and talks to it over stdin/stdout, one JSON
object per line:

    game  -> agent   {"type": "observe", "id": 7, "snake": [[7, 5], ...], "apples": [...], ...}
    agent -> game    {"type": "decision", "id": 7, "direction": "Up", "confidence": 0.97, ...}
    game  -> agent   {"type": "game_over", "run": 2, "apples": 31, "steps": 512, "length": 34}

Stdout carries only protocol messages; logs go to stderr, which the game leaves attached to
its console. Closing stdin ends the session and prints a summary.

It can also be run by hand to check the model loads:  python agent/laya_player.py < /dev/null
"""

import argparse
import json
import statistics
import sys
import time
import warnings

from board import Observation
from laya_policy import DEFAULT_MODEL, LayaPolicy


def log(msg: str):
    print("[laya] " + msg, file=sys.stderr, flush=True)


def main():
    ap = argparse.ArgumentParser(description="Laya agent for the Snake game (JSON lines on stdin/stdout).")
    ap.add_argument("--model", default=DEFAULT_MODEL, help="Hugging Face repo or local checkpoint dir")
    ap.add_argument("--subfolder", default=None, help='checkpoint inside the repo, e.g. "typed-decisions"')
    ap.add_argument("--device", default=None, help="cuda / cpu / mps (default: best available)")
    ap.add_argument("--mask-fatal", action="store_true",
                    help="drop moves that kill the snake before asking Laya")
    ap.add_argument("--verbose", action="store_true", help="log every decision")
    args = ap.parse_args()

    # Model loading prints download bars and warnings; keep them off the protocol stream.
    protocol = sys.stdout
    sys.stdout = sys.stderr
    warnings.filterwarnings("ignore")

    def send(msg: dict):
        protocol.write(json.dumps(msg) + "\n")
        protocol.flush()

    log("loading %s%s ..." % (args.model, "/" + args.subfolder if args.subfolder else ""))
    start = time.time()
    policy = LayaPolicy.load(args.model, args.subfolder, args.device, args.mask_fatal)
    log("ready on %s in %.1fs (mask_fatal=%s)" % (policy.device, time.time() - start, args.mask_fatal))
    send({"type": "ready", "model": args.model, "subfolder": args.subfolder,
          "device": policy.device, "mask_fatal": args.mask_fatal})

    runs = []
    latencies = []
    try:
        serve(policy, send, runs, latencies, args.verbose)
    except KeyboardInterrupt:  # Ctrl+C in the game's terminal reaches this process too
        pass

    if runs:
        apples = [r.get("apples", 0) for r in runs]
        log("session: %d games, mean %.1f apples, best %d" % (len(runs), statistics.mean(apples), max(apples)))
    if latencies:
        log("median decision latency %.1f ms over %d decisions" % (statistics.median(latencies), len(latencies)))


def serve(policy, send, runs, latencies, verbose):
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        msg = {}
        try:
            msg = json.loads(line)
            kind = msg.get("type")
            if kind == "observe":
                obs = Observation.from_json(msg)
                d = policy.decide(obs)
                if not d.forced:
                    latencies.append(d.latency_ms)
                send({"type": "decision", "id": msg.get("id"), "direction": d.direction,
                      "confidence": round(d.confidence, 4), "scores": d.scores,
                      "forced": d.forced, "latency_ms": round(d.latency_ms, 1)})
                if verbose:
                    apples = " ".join("%s@%d,%d" % (a.kind, *a.pos) for a in obs.apples)
                    log("game %d step %4s  head %d,%d  %s  -> %-5s p=%.2f %s%.0fms"
                        % (len(runs) + 1, msg.get("step"), *obs.head, apples, d.direction,
                           d.confidence, "(forced) " if d.forced else "", d.latency_ms))
            elif kind == "game_over":
                runs.append(msg)
                log("game %d over: %s apples, length %s, %s steps"
                    % (len(runs), msg.get("apples"), msg.get("length"), msg.get("steps")))
            else:
                send({"type": "error", "id": msg.get("id"), "message": "unknown message type %r" % kind})
        except Exception as e:  # keep the game running whatever goes wrong on one message
            log("error handling %r: %s" % (line[:200], e))
            send({"type": "error", "id": msg.get("id") if isinstance(msg, dict) else None, "message": str(e)})


if __name__ == "__main__":
    main()
