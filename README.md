# chess-engine

A chess engine written from scratch in C#, with a playable GUI built in
[Godot 4](https://godotengine.org/). The engine handles the full rules of chess
(castling, en passant, promotion, threefold repetition, the fifty-move rule and
insufficient material) and plays via alpha-beta search with iterative deepening.

This is a learning project — the goal was to build move generation, search and
evaluation myself rather than to compete with established engines.

## Features

**Board representation** (`Chess/Core/`)
- 64-entry square array plus per-piece-type [piece lists](https://www.chessprogramming.org/Piece-Lists) for fast iteration
- Incrementally updated [Zobrist hashing](https://www.chessprogramming.org/Zobrist_Hashing) for position identity and repetition detection
- Make/unmake move with restorable state (en passant square, castling rights, move clocks)
- FEN parsing and generation (`FenUtil`)

**Move generation** (`Chess/Core/MoveGenerator.cs`)
- Pseudo-legal generation followed by a legality filter via `IsSquareAttacked`
- Capture-and-promotion-only mode used by the quiescence search

**Search** (`Chess/Bot/Searcher.cs`)
- Negamax with alpha-beta pruning
- Iterative deepening
- Quiescence search on captures to reduce the horizon effect
- Draw detection by repetition inside the search tree

**Move ordering** (`Chess/Bot/MoveOrdering.cs`)
- Captures first, scored [MVV-LVA](https://www.chessprogramming.org/MVV-LVA) style (`10 × victim − attacker`)
- Promotions scored by the promoted piece

**Evaluation** (`Chess/Bot/Evaluation.cs`)
- Material counting
- [Tapered](https://www.chessprogramming.org/Tapered_Eval) piece-square tables interpolated between midgame and endgame by a material-based phase value

**UI** (`Scripts/`, `Scenes/`)
- Drag-and-drop board, human vs. human and human vs. bot
- Load a position from FEN, export the current position as FEN
- Side panel showing search diagnostics (depth reached, evaluation, time) and a static evaluation of the position

## Requirements

- [Godot 4.4](https://godotengine.org/download) — the **.NET / C# build**
- .NET SDK 8.0 or newer

## Running

```bash
git clone https://github.com/<your-user>/chess-engine.git
cd chess-engine
dotnet build
```

Then open the project folder in the Godot editor and press **F5**, or launch it
directly from the command line:

```bash
godot --path .
```

The main scene is `Scenes/main.tscn`.

## Perft test suite

Move generation is verified against [perft](https://www.chessprogramming.org/Perft)
node counts. The suite lives in `Chess/Testing/Perft/TestSuite.txt` as
`depth,expected_nodes,fen` lines and covers the standard tricky positions
(Kiwipete, en passant pin cases, promotion-heavy positions) up to depth 5.

Open `Scenes/test_suite.tscn` in the editor and run it to execute individual
tests; each result reports the node count, the time taken and whether it matched
the expected value.

Note that `TestUtil` resolves the suite with a path relative to the working
directory, so the project must be run from its root.

## Known limitations

Things a stronger engine would have that this one does not (yet):

- No transposition table — positions reached by different move orders are searched repeatedly
- Mate scores are not adjusted by ply, so the search does not prefer the *shortest* mate
- No killer-move or history heuristics in the move ordering
- No time management; the search runs to a fixed maximum depth
- Move generation is not bitboard-based
- No UCI interface, so the engine cannot be plugged into external chess GUIs

## Credits

<!-- TODO: replace with the actual source and license of the piece sprites in
     Assets/, or state that they are original work. -->
Piece sprites: see `Assets/`.

## License

[MIT](LICENSE)
