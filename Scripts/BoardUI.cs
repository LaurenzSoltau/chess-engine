using Chess;
using Godot;
using System;
using System.Collections.Generic;
using System.Resources;

public partial class BoardUI : Node2D
{
    [Export]
    PackedScene squareScene;
    Node2D tileContainer;
    Node2D pieceContainer;

    [Signal]
    public delegate void MoveAttemptedEventHandler(Vector2 fromSquare, Vector2 toSquare);
    private Vector2? selectedSquare = null;

    bool fromWhitePerspective = true;

    public Vector2 BoardSize = new(600, 600);
    const int BoardDimension = 8;
    int tileSizeInPx;

    Piece[] pieces = new Piece[64];
    Square[] squares = new Square[64];
    Board board;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Vector2 clickedSquare = GetSquareFromMouse(GetLocalMousePosition());

            if (selectedSquare == null)
            {
                // First click: select piece square
                selectedSquare = clickedSquare;
            }
            else
            {
                // Second click: destination square
                Vector2 from = selectedSquare.Value;
                Vector2 to = clickedSquare;

                // Emit the move signal
                GD.Print(from, to);
                EmitSignal(nameof(MoveAttempted), from, to);

                // Clear selection & highlights
                selectedSquare = null;
            }
        }
    }

    private Vector2 GetSquareFromMouse(Vector2 mousePos)
    {
        GD.Print(mousePos.X);
        GD.Print(mousePos.Y);
        // Example: convert mouse position to board coordinates (0-7, 0-7)
        // Adjust based on your board UI layout and scaling!
        int file = Math.Clamp(((int)mousePos.X) / ((int)tileSizeInPx), 0, 7);
        int rank = Math.Clamp(7 - (int)(mousePos.Y / tileSizeInPx), 0, 7);
        return new Vector2(rank, file);
    }

    public override void _Ready()
    {
        board = new Board();
        board.LoadPosition(FenUtil.StartFen);
        tileSizeInPx = (int)BoardSize.X / BoardDimension;
        tileContainer = GetNode<Node2D>("TileContainer");
        pieceContainer = GetNode<Node2D>("PieceContainer");
        CreateBoardUi();
        UpdatePosition(board);
    }

    public void HighlightPossibleMoves(Board board, int fromSquare)
    {
        List<Move> moves = board.GenerateLegalMoves();
        for (int i = 0; i < moves.Count; i++)
        {
            Move move = moves[i];
            if (move.From == fromSquare)
            {
                (int rank, int file) coord = BoardRepresentation.CoordFromIndex(i);
                SetSquareHighlightColor(coord, BoardColors.lightSquares.highlightPossibleMove, BoardColors.darkSquares.highlightPossibleMove);
            }
        }
    }


    void CreateBoardUi()
    {
        for (int rank = 0; rank < BoardDimension; rank++)
        {
            for (int file = 0; file < BoardDimension; file++)
            {
                Square square = squareScene.Instantiate() as Square;
                tileContainer.AddChild(square);
                square.Position = new Vector2(file * tileSizeInPx, rank * tileSizeInPx);
                bool isLight = (rank + file) % 2 == 0;
                square.Setup(isLight, new Vector2(tileSizeInPx, tileSizeInPx));
                squares[BoardRepresentation.IndexFromCoord(rank, file)] = square;
            }
        }
    }

    public void SetPerspective(int color)
    {
        fromWhitePerspective = color == Chess.Piece.White;
        UpdatePosition(board);
    }

    public void UpdatePosition(Board board)
    {

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            Piece piece = pieces[i];
            piece.QueueFree();
            pieces[i] = null;
        }

        for (int rank = 0; rank < BoardDimension; rank++)
        {
            for (int file = 0; file < BoardDimension; file++)
            {
                int pieceCode = board.Squares[BoardRepresentation.IndexFromCoord(rank, file)];
                if (pieceCode == Chess.Piece.None) continue;
                SpawnPiece((rank, file), pieceCode);

            }
        }
    }

    void SetSquareHighlightColor((int rank, int file) coord, Color lightColor, Color darkColor)
    {
        Square square = squares[BoardRepresentation.IndexFromCoord(coord.rank, coord.file)];
        square.SetHighlightColor(square.isLight ? lightColor : darkColor);
    }

    void SpawnPiece((int rank, int file) coord, int pieceCode)
    {
        int square = BoardRepresentation.IndexFromCoord(coord.rank, coord.file);
        string texturePath = BoardUIUtil.GetPieceTexturePath(pieceCode);
        Texture2D pieceTexture = GD.Load<Texture2D>(texturePath);
        PackedScene piecePackedScene = GD.Load<PackedScene>(BoardUIUtil.PieceScenePath);
        Piece pieceScene = piecePackedScene.Instantiate() as Piece;
        pieces[square] = pieceScene;
        pieceScene.Position = PositionFromIndex(square);
        float scaleFactor = tileSizeInPx / 128.0f;
        pieceScene.Scale = new Vector2(scaleFactor, scaleFactor);
        pieceScene.GetNode<Sprite2D>("Sprite2D").Texture = pieceTexture;
        pieceContainer.AddChild(pieceScene);
    }


    Vector2 PositionFromIndex(int index)
    {
        int displayIndex = BoardRepresentation.PerspectiveIndex(index, fromWhitePerspective);

        (int rank, int file) coord = BoardRepresentation.CoordFromIndex(displayIndex);
        int x = coord.file * tileSizeInPx + tileSizeInPx / 2;
        int y = (int)BoardSize.X - (coord.rank * tileSizeInPx + tileSizeInPx / 2);

        return new Vector2(x, y);
    }

    public Vector2 GetBoardCenterOffset()
    {
        return new Vector2(tileSizeInPx * BoardDimension / 2, tileSizeInPx * BoardDimension / 2);
    }
}
