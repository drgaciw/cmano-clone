#!/usr/bin/env python3
"""Production stress-axis proof gate (DRG-63 / S110-02).

Thin CLI alias over ``verify_stress_axes.run_gate`` so the gauntlet tooling path
has an obvious production entrypoint name. Prefer this module (or the shell
wrapper ``run-stress-proof-gate.sh``) when wiring runbooks and CI; library
callers may import ``verify_axis`` / ``verify_axes`` directly.

Exit codes match ``verify_stress_axes.main``:
  0 — gate pass (config-only unproven axes allowed)
  1 — non-config-only axis unproven
  2 — usage / I/O error
"""

from __future__ import annotations

import sys

from verify_stress_axes import (  # re-export for discoverability
    CONFIG_ONLY_PROOF,
    is_hard_fail_result,
    load_evidence,
    main as _verify_main,
    run_gate,
    verify_axes,
    verify_axis,
)

__all__ = [
    "CONFIG_ONLY_PROOF",
    "is_hard_fail_result",
    "load_evidence",
    "main",
    "run_gate",
    "verify_axis",
    "verify_axes",
]


def main(argv: list[str] | None = None) -> int:
    """Delegate to verify_stress_axes.main (identical CLI flags)."""
    return _verify_main(argv)


if __name__ == "__main__":
    sys.exit(main())
