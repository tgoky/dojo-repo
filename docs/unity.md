# Unity Frontend

## 1  Quick Start

1. Install **Unity 2022.3.15f1** or later and **Git**.
2. Clone the repo and open `unity/` (a.k.a. *unity-game*) from Unity Hub.
3. Ensure packages:
   - **Newtonsoft.Json** – `com.unity.nuget.newtonsoft-json`
   - **Dojo Unity SDK v1.5.0** – import the
     [`dojo.unitypackage`](https://github.com/dojoengine/dojo.unity/releases/tag/v1.5.0)
4. Create / edit the two configuration assets (see § 2.1).
5. Generate C# bindings from the backend:
   ```bash
   cd backend
   sozo build --unity --bindings-output ../unity/Assets/Dojo/Runtime/bindings
   ```
6. Play the *Main* scene.  Blockchain traffic uses localhost by default.

---

## 2  Project Structure

```
unity/
├── Assets/
│   ├── Dojo/            # SDK + generated bindings
│   ├── Prefabs/
│   ├── Scenes/Main.unity
│   ├── Scripts/
│   │   ├── GameManager.cs
│   │   ├── Mole.cs
│   │   ├── UI/
│   │   └── Data/GameManagerData.cs
│   └── ...
└── ProjectSettings/
```

### 2.1  Configuration ScriptableObjects

| Asset | Purpose | Location |
|-------|---------|----------|
| **WorldManagerData** | Endpoint & world settings for Dojo connection | `Assets/Dojo/Runtime/Config` |
| **GameManagerData** | RPC, master account & contract addresses | `Assets/Scripts` |

#### Required Fields

```csharp
// WorldManagerData (simplified)
public string toriiUrl = "http://localhost:8080";
public string relayUrl = "/ip4/127.0.0.1/tcp/9090";
public string relayWebrtcUrl;           // WebGL only
public FieldElement worldAddress;
```
```csharp
// GameManagerData (simplified)
public string rpcUrl = "http://localhost:5050";
public string masterAccountAddress = "";
public string masterAccountPrivateKey = "";   // ⚠️ dev-only
public string actionsContractAddress = "";
```
⚠️ *Never ship private keys in client builds; use a wallet provider in production.*

---

## 3  Blockchain Integration

### 3.1  WorldManager Flow

`WorldManager` ( `Assets/Dojo/Runtime/WorldManager.cs` ) boots the connection and handles
entity sync:

```csharp
async void Awake() {
#if UNITY_WEBGL && !UNITY_EDITOR
    var client = new ToriiWasmClient(cfg.toriiUrl, cfg.relayWebrtcUrl, cfg.worldAddress);
    await client.CreateClient();
#else
    var client = new ToriiClient(cfg.toriiUrl, cfg.relayUrl, cfg.worldAddress);
#endif
    await syncMaster.SynchronizeEntities();
    syncMaster.RegisterEntityCallbacks();
}
```

Key helpers: `SynchronizationMaster`, `UnityMainThreadDispatcher`.

### 3.2  Entity Synchronization & Access

```csharp
GameObject e  = worldManager.Entity("player_key");
var scoreComp = e.GetComponent<PlayerScore>();
uint current  = scoreComp.score;
```

### 3.3  Executing Actions

`Actions` (generated from Cairo `actions` system) exposes strongly typed methods:

```csharp
await actions.start_game(account);
await actions.hit_mole(account, (uint)points);
await actions.update_frame(account, (uint)(msRemaining));
await actions.game_over(account, (uint)score, (byte)reason);
```

### 3.4  WebGL vs Desktop

WebGL builds swap the transport layer for WebRTC (see compile-time branch above). Ensure
`relayWebrtcUrl` is populated for your relay server.

---

## 4  Gameplay Components

| Component | Responsibilities |
|-----------|------------------|
| **GameManager** | Game loop, timer, score, spawning moles, blockchain I/O |
| **Mole** | Animations, hit detection, type-based scoring |
| **UIController** | Score/time HUD, buttons, game-over screen |
| **BurnerManager** | Creates temporary burner wallets for players |

### Interaction Timeline

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐         ┌─────────────┐
│             │         │              │         │             │         │             │
│  UI         │         │ GameManager  │         │  Actions    │         │ Dojo Backend│
│             │         │              │         │             │         │             │
└──────┬──────┘         └──────┬───────┘         └──────┬──────┘         └──────┬──────┘
       │                       │                        │                       │
       │  Start Button         │                        │                       │
       ├──────────────────────►│                        │                       │
       │                       │  Reset Game State      │                       │
       │                       ├──────────────────┐     │                       │
       │                       │                  │     │                       │
       │                       │◄─────────────────┘     │                       │
       │                       │      start_game()      │                       │
       │                       ├─────────────────────────►                      │
       │                       │                        │    Blockchain TX      │
       │                       │                        ├──────────────────────►│
       │                       │                        │                       │
       │                       │                       │
       │                       │◄──────────────────────┤
       │                       │                       │
       │    Update UI          │                       │
       │◄──────────────────────┤                       │
       │                       │                       │
```

### Mole Hit Sequence

```
┌─────────────┐         ┌──────────────┐        ┌─────────────┐
│             │         │              │        │             │
│   Mole      │         │ GameManager  │        │WorldManager │
│             │         │              │        │             │
└──────┬──────┘         └──────┬───────┘        └──────┬──────┘
       │                       │                       │
       │  Player Hit           │                       │
       ├──────────────────────►│                       │
       │                       │    hit_mole()         │
       │                       ├──────────────────────►│
       │                       │                       │
       │                       │◄──────────────────────┤
       │                       │                       │
       │    Play Hit           │                       │
       │◄──────────────────────┤                       │
       │    Animation          │                       │
       │                       │                       │
```

---

## 5  Building & Deployment

Desktop: use *File ▸ Build Settings* and choose PC/Mac.

WebGL:
1. Install WebGL module.
2. Player Settings → Publishing Settings → set *Compression* to *Disabled* (IPFS-friendly).
3. Ensure relay/WebRTC endpoints reachable via HTTPS.

---

## 6  Troubleshooting

| Symptom | Fix |
|---------|-----|
| Connection timeout | Backend running? `toriiUrl` correct? Firewall? |
| Missing `Newtonsoft.Json` | Re-add via Package Manager |
| Bindings won’t compile | Re-run `sozo build --unity`; ensure all Cairo models use `public` fields |
| WebGL stalls on connect | Check `relayWebrtcUrl` & browser console |

---

## 7  FAQ

**Q.** *Why do I see “Invalid serialization length” errors?*  
**A.** Generated bindings may be out of sync with Cairo models – rebuild.

**Q.** *Can I use a different Starknet RPC?*  
**A.** Yes, change `GameManagerData.rpcUrl` and ensure CORS is allowed.

---

## 8  Open Questions ⚠️

1. The `BurnerManager` workflow is inferred from code but not documented upstream; verify that it still matches the latest Dojo Unity SDK.
2. The list of supported mole types (standard, hardhat, bomb) is based on current gameplay scripts – confirm with design team if adding new types changes scoring contract logic.

