using Chess;
using Godot;
using System;

public partial class BoardUI : Node2D
{
    [Export]
    PackedScene squareScene;
    Node2D tileContainer;
    Node2D pieceContainer;

    public enum InputState
    {
        None,
        PieceSelected,
        DraggingPiece,
        Blocked
    }

    bool firstMoveMade = false;

    InputState currentState;

    (int rank, int file) selectedPieceSquare;

    (int from, int to) lastMoveMade;

    [Signal]
    public delegate void AttempMakeMoveEventHandler(int fromIndex, int toIndex);
    bool fromWhitePerspective = true;

    public Vector2 BoardSize = new(600, 600);
    const int BoardDimension = 8;
    int tileSizeInPx;

    Piece[] pieces = new Piece[64];
    Square[] squares = new Square[64];
    Board logicBoard;
    GameManager gameManager;

    public override void _Ready()
    {
        gameManager = (GameManager)GetTree().CurrentScene;
        gameManager.HumanTurn += OnHumanTurn;
        tileSizeInPx = (int)BoardSize.X / BoardDimension;
        tileContainer = GetNode<Node2D>("TileContainer");
        pieceContainer = GetNode<Node2D>("PieceContainer");
        CreateBoardUi();
        currentState = InputState.None;
        Board board = new();
        board.LoadPosition(FenUtil.StartFen);
        SetBoard(board);
    }
    public override void _Process(double delta)
    {
        HandleInput();
    }

    public void OnAiTurn(Move move)
    {
        firstMoveMade = true;
        lastMoveMade = (move.From, move.To);
        ResetSquareColours();
    }

    void OnHumanTurn()
    {
        currentState = InputState.None;
    }
    public void SetBoard(Board board)
    {
        firstMoveMade = false;
        logicBoard = board;
        ResetSquareColours(false);
        UpdatePosition();
    }

    void HandleInput()
    {
        if (currentState == InputState.Blocked)
        {
            return;
        }
        Vector2 mousePos = GetLocalMousePosition();

        if (currentState == InputState.None)
        {
            HandlePieceSelection(mousePos);
        }
        else if (currentState == InputState.DraggingPiece)
        {
            HandleDragMovement(mousePos);
        }
        else if (currentState == InputState.PieceSelected)
        {
            HandleClickMovement(mousePos);
        }
    }

    void HandleClickMovement(Vector2 mousePos)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            HandlePiecePlacement(mousePos);
        }
    }

    void HandlePieceSelection(Vector2 mousePos)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var pressedSquare = GetSquareFromMouse(mousePos);
            int index = BoardRepresentation.IndexFromCoord(pressedSquare.rank, pressedSquare.file);
            if (index < 0 || index > 63) return;

            selectedPieceSquare = pressedSquare;
            if (Chess.Piece.IsColor(logicBoard.Squares[index], logicBoard.ColourToMove))
            {
                ResetSquareColours();
                HighlightLegalMoves();
                SelectSquare(selectedPieceSquare);
                currentState = InputState.DraggingPiece;
            }
        }
    }

    void HighlightLegalMoves()
    {
        MoveGenerator gen = new();
        var legalMoves = gen.GenerateLegalMoves(logicBoard, logicBoard.ColourToMove);
        int selectedMoveIndex = BoardRepresentation.IndexFromCoord(selectedPieceSquare.rank, selectedPieceSquare.file);
        foreach (Move move in legalMoves) {

            if (selectedMoveIndex == move.From)
            {
                (int rank, int file) coords = BoardRepresentation.CoordFromIndex(move.To);
                SetSquareHighlightColor(coords, BoardColors.lightSquares.highlightPossibleMove, BoardColors.darkSquares.highlightPossibleMove);
            }
        }
    }

    void HandleDragMovement(Vector2 mousePos)
    {
        DragPiece(selectedPieceSquare, mousePos);
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            HandlePiecePlacement(mousePos);
        }
    }


    void HandlePiecePlacement(Vector2 mousePos)
    {
        (int rank, int file) targetSquare = GetSquareFromMouse(mousePos);
        int index = BoardRepresentation.IndexFromCoord(targetSquare.rank, targetSquare.file);
        if (index < 0 || index > 63) return;

        if (targetSquare == selectedPieceSquare)
        {
            ResetPiecePosition(selectedPieceSquare);
            if (currentState == InputState.DraggingPiece)
            {
                currentState = InputState.PieceSelected;
            }
            else
            {
                ResetSquareColours();
                currentState = InputState.None;
                DeselectSquare(selectedPieceSquare);
            }
        }
        else
        {
            if (Chess.Piece.IsColor(logicBoard.Squares[index], logicBoard.ColourToMove) && logicBoard.Squares[index] != Chess.Piece.None)
            {
                CancelPieceSelection();
            }
            else
            {
                TryMakeMove(selectedPieceSquare, targetSquare);
            }
        }
    }

    void TryMakeMove((int rank, int file) selectedPieceSquare, (int rank, int file) targetSquare)
    {
        int fromIndex = BoardRepresentation.IndexFromCoord(selectedPieceSquare.rank, selectedPieceSquare.file);
        int toIndex = BoardRepresentation.IndexFromCoord(targetSquare.rank, targetSquare.file);
        EmitSignal(SignalName.AttempMakeMove, fromIndex, toIndex);
    }

    public void OnMoveAccepted(Move move)
    {
        currentState = InputState.Blocked;
        lastMoveMade = (move.From, move.To);
        UpdatePosition();
        ResetSquareColours();
        firstMoveMade = true;
    }

    public void OnMoveDeclined()
    {
        ResetSquareColours();
        CancelPieceSelection();
    }

    void ResetSquareColours(bool highlight = true)
    {
        for (int rank = 0; rank < BoardDimension; rank++)
        {
            for (int file = 0; file < BoardDimension; file++)
            {
                SetSquareHighlightColor((rank, file), BoardColors.lightSquares.normal, BoardColors.darkSquares.normal);
            }
        }
        var from = BoardRepresentation.CoordFromIndex(lastMoveMade.from);
        var to = BoardRepresentation.CoordFromIndex(lastMoveMade.to);
        if (highlight && firstMoveMade)
        {
            SetSquareHighlightColor(from, BoardColors.lightSquares.highlightLastMove, BoardColors.darkSquares.highlightLastMove);
            SetSquareHighlightColor(to, BoardColors.lightSquares.highlightMove, BoardColors.darkSquares.highlightMove);
        }
    }

    void CancelPieceSelection()
    {
        if (currentState != InputState.None)
        {
            currentState = InputState.None;
            DeselectSquare(selectedPieceSquare);
            ResetPiecePosition(selectedPieceSquare);
        }
    }

    void ResetPiecePosition((int rank, int file) square)
    {
        int index = BoardRepresentation.IndexFromCoord((int)square.rank, (int)square.file);
        Piece piece = pieces[index];
        piece.Position = PositionFromIndex(index);
    }

    void DragPiece((int Rank, int File) pieceCoord, Vector2 mousePos)
    {
        int index = BoardRepresentation.IndexFromCoord((int)pieceCoord.Rank, (int)pieceCoord.File);
        if (index < 0 || index > 63) return;
        Piece piece = pieces[index];
        piece.Position = mousePos;
        piece.ZIndex = 2;
    }

    void SelectSquare((int rank, int file) square)
    {
        SetSquareHighlightColor(((int)square.rank, (int)square.file), BoardColors.lightSquares.highlightMove, BoardColors.darkSquares.highlightMove);
    }

    void DeselectSquare((int rank, int file) square)
    {
        SetSquareHighlightColor(((int)square.rank, (int)square.file), BoardColors.lightSquares.normal, BoardColors.darkSquares.normal);
    }




    // returns internal representation of Square clicked on
    private (int rank, int file) GetSquareFromMouse(Vector2 mousePos)
    {
        int file = Math.Clamp(((int)mousePos.X) / ((int)tileSizeInPx), 0, 7);
        int rank = Math.Clamp(7 - (int)(mousePos.Y / tileSizeInPx), 0, 7);

        if (!fromWhitePerspective)
        {
            rank = 7 - rank;
            file = 7 - file;
        }
        return (rank, file);
    }



    void CreateBoardUi()
    {
        for (int rank = 0; rank < BoardDimension; rank++)
        {
            for (int file = 0; file < BoardDimension; file++)
            {
                Square square = squareScene.Instantiate() as Square;
                tileContainer.AddChild(square);

                var coord = (rank, file);
                var visCoord = CoordUtil.FlipForPerspective(coord, true);
                float x = visCoord.file * tileSizeInPx;
                float y = (7 - visCoord.rank) * tileSizeInPx;
                square.Position = new Vector2(x, y);
                bool isLight = (rank + file) % 2 != 0;
                square.Setup(isLight, new Vector2(tileSizeInPx, tileSizeInPx));
                squares[BoardRepresentation.IndexFromCoord(rank, file)] = square;
            }
        }
    }

    public void SetPerspective(int color)
    {
        fromWhitePerspective = color == Chess.Piece.White;
        UpdatePosition();
    }

    public void blockInputState()
    {
        currentState = InputState.Blocked;
    }

    public void UpdatePosition()
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
                int pieceCode = logicBoard.Squares[BoardRepresentation.IndexFromCoord(rank, file)];
                if (pieceCode == Chess.Piece.None) continue;
                SpawnPiece((rank, file), pieceCode);

            }
        }
    }

    void SetSquareHighlightColor((int rank, int file) coord, Color lightColor, Color darkColor)
    {
        var visCoord = CoordUtil.FlipForPerspective(coord, fromWhitePerspective);
        Square square = squares[BoardRepresentation.IndexFromCoord(visCoord.rank, visCoord.file)];
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
        (int rank, int file) coord = BoardRepresentation.CoordFromIndex(index);
        (int rank, int file) visCoord = CoordUtil.FlipForPerspective(coord, fromWhitePerspective);
        int x = visCoord.file * tileSizeInPx + tileSizeInPx / 2;
        int y = (7 - visCoord.rank) * tileSizeInPx + tileSizeInPx / 2;

        return new Vector2(x, y);
    }

    public Vector2 GetBoardCenterOffset()
    {
        return new Vector2(tileSizeInPx * BoardDimension / 2, tileSizeInPx * BoardDimension / 2);
    }
}
