# Backend (Dojo)

## 1  Overview

* **Language / Framework**: Cairo 2 + Dojo v1.5.0
* **Goal**: Provide verifiable game state for Unity client.
* **Deployment targets**:  
  • Local devnet (Katana)  
  • Sovereign rollup on Celestia (RPC-compatible sequencer)

Key components: models, `actions` system, events, migration scripts.

---

## 2  Setup (Local Development)

```bash
# install toolchain
curl -L https://install.dojoengine.org | bash          # installs dojoup

dojoup use 1.5.0                                       # cairo, scarb, sozo, katana, torii

git clone <repo>
cd whack-a-mole-dojo/backend

# fetch deps & compile
scarb fetch
sozo build

# one-shot dev node + deploy + indexer
sozo dev   # ⏳ keep terminal open
```

*Generated bindings*: after any contract change run

```bash
sozo build --unity --bindings-output ../unity/Assets/Dojo/Runtime/bindings
```

---

## 3  Data Models

| Model | Purpose |
|-------|---------|
| `GameSession` | Active session per player – timer, score, active flag |
| `PlayerScore` | Persistent score & high-score per player |
| `PlayerAction` | Historical hit/miss events (optional for analytics) |

```cairo
#[dojo::model] struct PlayerScore {
    #[key] player: ContractAddress,
    score: u32,
    high_score: u32,
}
```

Events emitted mirror gameplay milestones (`GameStarted`, `MoleHit`, etc.).

---

## 4  `actions` System

`actions.cairo` implements all game logic through the `IActions` interface:

| Function | Triggered by | Core Logic |
|----------|--------------|------------|
| `start_game()` | UI Start button | init `GameSession`, reset score |
| `hit_mole(points)` | Mole hit | add points, +1 s time, emit `MoleHit` |
| `miss_mole(is_mole)` | Missed click | −2 s penalty if mole present |
| `update_frame(remaining)` | each frame | sync timer, auto end if 0 |
| `game_over(score, reason)` | Timer 0 or bomb | finalise session, update high-score |

Implementation safeguards:

1. Re-entrancy prevented by per-player session flag.
2. Time arithmetic uses `u32` ms; block timestamp cross-checked for abuse ⚠️ (needs audit).

---

## 5  Deployment

### 5.1  Local Devnet (quick)

```bash
katana --disable-fee &   # optional separate
sozo dev                 # rebuilds on file change
```

### 5.2  Docker Compose (isolated)

```bash
cd backend
docker compose up -d      # katana + torii + sozo migrate
```

### 5.3  Sovereign Rollup (prod)

```bash
export SOZO_RPC_URL=<ROLLUP_RPC>
export SOZO_ACCOUNT_ADDRESS=<DEPLOYER>
export SOZO_PRIVATE_KEY=<PRIVKEY>

sozo build
sozo migrate              # deploys world & systems

# grant write perms
sozo auth grant --world <WORLD_ADDR> --wait \
  writer GameSession,dojo_starter-actions
sozo auth grant --world <WORLD_ADDR> --wait \
  writer PlayerScore,dojo_starter-actions
```

---

## 6  API Reference (high-level)

```
IActions
  start_game()
  hit_mole(u32 points)
  miss_mole(u8 is_mole)
  update_frame(u32 remaining_ms)
  game_over(u32 score, u8 reason)
```

Generated JSON-RPC wrapper emitted by Dojo provides ABI for external callers.

---

## 7  Testing

```bash
scarb test   # Cairo unit tests (./tests)
```

> ⚠️  No test coverage currently for `miss_mole` edge-cases. Add fuzz tests.

---

## 8  Troubleshooting

| Issue | Fix |
|-------|-----|
| Compile errors | `dojoup use 1.5.0`; run `sozo clean` & rebuild |
| Unity cannot connect | Katana running? RPC correct? CORS? |
| Permissions error on write | Re-run `sozo auth grant` with correct world address |

---

## 9  Open Questions ⚠️

1. **Gas Cost under spam** – high `hit_mole` frequency may DoS; consider server-side rate limit.
2. **PlayerAction pruning** – storage growth unbounded; need archival strategy.
