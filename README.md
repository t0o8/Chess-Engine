# TOML Chess Engine

TOML is a UCI compatible high-performance, fully custom Chess Engine written in C#. It uses a 64-bit Bitboard board representation, Magic Bitboards for sliding piece move generation, and a highly optimized zero-allocation search algorithm.

## Features

* **Board Representation:** 64-bit Bitboards combined with a 1D mailbox array (`ushort[]`) for fast piece lookups and updates.
* **Move Generation:** 
  * Pseudo-legal move generation with legality checking.
  * **Magic Bitboards** for Rook and Bishop attack lookups.
  * Zero-allocation move generation using `stackalloc`
* **Search Algorithm:**
  * Iterative Deepening.
  * Alpha-Beta Pruning with Principal Variation Search (PVS).
  * Quiescence Search to resolve forcing tactical sequences.
  * Delta Pruning for faster Quiescence Search resolution.
* **Heuristics & Pruning:**
  * Transposition Table (1M+ entries) powered by **Zobrist Hashing**.
  * Killer Move heuristic.
  * Move Ordering (Hash moves → Promotions → Captures sorted by MVV-LVA → Killer moves).
* **Evaluation:**
  * Material counting.
  * Piece-Square Tables (PSTs) for positional bonuses in different phases of the game.
  * Advanced pawn structure evaluation (passed pawns, isolated pawns, and pawn chain detection).
* **Testing & Benchmarking:**
  * Built-in `Perft` suite validating engine accuracy against known complex positions up to high depths.
 
## Project Structure

* **`Main.cs`**: The entry point to initialize precomputed data, set up positions, and trigger searches.
* **`Position.cs`**: Handles the internal board state, making/unmaking moves, FEN parsing, and pin/attack detection.
* **`MoveGeneration.cs`**: Generates all pseudo-legal moves for a given position.
* **`Search.cs`**: The core alpha-beta search algorithm, time management loop, and UCI-like console output.
* **`Evaluation.cs`**: Static evaluation function scoring piece values, piece-square tables, and structural bonuses.
* **`PrecomputedInfo.cs` / `MagicNumberGenerator.cs`**: Handles generation of move bitboards, blocking masks, and magic numbers on startup.
* **`ZobristHashing.cs` / `TranspositionTable.cs`**: Implementation of position caching to avoid re-searching identical sub-trees.
* **`Benchmark.cs`**: An integrated suite to run node-counting checks (Perft) to ensure move generation rules (en passant, castling rights, pins) are handled flawlessly.

## Getting Started

### Initializing the Engine
Before using the engine, you must initialize the precomputed lookup tables and Zobrist hash seeds:

```csharp
using TOML;

// Precomputes magic bitboards, distance tables, and random hashes
Main.Start(); 
```

### Setting up a Position
You can set up a board state using standard FEN (Forsyth-Edwards Notation):

```csharp
// Setup the starting position
Main.SetupPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
```

### Running a Search
You can allocate a time limit and begin the search. The engine will print UCI-styled output (`info depth ... nodes ... nps ...`) during the search and return the best move:

```csharp
// Start search with a 5000ms (5 seconds) time limit
Main.SearchTime = 5000;
Move bestMove = Main.BeginSearch();

Console.WriteLine("Best move is: " + bestMove.GetText());
```

### Running the Perft Suite
To verify the integrity of the move generator, you can run the built-in benchmarking tool:

```csharp
// Runs a battery of strict move generation tests
TOML.Benchmark.PerftTest();
```
