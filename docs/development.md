# Development Guide

Single go-to reference for contributing to **Whack-a-Mole Dojo** (Unity + Dojo).

---

## 1  Prerequisites

| Tool | Version |
|------|---------|
| Dojo (via `dojoup`) | v1.5.0 |
| Cairo | v2.10.1 |
| Rust | stable |
| Unity | 2022.3.15f1+ |
| Git | latest |

Install Dojo stack:
```bash
curl -L https://install.dojoengine.org | bash

dojoup use 1.5.0
```

---

## 2  Local Dev Workflow

### 2.1  Start Backend
```bash
# terminal 1
katana --disable-fee &

# terminal 2
cd backend
sozo dev          # build, migrate, index
```

### 2.2  Run Unity
1. Open `unity/` in Unity Hub.  
2. Configure `WorldManagerData` & `GameManagerData` for localhost (see
   `docs/unity.md`).  
3. Press **Play**.

### 2.3  Code-Change Cycle

| Step | Backend | Unity |
|------|---------|-------|
| Update models / systems | Edit Cairo, `sozo build` | – |
| Redeploy | `sozo migrate` | – |
| Regenerate bindings | `sozo build --unity --bindings-output ../unity/...` | Refresh scripts |
| Gameplay logic | – | Modify C# scripts |
| Test | `scarb test` | Play mode |

---

## 3  Testing

* **Backend**: `scarb test` for unit; `sozo execute` for manual calls.  
* **Frontend**: play-mode tests; ensure blockchain state reflects UI.

---

## 4  Git Workflow

1. Feature branch per change.  
2. Commit early & often with descriptive messages.  
3. Pull request → CI runs `scarb test` + Unity compile check.  
4. Review & squash merge.

---

## 5  Deployment Pipeline

### 5.1  Testnet
```bash
sozo migrate --network testnet
# regen bindings, configure Unity
```
Build Unity *Development* build and verify.

### 5.2  Production Rollup
```bash
sozo migrate --network mainnet  # or sovereign RPC
```
Grant write perms to `actions`. Update Unity configs.

---

## 6  Troubleshooting

| Area | Symptom | Fix |
|------|---------|-----|
| Backend compile | Cairo errors | `dojoup use 1.5.0`, run `sozo clean` |
| Unity connect | `Failed to connect` | Katana running? RPC correct? CORS? |
| Binding mismatch | C# field missing | Regenerate bindings, ensure `pub` fields |

---

## 7  Open Items ⚠️

1. Automate binding copy in a post-build script.  
2. Add CI lint for Unity scripts (currently only Cairo tests run).

