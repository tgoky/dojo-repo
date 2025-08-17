# Project Architecture

## 1 Architecture Overview

![Dojo Framework Architecture](Dojo%20Example%20Arch%20-%20Dojo%20Framework%20Arch.jpg)

The diagram above illustrates the complete Dojo framework architecture, showing two distinct flows:

### 1.1 Developer Flow (Deployment)
```
Developer → Cairo Project → Sozo CLI → Katana Sequencer
```

### 1.2 Player Flow (Runtime)
```
Player → Unity UI → JSON-RPC → Katana Sequencer
            ↓
       GraphQL → Torii Indexer
```

The system is **hybrid**: Unity delivers instant client-side gameplay, while the Dojo world—served locally via the **Katana** sequencer and **Torii** indexer—maintains an auditable source of truth for scores and sessions.

---

## 2 Core Components & Flow

### 2.1 Development & Deployment Stack

| Component | Role | Interface |
|-----------|------|-----------|
| **Cairo Project** | Smart contracts (models, systems) | Built with `sozo build` |
| **Sozo CLI** | Development toolkit & deployment | Commands: `build`, `migrate`, `dev` |
| **Katana Sequencer** | Local blockchain node | Processes transactions, maintains state |
| **Torii Indexer** | GraphQL query engine | Indexes Katana state for efficient reads |

### 2.2 Runtime Player Experience

| Component | Role | Communication Protocol |
|-----------|------|------------------------|
| **Unity UI** | Game client & user interface | Player interaction layer |
| **JSON-RPC** | Transaction submission | Write operations to Katana |
| **GraphQL** | State queries & subscriptions | Read operations from Torii |
| **Katana ↔ Torii** | State synchronization | Internal blockchain indexing |

---

## 3 Unity Frontend Components

| Script | Role | Communication |
|--------|------|---------------|
| **WorldManager** | Connects to Torii/Katana, syncs entities | GraphQL subscriptions |
| **SynchronizationMaster** | Maps blockchain entities → Unity objects | Event-driven updates |
| **GameManager** | Game loop, calls Actions contract | JSON-RPC to Katana |
| **BurnerManager** | Creates temporary accounts for players | Account management |

---

## 4 Dojo Backend (Cairo)

### 4.1 Smart Contract Architecture

| Type | Example | Purpose | Accessed Via |
|------|---------|---------|--------------|
| **Model** | `PlayerScore`, `GameSession` | On-chain data tables | GraphQL queries |
| **System/Action** | `actions::hit_mole` | Game logic & state mutations | JSON-RPC calls |
| **Event** | `MoleHit`, `GameEnded` | Real-time notifications | GraphQL subscriptions |

### 4.2 Data Models (Cairo)

| Model | Keys | Important Fields | Purpose |
|-------|------|------------------|---------|
| **PlayerScore** | `player` | `score`, `high_score` | Persistent player statistics |
| **PlayerAction** | `id` | `position`, `mole_type`, `hit`, `points` | Individual game actions |
| **GameSession** | `player` | `remaining_time`, `active`, `score` | Active game state |

Events emitted: `GameStarted`, `MoleHit`, `MoleMissed`, `GameTimeUpdated`, `GameEnded`.

---

## 5 Integration Flow & Communication Patterns

### 5.1 Game Session Lifecycle

```
1. Player starts game
   Unity → JSON-RPC → Katana: start_game()
   
2. Katana creates GameSession → Torii indexes state
   
3. Unity subscribes to updates
   Unity ← GraphQL subscription ← Torii: GameSession state
   
4. Gameplay loop
   Unity → JSON-RPC → Katana: hit_mole(position, points)
   Katana updates models → Torii indexes → Unity receives updates
   
5. Game completion
   Unity → JSON-RPC → Katana: game_over(final_score)
   Final state persisted & indexed
```

### 5.2 Communication Protocol Details

```
Write Operations (Mutations):
Unity → JSON-RPC → Katana Sequencer → State Changes

Read Operations (Queries):
Unity → GraphQL → Torii Indexer → Indexed State

Real-time Updates:
Katana → Torii → GraphQL Subscription → Unity
```

---

## 6 State Synchronization Strategy

### 6.1 Data Flow Patterns

1. **Optimistic UI Updates**: GameManager updates local state immediately
2. **Authoritative Confirmation**: Blockchain state replaces optimistic state
3. **Real-time Sync**: GraphQL subscriptions push updates to Unity
4. **Initial Load**: `SynchronizeEntities()` fetches current state on scene load

### 6.2 Communication Channels

| Operation Type | Protocol | Endpoint | Purpose |
|----------------|----------|----------|---------|
| **Transactions** | JSON-RPC | Katana | State mutations, game actions |
| **Queries** | GraphQL | Torii | Current state, historical data |
| **Subscriptions** | GraphQL WebSocket | Torii | Real-time state updates |

---

## 7 Architectural Considerations

### 7.1 Design Principles
- **Separation of Concerns**: Unity handles UX/rendering; Dojo enforces game rules
- **Eventual Consistency**: Optimistic updates with authoritative confirmation
- **Auditability**: All game state mutations are verifiable on-chain
- **Scalability**: GraphQL subscriptions enable efficient real-time updates

### 7.2 Trade-offs & Limitations
- **Latency**: Network calls for each game action vs. pure client-side gameplay
- **Complexity**: Dual-state management (optimistic + authoritative)
- **Single-player Focus**: Current design assumes isolated game sessions

---

## 8 Future Architectural Considerations

1. **Multiplayer Extension**: Architecture would need session management for concurrent players
2. **Cross-chain State**: Consider implications for multi-chain deployment
3. **State Pruning**: Long-term storage strategy for historical game data
4. **Randomness**: Current client-side RNG may need server-side validation for competitive scenarios
