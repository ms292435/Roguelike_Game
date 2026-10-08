# Roguelike Benchmark: Classic OOP vs. Unity DOTS/ECS

[![Unity Version](https://img.shields.io/badge/Unity-6000.4.0f1-black?logo=unity)](https://unity.com/)
[![DOTS / Entities](https://img.shields.io/badge/Unity%20DOTS-Entities%201.3+-blue?logo=unity)](https://unity.com/dots)
[![Burst Compiler](https://img.shields.io/badge/Burst-Compiled-green)](https://docs.unity3d.com/Packages/com.unity.burst@latest)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Facade%20Pattern-orange)](#phase-2-dotsecs-hybrid-architecture)

A high-performance technical benchmark and portfolio project developed in **Unity 6**. The primary objective is to scientifically explore, measure, and analyze the architectural and hardware-level performance boundaries between **Classic Object-Oriented Programming (MonoBehaviour / OOP)** and **Data-Oriented Technology Stack (DOTS / ECS)** in a Survivor-like game context.

![In-Game Horde Benchmark](Documentation/images/gameplay_horde.png)

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Phase 1: Classic OOP Architecture & Profiling](#phase-1-classic-oop-architecture-profiling)
   - [OOP UML Class Diagram](#oop-uml-class-diagram)
   - [Key Architectural Patterns in OOP](#key-architectural-patterns-in-oop)
   - [Profiling & Identifying the Bottleneck](#profiling-identifying-the-bottleneck)
   - [OOP Benchmark Results](#oop-benchmark-results)
3. [Phase 2: DOTS/ECS & Hybrid Architecture](#phase-2-dotsecs-hybrid-architecture)
   - [Data-Oriented Pipeline & Hybrid Bridge Diagram](#data-oriented-pipeline-hybrid-bridge-diagram)
   - [Data-Oriented Design (DOD) Principles](#data-oriented-design-dod-principles)
   - [Core Systems Overview](#core-systems-overview)
   - [Decoupling with the EnemyBridge Facade](#decoupling-with-the-enemybridge-facade)
4. [Performance Comparison & Hardware Analysis](#performance-comparison-hardware-analysis)
5. [Engineering Standards & Best Practices](#engineering-standards-best-practices)
6. [Controls & In-Game Benchmark HUD](#controls-in-game-benchmark-hud)
7. [Tech Stack](#tech-stack)

---

<a id="project-overview"></a>
## Project Overview

In survivor-like horde games, rendering and updating tens of thousands of dynamic agents simultaneously is one of the most demanding challenges for game engines. 

This project implements the same game mechanics twice:
1. **First Implementation (OOP)**: Pushed to the theoretical limits of traditional Unity `MonoBehaviour` development using best-practice optimizations (Object Pooling, 2D Spatial Hash Grid, Time-Sliced Boids Steering).
2. **Second Implementation (DOTS/ECS)**: Re-engineered from the ground up using **Entities**, **C# Job System**, and the **Burst Compiler**, backed by GPU-driven batched rendering via `BatchRendererGroup`.

The project features a **real-time in-game benchmark controller** allowing runtime horde adjustments up to **1,000,000 entities** without code recompilation.

---

<a id="phase-1-classic-oop-architecture-profiling"></a>
## Phase 1: Classic OOP Architecture & Profiling

<a id="oop-uml-class-diagram"></a>
### OOP UML Class Diagram

The classic OOP implementation was architected around modular, decoupled subsystems with strict memory pooling:

![OOP UML Class Diagram](Documentation/images/oop_class_diagram.png)

<a id="key-architectural-patterns-in-oop"></a>
### Key Architectural Patterns in OOP

```mermaid
classDiagram
    direction TB

    %% Observer Pattern
    class ISubject {
        <<interface>>
        +AddObserver(IObserver)
        +RemoveObserver(IObserver)
        +Notify(string, object)
    }
    class IObserver {
        <<interface>>
        +OnNotify(ISubject, string, object)
    }
    class Player {
        +mCurrentPosition: Vector3
        +AddObserver(IObserver)
        +Notify()
    }
    class ExperienceBar {
        +OnNotify()
    }
    class LevelUpManager {
        +OnNotify()
    }

    ISubject <|.. Player
    IObserver <|.. ExperienceBar
    IObserver <|.. LevelUpManager
    Player --> IObserver : notifies

    %% Strategy Pattern (Weapons)
    class IWeapon {
        <<interface>>
        +Attack()
        +UpdateWeapon(float)
        +Initialize(WeaponUpgradeData)
    }
    class FireWand {
        +Attack()
    }
    class Sword {
        +Attack()
    }
    class WeaponManager {
        +UpdateWeapons()
    }

    IWeapon <|.. FireWand
    IWeapon <|.. Sword
    WeaponManager --> IWeapon : executes

    %% Spatial Partitioning & Object Pools
    class SpatialGrid {
        +AddEnemy(Enemy)
        +GetEnemiesInRadius(Vector3, float)
    }
    class EnemyPool {
        +GetEnemy()
        +ReturnEnemy(Enemy)
    }
    class Enemy {
        +Tick()
        +HandleSeparation()
    }

    EnemyPool --> Enemy : manages
    SpatialGrid --> Enemy : indexes
```

1. **Observer Pattern**: `Player` acts as `ISubject` notifying decoupled UI systems (`ExperienceBar`, `LevelUpManager`) without direct dependencies.
2. **Strategy Pattern for Weapons**: `WeaponManager` updates polymorphic weapons through the `IWeapon` interface. Weapon stats are data-driven via `ScriptableObject` assets (`WeaponUpgradeData`), enabling frictionless content expansion.
3. **Spatial Hash Grid**: Reduces collision and neighbor search complexity from $\mathcal{O}(n^2)$ to $\mathcal{O}(n)$ by mapping agent coordinates into discrete 2D hash cells: each query only scans the neighboring cells instead of every enemy.
4. **Strict Object Pooling**: Pre-allocates enemies, damage text, projectiles, and experience gems, reducing runtime allocations on the critical path to **0 – 1 KB per frame**.
5. **Boids-Inspired Horde Steering**: Blended vector steering combining *Seek* (player attraction) with a smoothed, time-sliced *Separation* vector (`Vector3.Lerp`) to prevent unnatural teleportation or overlapping.

---

<a id="profiling-identifying-the-bottleneck"></a>
### Profiling & Identifying the Bottleneck

Profiling with the Unity Profiler identified the primary bottleneck inside `EnemyPool.Update() -> Enemy.Tick()`:

![Profiler EnemyPool Bottleneck](Documentation/images/profiler_enemy_pool.png)

Further deep-sampling of `Enemy.Tick()` revealed that the steering calculations (`1_Enemy_Maths_Boids`) accounted for more than **75% of the frame CPU time**:

![Profiler Boids Sample](Documentation/images/profiler_boids_sample.png)

Even with spatial grid lookups, neighbor caching, and 4-frame time-sliced separation updates, executing horde steering on a single thread hit a hard technical ceiling.

---

<a id="oop-benchmark-results"></a>
### OOP Benchmark Results

| Enemy Count | Frame Update Time (ms) | Target Frame Rate | Status / Playability |
|:---:|:---:|:---:|:---|
| **2,000** | 4.21 ms | > 144 FPS | Extremely smooth, perfectly stable |
| **4,000** | 8.94 ms | ~120 FPS | Minor drops below monitor refresh rate |
| **6,000** | 19.64 ms | ~60 FPS | Noticeable frame time increase; playable |
| **8,000** | 35.40 ms | ~30 FPS | Degraded responsiveness; functional limit |
| **10,000** | **45.59 ms** | **< 20 FPS** | **Unplayable (Hardware Ceiling Reached)** |

> **OOP Conclusion**: The playable limit of the OOP implementation is **~8,000 entities** (~30 FPS); at 10,000 the game drops below 20 FPS. The root cause is not algorithmic inefficiency, but **hardware memory hierarchy**. OOP stores `GameObject` and `MonoBehaviour` instances as individual reference types scattered throughout the RAM. The CPU is constantly stalled by **Cache Misses** (pointer chasing), starving execution pipelines.

---

<a id="phase-2-dotsecs-hybrid-architecture"></a>
## Phase 2: DOTS/ECS & Hybrid Architecture

<a id="data-oriented-pipeline-hybrid-bridge-diagram"></a>
### Data-Oriented Pipeline & Hybrid Bridge Diagram

In DOTS, architecture shifts from class hierarchies to a **Data-Oriented Pipeline**. The high-level MonoBehaviour gameplay domain interfaces with the unmanaged ECS domain through an explicit **Facade / Bridge**:

```mermaid
flowchart TD
    subgraph AuthoringLayer ["1. Subscene Authoring & Baking"]
        EA["EnemyAuthoring"] -->|Baker| EC["Entity + EnemySpeedComponent"]
        ESA["EnemySpawnerAuthoring"] -->|Baker| ED["Spawner Entity + EnemySpawnerData"]
    end

    subgraph SimulationPipeline ["2. DOTS Simulation Pipeline (SimulationSystemGroup)"]
        direction TB
        EHS["<b>EnemyHashSystem</b><br/><i>[UpdateBefore EnemyMovementSystem]</i><br/>NativeParallelMultiHashMap Spatial Partitioning"]
        EMS["<b>EnemyMovementSystem</b><br/><i>Parallel Burst Job (IJobEntity)</i><br/>Multithreaded Horde Movement"]
        ECS["<b>EnemyCounterSystem</b><br/><i>[UpdateAfter EnemyMovementSystem]</i><br/>O(chunks) Total Count + Burst Culling Job"]
        ESS["<b>EnemySpawnerSystem</b><br/>GPU BatchRendererGroup Instantiation"]
        EXS["<b>ExperienceSpawnSystem / CollectSystem</b><br/>Entity Command Buffer (ECB) Lifecycle"]

        EHS -->|Spatial Grid Ready| EMS
        EMS -->|Updated Transforms| ECS
        ECS --> ESS
        ESS --> EXS
    end

    subgraph HybridFacade ["3. Architectural Bridge (Facade Pattern)"]
        EB["<b>EnemyBridge</b> (Static Facade)<br/>• TryGetClosestEnemy()<br/>• DealDamage()<br/>• CheckCollision()"]
        BC["<b>BenchmarkController</b><br/>Runtime Horde Target (0 - 1M)"]
    end

    subgraph GameplayLayer ["4. Gameplay Domain (MonoBehaviour / OOP)"]
        PL["Player"]
        FW["FireWand (IWeapon)"]
        SW["Sword (IWeapon)"]
        PR["Projectile"]
        UI["Benchmark HUD & EnemyCounterUI"]
    end

    BC -->|"Sets Target Count"| ESS
    UI -->|"Reads Total & Visible"| ECS
    FW -->|"TryGetClosestEnemy()"| EB
    SW -->|"DealDamage()"| EB
    PR -->|"Single-Pass DealDamage()"| EB

    EB -->|"Reads Spatial Grid"| EHS
    EB -->|"Queues XP Tag & Destroy via ECB"| SimulationPipeline
```

---

<a id="data-oriented-design-dod-principles"></a>
### Data-Oriented Design (DOD) Principles

To break past the 10,000 entity ceiling, the simulation was converted to **Data-Oriented Design (DOD)**:

```
Traditional OOP (Array of Structures - Scattered Pointers):
[ Heap Object 1: Transform | Health | Speed | Sprite ] -> Pointer -> [ Heap Object 2 ] (Cache Misses)

DOTS / ECS (Structure of Arrays - 16KB Archetype Chunks):
Chunk 1: [ Transform | Transform | Transform ... ]  <-- Linear Cache Line Prefetching
Chunk 2: [ Speed     | Speed     | Speed     ... ]  <-- Burst-compiled, multithreaded iteration
```

- **Archetype Chunks**: Entities with identical component configurations are packed contiguously into memory chunks of 16 KB.
- **Cache Locality**: Iterating over linear arrays maximizes L1/L2 cache prefetching, minimizing RAM latency.
- **Burst Compiler**: Compiles C# jobs into highly optimized native code (LLVM), spread across all worker threads by the C# Job System.

---

<a id="core-systems-overview"></a>
### Core Systems Overview

1. [`EnemyMovementSystem`](Assets/Scripts/Enemy/EnemyMovementSystem.cs):
   - Burst-compiled parallel job (`IJobEntity`) computing directional vectors towards the player.
   - Zero heap allocation, fully multithreaded across all available CPU worker threads.
2. [`EnemyHashSystem`](Assets/Scripts/Enemy/EnemyHashSystem.cs):
   - Partitions active enemies into a `NativeParallelMultiHashMap<int2, EnemyGridData>`.
   - Dynamic capacity scaling with hysteresis to prevent frequent native reallocations.
3. [`EnemySpawnerSystem`](Assets/Scripts/Enemy/EnemySpawnerSystem.cs):
   - Uses `BatchRendererGroup` / `EntitiesGraphicsSystem` for GPU-instanced rendering.
   - Spawns enemies dynamically along camera edges in configurable batch sizes up to target horde limits.
4. [`EnemyCounterSystem`](Assets/Scripts/UI/EnemyCounterSystem.cs):
   - Queries exact entity count instantly in $\mathcal{O}(\text{chunks})$ directly from archetype metadata (`mEnemyQuery.CalculateEntityCount()`), introducing zero frame delay and no thread contention.
   - Runs a Burst-compiled culling job (`CountVisibleEnemiesJob`) to report on-screen entity density.
5. [`ExperienceSpawnSystem`](Assets/Scripts/Experience/ExperienceSpawnerSystem.cs) & [`ExperienceCollectionSystem`](Assets/Scripts/Experience/ExperienceMovementSystem.cs):
   - Handles XP orb instantiation and magnet attraction towards the player through decoupled Entity Command Buffers (ECB).

---

<a id="decoupling-with-the-enemybridge-facade"></a>
### Decoupling with the EnemyBridge Facade

The [`EnemyBridge.cs`](Assets/Scripts/Enemy/EnemyBridge.cs) facade completely decouples gameplay logic from unmanaged ECS:
- **No Leaky Abstractions**: Gameplay scripts (`FireWand`, `Projectile`, `Sword`) never import `Unity.Entities` or touch `World.DefaultGameObjectInjectionWorld`.
- **Encapsulated Grid Metrics**: Spatial cell size (`CellSize`) is declared `internal` to the simulation domain. High-level weapons simply query `EnemyBridge.TryGetClosestEnemy(origin, range, out target)`.
- **Single-Pass Projectile Collision**: Instead of performing multiple spatial queries per projectile per frame, `EnemyBridge.DealDamage` applies damage and returns the hit count in a single traversal, instead of a separate collision check followed by a damage query.
- **Safe Enemy Destruction**: Command buffers are only played back at the end of the frame, so an enemy hit twice in the same frame could be destroyed twice (duplicate XP orbs, playback exception). Each enemy carries an enableable `EnemyDeadTag`, switched on as soon as its destruction is queued, so weapons, player contact and the spawner all skip enemies that are already dying.

---

<a id="performance-comparison-hardware-analysis"></a>
## Performance Comparison & Hardware Analysis

### Methodology

- **Build**: development build connected to the Unity Profiler (no Editor overhead).
- **Simulation metric**: OOP measures `EnemyPool.Update()`; DOTS measures `SimulationSystemGroup` on the main thread. Since DOTS jobs run on worker threads, this includes scheduling the jobs *and waiting for them*, plus the XP orbs and the spawner: a slightly pessimistic but fair equivalent.
- **Frame metric**: `PlayerLoop`, the whole CPU frame (simulation, rendering preparation, UI).
- **Protocol**: values from a representative frame once the horde size is stable. The GPU time was not captured, so FPS values are CPU-bound estimates (`1000 / PlayerLoop`).

### DOTS / ECS Results

| Enemies | Simulation (ms) | Time per Enemy (µs) | Full CPU Frame (ms) | CPU-bound FPS |
|:---:|:---:|:---:|:---:|:---:|
| 2,000 | 0.24 | 0.120 | 2.73 | ~366 |
| 4,000 | 0.29 | 0.073 | 2.62 | ~382 |
| 6,000 | 0.31 | 0.052 | 2.63 | ~380 |
| 8,000 | 0.41 | 0.051 | 3.04 | ~329 |
| 10,000 | 0.50 | 0.050 | 2.78 | ~360 |
| 50,000 | 1.92 | 0.038 | 6.83 | ~146 |
| 100,000 | 4.14 | 0.041 | 9.88 | ~101 |
| **250,000** | **9.08** | **0.036** | **22.93** | **~44** |
| 500,000 | 19.02 | 0.038 | 43.00 | ~23 |
| 1,000,000 | 40.57 | 0.041 | 88.62 | ~11 |

Up to 10,000 enemies the cost is dominated by the fixed overhead of the ECS pipeline (system updates, job scheduling, synchronization). From 50,000 onwards, the simulation scales linearly at **~0.04 µs per enemy**.

### OOP vs. DOTS / ECS

| Enemies | OOP `EnemyPool.Update()` | DOTS `SimulationSystemGroup` | Speed-up |
|:---:|:---:|:---:|:---:|
| 2,000 | 4.21 ms | 0.24 ms | ×17.5 |
| 4,000 | 8.94 ms | 0.29 ms | ×30.8 |
| 6,000 | 19.64 ms | 0.31 ms | ×63.4 |
| 8,000 | 35.40 ms | 0.41 ms | ×86.3 |
| 10,000 | 45.59 ms | 0.50 ms | **×91.2** |

| Metric | Classic OOP (MonoBehaviour) | Unity DOTS / ECS | Improvement |
|:---|:---:|:---:|:---:|
| **Max Playable Entities (≥ 30 FPS)** | ~8,000 | **~250,000** (~44 FPS) | **~30×** |
| **Highest Count Tested** | 10,000 (< 20 FPS) | 1,000,000 (~11 FPS) | 100× |
| **Simulation Cost per Enemy** | ~4.6 µs (10k) | **~0.04 µs** | **~110×** |
| **Full CPU Frame @ 10,000 Entities** | 54.63 ms | **2.78 ms** | **~20×** |
| **Simulation Allocations per Frame** | 0 – 1 KB | **0 B** (102 B above 50k) | Negligible GC pressure |
| **Thread Utilization** | Main thread only | All job worker threads | Parallel |
| **Memory Layout** | Heap objects (scattered pointers) | Contiguous archetype chunks (SoA) | Far fewer cache misses |

> Simulating **1,000,000** enemies with DOTS (40.57 ms) takes less time than simulating **10,000** with OOP (45.59 ms).

### Where the New Bottleneck Is

- **The simulation is no longer the whole story**: with a large horde it only represents ~40–45% of the CPU frame. The rest goes to rendering (Entities Graphics GPU uploads) and engine overhead, which explains why the full-frame gain (~20×) is smaller than the simulation gain (~91×).
- **Spawning**: new enemies are instantiated on the main thread in batches of up to 25,000 per frame, which causes visible spikes when the target count is raised. Moving it into a Burst job is the next optimization.
- **Garbage collection**: the ~9 KB allocated per frame on the `PlayerLoop` come from `PostLateUpdate.FinishFrameRendering` (render pipeline), not from the ECS simulation.
- **Not isolated here**: the gain combines contiguous memory, Burst compilation and multithreading, and the rendering path also differs (`SpriteRenderer` vs. Entities Graphics).

---

<a id="engineering-standards-best-practices"></a>
## Engineering Standards & Best Practices

The codebase follows strict software engineering and C# conventions:

- **Naming Conventions**:
  - `m` prefix for private/protected class fields (`mCurrentPosition`, `mSpatialGrid`).
  - `p` prefix for method and constructor parameters (`pDeltaTime`, `pTargetPosition`).
  - `l` prefix for local variables (`lPlayerPos`, `lCellRange`).
- **Memory Safety**:
  - All native containers (`NativeArray`, `NativeParallelMultiHashMap`, `NativeReference`) are tracked and properly disposed in `OnDestroy()`.
  - Existence checks (`EntityManager.Exists`) and the `EnemyDeadTag` marker guard every ECB destruction, so an entity is never destroyed twice.
- **Documentation**:
  - 100% of public methods, systems, and structs are documented with standard XML docstrings in English.

---

<a id="controls-in-game-benchmark-hud"></a>
## Controls & In-Game Benchmark HUD

An interactive IMGUI HUD and keyboard hotkeys are included to inspect and control the benchmark at runtime:

| Hotkey / Control | Action | Details |
|:---:|:---|:---|
| **1 – 7** (Main / Numpad) | **Target Presets** | `1`: 1,000 &bull; `2`: 5,000 &bull; `3`: 10,000 &bull; `4`: 50,000 &bull; `5`: 100,000 &bull; `6`: 500,000 &bull; `7`: 1,000,000 |
| **+** / **-** | **Horde Step** | Adjust horde target by $\pm$ 10,000 (Hold **Shift** for $\pm$ 50,000) |
| **HUD buttons** | **Fine Adjustments** | `-50k` &bull; `-10k` &bull; `-1k` &bull; `+1k` &bull; `+10k` &bull; `+50k` &bull; `Clear All` |
| **C** or **Delete** | **Clear All** | Instantly destroys all active enemies and XP orbs |
| **F1** | **Toggle HUD** | Shows / hides the on-screen benchmark interface |
| **Z, Q, S, D** / Arrows | **Movement** | Player controls |

---

<a id="tech-stack"></a>
## Tech Stack
- **Engine**: Unity 6 (6000.4.0f1)
- **DOTS Packages**: `com.unity.entities` (1.3.12), `com.unity.entities.graphics` (1.3.12), `com.unity.burst` (1.8.24), `com.unity.mathematics` (1.3.2)
- **Input System**: `com.unity.inputsystem` (New Input System)
- **Rendering**: Universal Render Pipeline (URP) with GPU Instancing
