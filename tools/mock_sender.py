#!/usr/bin/env python3
"""iPhoneなしでUnity側を試すための疑似送信機．

iOS側と同じ JSON {"p", "seq", "t", "s"} をUDPで送る．標準ライブラリのみ．

例:
    python3 mock_sender.py                    # 東西が交互にランダムな強さで叩く
    python3 mock_sender.py --player 1 --rate 4
"""

import argparse
import json
import random
import socket
import time


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=9000)
    ap.add_argument("--player", type=int, choices=[0, 1, 2], default=0, help="0 なら東西両方")
    ap.add_argument("--rate", type=float, default=3.0, help="1人あたりの1秒間のトン回数")
    ap.add_argument("--duration", type=float, default=30.0, help="送信する秒数")
    args = ap.parse_args()

    players = [1, 2] if args.player == 0 else [args.player]
    seq = {1: 0, 2: 0}
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    start = time.monotonic()
    next_at = {p: start + random.uniform(0, 1 / args.rate) for p in players}

    while time.monotonic() - start < args.duration:
        now = time.monotonic()
        for p in players:
            if now >= next_at[p]:
                seq[p] += 1
                event = {"p": p, "seq": seq[p], "t": now, "s": round(random.uniform(0.3, 1.0), 2)}
                sock.sendto(json.dumps(event).encode(), (args.host, args.port))
                print(event)
                # 間隔に揺らぎを入れて人間のリズムに近づける
                next_at[p] = now + random.uniform(0.6, 1.4) / args.rate
        time.sleep(0.005)


if __name__ == "__main__":
    main()
