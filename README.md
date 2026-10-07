# Roguelike Benchmark: Classic OOP vs. Unity DOTS/ECS

[![Unity Version](https://img.shields.io/badge/Unity-6000.4.0f1-black?logo=unity)](https://unity.com/)
[![DOTS / Entities](https://img.shields.io/badge/Unity%20DOTS-Entities%201.3+-blue?logo=unity)](https://unity.com/dots)
[![Burst Compiler](https://img.shields.io/badge/Burst-Compiled%20(SIMD)-green)](https://docs.unity3d.com/Packages/com.unity.burst@latest)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Facade%20Pattern-orange)](#system-architecture--hybrid-bridge)
[![License](https://img.shields.io/badge/License-MIT-lightgrey.svg)](LICENSE)

A high-performance technical benchmark and portfolio project developed in **Unity 6**. The primary objective is to scientifically explore, measure, and analyze the architectural and hardware-level performance boundaries between **Classic Object-Oriented Programming (MonoBehaviour / OOP)** and **Data-Oriented Technology Stack (DOTS / ECS)** in a Survivor-like game context.

---

## 📑 Table of Contents
1. [Project Overview](#project-overview)
2. [System Architecture & Hybrid Bridge](#system-architecture--hybrid-bridge)
3. [Phase 1: Classic OOP Implementation & Profiling](#phase-1-classic-oop-implementation--profiling)
   - [Architectural Patterns](#architectural-patterns-in-oop)
   - [Profiling & Identifying the Bottleneck](#profiling--identifying-the-bottleneck)
   - [OOP Benchmark Results](#oop-benchmark-results)
4. [Phase 2: DOTS/ECS Re-Engineering](#phase-2-dotsecs-re-engineering)
   - [Data-Oriented Design (DOD) Principles](#data-oriented-design-dod-principles)
   - [Core Systems Overview](#core-systems-overview)
   - [In-Game Runtime Benchmark Suite](#in-game-runtime-benchmark-suite)
5. [Performance Comparison & Hardware Analysis](#performance-comparison--hardware-analysis)
6. [Engineering Standards & Best Practices](#engineering-standards--best-practices)
7. [Controls & In-Game Benchmark HUD](#controls--in-game-benchmark-hud)

---

## 🎯 Project Overview

In survivor-like horde games, rendering and updating tens of thousands of dynamic agents simultaneously is one of the most demanding challenges for game engines. 

This project implements the same game mechanics twice:
1. **First Implementation (OOP)**: Pushed to the theoretical limits of traditional Unity `MonoBehaviour` development using best-practice optimizations (Object Pooling, 2D Spatial Hash Grid, Time-Sliced Boids Steering).
2. **Second Implementation (DOTS/ECS)**: Re-engineered from the ground up using **Entities**, **C# Job System**, and the **Burst Compiler**, backed by GPU-driven batched rendering via `BatchRendererGroup`.

The project features a **real-time in-game benchmark controller** allowing runtime horde adjustments up to **1,000,000 entities** without code recompilation.

---

## 🏗️ System Architecture & Hybrid Bridge

A cornerstone of this project is **Clean Architecture** and strict adherence to the **Single Responsibility Principle (SRP)**. 

To prevent high-level gameplay scripts (weapons, projectiles, visual feedback) from leaking low-level ECS pointers, unmanaged memory handles, or spatial hashing calculations, the project introduces the **Bridge / Facade Pattern** via [`EnemyBridge.cs`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Enemy/EnemyBridge.cs).

```mermaid
flowchart TD
    subgraph GameplayLayer ["Gameplay Domain (MonoBehaviour / OOP)"]
        FW["FireWand (IWeapon)"]
        SW["Sword (IWeapon)"]
        PR["Projectile"]
        UI["Benchmark HUD & EnemyCounterUI"]
    end

    subgraph FacadeLayer ["Architectural Bridge (Facade Pattern)"]
        EB["EnemyBridge (Static Service)"]
        BC["BenchmarkController"]
    end

    subgraph DotsLayer ["Simulation Domain (DOTS / ECS - Burst & Unmanaged)"]
        EHS["EnemyHashSystem (Spatial Partitioning)"]
        EMS["EnemyMovementSystem (SIMD Burst Job)"]
        ESS["EnemySpawnerSystem (Batch GPU Instancing)"]
        ECS["EnemyCounterSystem (Archetype Metadata)"]
        EXS["ExperienceSpawnSystem / CollectSystem"]
    end

    FW -->|"TryGetClosestEnemy()"| EB
    SW -->|"DealDamage()"| EB
    PR -->|"DealDamage()"| EB
    UI -->|"Reads Total & Visible"| ECS
    BC -->|"Sets Target Horde (0 - 1M)"| ESS

    EB -->|"Neighbourhood Spatial Lookup"| EHS
    EB -->|"Deferred Destruction via ECB"| DotsLayer
```

### Decoupling Benefits
- **No Leaky Abstractions**: Gameplay scripts never import `Unity.Entities` or touch `World.DefaultGameObjectInjectionWorld`.
- **Encapsulated Grid Metrics**: Spatial cell size (`CellSize`) is declared `internal` to the simulation domain. High-level weapons simply query `EnemyBridge.TryGetClosestEnemy(origin, range, out target)`.
- **Single-Pass Projectile Collision**: Instead of performing multiple spatial queries per projectile per frame, `EnemyBridge.DealDamage` applies damage and returns the hit count in a single traversal, cutting collision detection overhead in half.

---

## 🧱 Phase 1: Classic OOP Implementation & Profiling

### Architectural Patterns in OOP
To establish a fair and optimized baseline, the OOP phase incorporated several production-grade patterns:
- **Spatial Hash Grid**: Reduced collision detection complexity between horde units from $\mathcal{O}(n^2)$ to $\mathcal{O}(1)$ by mapping positions into discrete 2D spatial buckets.
- **Dynamic Chunk System**: Procedurally loads map tiles around the player to support camera zoom and unbounded exploration without out-of-bounds artifacts.
- **Strict Object Pooling**: Pre-allocated pools for enemies, damage numbers, projectiles, and experience gems, dropping runtime allocations on the critical path to **0 – 1 KB per frame**.
- **Observer Pattern**: Decoupled events for player health, level up triggers, and HUD updates.
- **Boids-Inspired Horde Steering**: Blended vector steering combining *Seek* (player attraction) with a smoothed, time-sliced *Separation* vector (`Vector3.Lerp`) to prevent unnatural teleportation or overlap.

### Profiling & Identifying the Bottleneck

Profiling with Unity Profiler revealed that the primary bottleneck was located in `EnemyPool.Update() -> Enemy.Tick()`:

```
[Profiler Call Stack]
EnemyPool.Update()
 └── Enemy.Tick()
      └── CalculateSteeringForces()  <-- ~75% of Frame CPU Time (Boids Separation)
```

Even with spatial partitioning and time-slicing, updating large hordes on a single thread hit a hard performance ceiling.

### OOP Benchmark Results

| Enemy Count | Frame Update Time (ms) | Target Frame Rate | Status / Playability |
|:---:|:---:|:---:|:---|
| **2,000** | 4.21 ms | > 144 FPS | Extremely smooth, perfectly stable |
| **4,000** | 8.94 ms | ~120 FPS | Minor drops below monitor refresh rate |
| **6,000** | 19.64 ms | ~60 FPS | Noticeable frame time increase; playable |
| **8,000** | 35.40 ms | ~30 FPS | Degraded responsiveness; functional limit |
| **10,000** | **45.59 ms** | **< 20 FPS** | **Unplayable (Hardware Ceiling Reached)** |

> **OOP Conclusion**: The limit of the OOP implementation is **~8,000–10,000 entities**. The root cause is not algorithmic inefficiency, but **hardware memory hierarchy**. OOP stores `GameObject` and `MonoBehaviour` instances as individual reference types scattered throughout the RAM. The CPU is constantly stalled by **Cache Misses** (pointer chasing), starving execution pipelines.

---

## ⚡ Phase 2: DOTS/ECS Re-Engineering

### Data-Oriented Design (DOD) Principles

To break past the 10,000 entity ceiling, the simulation was converted to **Data-Oriented Design (DOD)**:

```
Traditional OOP (Array of Structures - Scattered Pointers):
[ Heap Object 1: Transform | Health | Speed | Sprite ] -> Pointer -> [ Heap Object 2 ] (Cache Misses)

DOTS / ECS (Structure of Arrays - 16KB Archetype Chunks):
Chunk 1: [ Transform | Transform | Transform ... ]  <-- Linear Cache Line Prefetching
Chunk 2: [ Speed     | Speed     | Speed     ... ]  <-- SIMD Vectorized Execution
```

- **Archetype Chunks**: Entities with identical component configurations are packed contiguously into memory chunks of 16 KB.
- **Cache Locality**: Iterating over linear arrays maximizes L1/L2 cache prefetching, minimizing RAM latency.
- **Burst Compiler**: Compiles C# code into highly optimized native machine assembly with auto-vectorization (SIMD / AVX2).

### Core Systems Overview

1. [`EnemyMovementSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Enemy/EnemyMovementSystem.cs):
   - Burst-compiled parallel job (`IJobEntity`) computing directional vectors towards the player.
   - Zero heap allocation, fully multithreaded across all available CPU cores.
2. [`EnemyHashSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Enemy/EnemyHashSystem.cs):
   - Partitions active enemies into a `NativeParallelMultiHashMap<int2, EnemyGridData>`.
   - Dynamic capacity scaling with hysteresis to prevent frequent native reallocations.
3. [`EnemySpawnerSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Enemy/EnemySpawnerSystem.cs):
   - Uses `BatchRendererGroup` / `EntitiesGraphicsSystem` for GPU-instanced rendering.
   - Spawns enemies dynamically along camera edges in configurable batch sizes up to target horde limits.
4. [`EnemyCounterSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/UI/EnemyCounterSystem.cs):
   - Queries exact entity count instantly in $\mathcal{O}(\text{chunks})$ directly from archetype metadata (`mEnemyQuery.CalculateEntityCount()`), introducing zero frame delay and no thread contention.
   - Runs a Burst-compiled culling job (`CountVisibleEnemiesJob`) to report on-screen entity density.
5. [`ExperienceSpawnSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Experience/ExperienceSpawnSystem.cs) & [`ExperienceCollectSystem`](file:///C:/Users/mart1/Documents/Roguelike/Assets/Scripts/Experience/ExperienceCollectSystem.cs):
   - Handles XP orb instantiation and magnet attraction towards the player through decoupled Entity Command Buffers (ECB).

---

## 📊 Performance Comparison & Hardware Analysis

| Metric | Classic OOP (MonoBehaviour) | Unity DOTS / ECS | Improvement Factor |
|:---|:---:|:---:|:---:|
| **Maximum Playable Entities (≥ 30 FPS)** | ~8,000 | **100,000+** | **> 12.5x** |
| **Horde Ceiling Tested** | 10,000 (< 20 FPS) | **1,000,000 entities** | **100x Scale** |
| **CPU Frame Time @ 2,000 Entities** | 4.21 ms | **< 0.45 ms** | **~9.3x Faster** |
| **CPU Frame Time @ 10,000 Entities** | 45.59 ms (Spike) | **< 1.80 ms** | **~25x Faster** |
| **Memory Allocation per Frame** | 0 – 1 KB | **0 Bytes** (100% Unmanaged) | Zero GC Pressure |
| **Thread Utilization** | Single Thread (Main Thread) | Full Multithreading (Job Worker Threads) | Near-linear scaling across cores |
| **Memory Layout** | Heap (Scattered pointers) | Contiguous Chunks (Cache-coherent SoA) | Eliminates CPU Cache Misses |

---

## 🛠️ Engineering Standards & Best Practices

The codebase follows strict software engineering and C# conventions:

- **Naming Conventions**:
  - `m` prefix for private/protected class fields (`mCurrentPosition`, `mSpatialGrid`).
  - `p` prefix for method and constructor parameters (`pDeltaTime`, `pTargetPosition`).
  - `l` prefix for local variables (`lPlayerPos`, `lCellRange`).
- **Memory Safety**:
  - All native containers (`NativeArray`, `NativeParallelMultiHashMap`, `NativeReference`) are tracked and properly disposed in `OnDestroy()`.
  - Existence checks (`EntityManager.Exists`) precede ECB structural change commands to eliminate unmanaged dangling pointer exceptions.
- **Documentation**:
  - 100% of public methods, systems, and structs are documented with standard XML docstrings in English.

---

## 🎮 Controls & In-Game Benchmark HUD

An interactive IMGUI HUD and keyboard hotkeys are included to inspect and control the benchmark at runtime:

| Hotkey / Control | Action | Details |
|:---:|:---|:---|
| **1 – 7** (Main / Numpad) | **Target Presets** | `1`: 1,000 &bull; `2`: 5,000 &bull; `3`: 10,000 &bull; `4`: 50,000 &bull; `5`: 100,000 &bull; `6`: 500,000 &bull; `7`: 1,000,000 |
| **+** / **-** | **Horde Step** | Adjust horde target by $\pm$ 10,000 (Hold **Shift** for $\pm$ 50,000) |
| **C** or **Delete** | **Clear All** | Instantly destroys all active enemies and XP orbs |
| **F1** | **Toggle HUD** | Shows / hides the on-screen benchmark interface |
| **Z, Q, S, D** / Arrows | **Movement** | Player controls |

---

## 💻 Tech Stack
- **Engine**: Unity 6 (6000.4.0f1)
- **DOTS Packages**: `com.unity.entities` (1.3.12), `com.unity.entities.graphics` (1.3.12), `com.unity.burst` (1.8.24), `com.unity.mathematics` (1.3.2)
- **Input System**: `com.unity.inputsystem` (New Input System)
- **Rendering**: Universal Render Pipeline (URP) with GPU Instancing
