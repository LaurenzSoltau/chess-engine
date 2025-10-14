using Chess;
using Chess.Bot;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GameManager : Control
{
    [Export]
    PackedScene boardUIPackedScene;
    [Export]
    SubViewport SubViewport;
    [Export]
    public LineEdit fenLine;
    [Export]
    Button evalPosButton;
    [Export]
    RichTextLabel evalPosLabel;
    VBoxContainer sideBar;
    RichTextLabel searchDiagnosticsLabel;

    [Signal]
    public delegate void HumanTurnEventHandler();
    public BoardUI boardUi;

    public enum PlayerType { Human, Bot };

    public enum GameState { Playing, WhiteIsMated, BlackIsMated, Stalemate, Repition, FiftyMove, InsufficientMaterial }

    public GameState gameState;
    PlayerType whitePlayerType;
    PlayerType blackPlayerType;
    PlayerType playerToMove;
    int gameId;
    int colorToMove;
    public Board board;

    Random random;
    Searcher ai;

    public string PositionFen;



    public override void _Ready()
    {
        boardUi = boardUIPackedScene.Instantiate() as BoardUI;
        sideBar = GetNode<VBoxContainer>("HBoxContainer/MarginContainer/SideBar");
        searchDiagnosticsLabel = GetNode<RichTextLabel>("HBoxContainer/MarginContainer/SideBar/RichTextLabel");
        SubViewport.AddChild(boardUi);
        boardUi.AttempMakeMove += OnMoveAttempted;
        evalPosButton.Pressed += () => HandleEvaluateButtonPressed(evalPosButton);
        foreach (Node child in GetNode<VBoxContainer>("HBoxContainer/MarginContainer/SideBar").GetChildren())
        {
            if (child.IsInGroup("StartGameButton"))
            {
                if (child is Button startGameButton)
                {
                    startGameButton.Pressed += () => HandleStartButtonPressed(startGameButton);
                }
            }
        }

        random = new Random();

        PositionFen = FenUtil.StartFen;
        gameId = 0;
        SetPlayers(1);
        NewGame();
    }


    private void OnMoveAttempted(int fromIndex, int toIndex)
    {
        if (playerToMove != PlayerType.Human || gameState != GameState.Playing)
        {
            return;
        }

        Move legalMove = new();
        bool isLegal = false;

        MoveGenerator gen = new();
        var legalMoves = gen.GenerateLegalMoves(board, board.ColourToMove);
        foreach (Move move in legalMoves)
        {
            if (move.From == fromIndex && move.To == toIndex)
            {
                legalMove = move;
                isLegal = true;
                break;
            }
        }
        if (isLegal)
        {
            board.MakeMove(legalMove);
            boardUi.UpdatePosition();
            boardUi.OnMoveAccepted(legalMove);
            SwitchTurn();
            UpdateGameState();
        }
        else
        {
            boardUi.OnMoveDeclined();
        }
    }

    void PerformTurn()
    {
        if (gameState != GameState.Playing)
        {
            return;
        }

        if (GetPlayerType(colorToMove) == PlayerType.Bot)
        {
            MakeAiMove();
        } else
        {
            EmitSignal(SignalName.HumanTurn);
        }
    }

    PlayerType GetPlayerType(int color)
    {
        return color == Chess.Piece.White ? whitePlayerType : blackPlayerType;
    }

    void SwitchTurn()
    {
        if (gameState != GameState.Playing)
        {
            return;
        }
        colorToMove = colorToMove == Chess.Piece.White ? Chess.Piece.Black : Chess.Piece.White;
        playerToMove = GetPlayerType(colorToMove);
        if (playerToMove == PlayerType.Human)
        {
            EmitSignal(SignalName.HumanTurn);
        }
        else
        {
            MakeAiMove();
        }

    }

    void HandleStartButtonPressed(Button button)
    {
        SetPlayers((int)button.GetMeta("GameModeCode"));
        NewGame();
    }


    void HandleEvaluateButtonPressed(Button button)
    {
        Evaluation evaluation = new();
        int score = evaluation.Evaluate(board);
        evalPosLabel.Text = "Pos eval: " + score;
    }

    void NewGame()
    {
        gameId++;
        // load position from fen string if present, else load starting position
        if (fenLine.Text == "")
        {
            PositionFen = FenUtil.StartFen;
        }
        else
        {
            PositionFen = fenLine.Text;
        }

        string colorToMoveString = FenUtil.getColorToMove(PositionFen);
        colorToMove = colorToMoveString == "w" ? Chess.Piece.White : Chess.Piece.Black;
        playerToMove = colorToMoveString == "w" ? whitePlayerType : blackPlayerType;
        board = new();
        board.LoadPosition(PositionFen);
        ai = new Searcher(board);
        boardUi.SetBoard(board);
        boardUi.SetPerspective(whitePlayerType == PlayerType.Human ? Chess.Piece.White : Chess.Piece.Black);

        gameState = GameState.Playing;
        PerformTurn();
    }

    void SetPlayers(int playModeCode)
    {
        if (playModeCode == 1)
        {
            whitePlayerType = PlayerType.Human;
            blackPlayerType = PlayerType.Bot;
        }
        else if (playModeCode == 2)
        {
            whitePlayerType = PlayerType.Bot;
            blackPlayerType = PlayerType.Human;
        }
        else if (playModeCode == 3)
        {
            whitePlayerType = PlayerType.Bot;
            blackPlayerType = PlayerType.Bot;
        } else if (playModeCode == 4)
        {
            whitePlayerType = PlayerType.Human;
            blackPlayerType = PlayerType.Human;
        }
    }

    async void MakeAiMove()
    {
        int thisGameId = gameId;
        await Task.Delay(20);
        if (thisGameId != gameId)
        {
            return;
        }
        ai.StartSearch(5);
        Move move = ai.bestMove;
        if (!move.isValid)
        {
            GD.Print("Invalid move from search");
            UpdateGameState();
            return;
        }

        board.MakeMove(move);
        boardUi.UpdatePosition();
        boardUi.OnAiTurn(move);
        UpdateGameState();
        UpdateSearchDiagnostics(ai.botDiagnostics);
        SwitchTurn();
    }

    void UpdateSearchDiagnostics(BotDiagnostics diagnostics)
    {
        searchDiagnosticsLabel.Text = $"Time searched: {diagnostics.searchTimeMs}ms\nEval: {diagnostics.eval}\nDepth searched: {diagnostics.depthSearched}";
    }

    void UpdateGameState()
    {
        MoveGenerator gen = new();
        var legalMoves = gen.GenerateLegalMoves(board, board.ColourToMove);
        if (legalMoves.Count == 0)
        {
            if (board.IsKingInCheck(-colorToMove))
            {
                gameState = (colorToMove == Chess.Piece.Black) ? GameState.WhiteIsMated : GameState.BlackIsMated;
            }
            else
            {
                gameState = GameState.Stalemate;
            }
            OnGameOver();
            return;
        }

        if (board.IsFiftyMoveRuleReached())
        {
            gameState = GameState.FiftyMove;
            OnGameOver();
            return;
        }

        if (board.IsInsufficientMaterial())
        {
            gameState = GameState.InsufficientMaterial;
            OnGameOver();
            return;
        }

        if (board.IsThreefoldRepition())
        {
            gameState = GameState.Repition;
            OnGameOver();
            return;
        }
    }

    void OnGameOver()
{
    string message = gameState switch
    {
        GameState.WhiteIsMated => "Schwarz gewinnt durch Schachmatt!",
        GameState.BlackIsMated => "Weiß gewinnt durch Schachmatt!",
        GameState.Stalemate => "Remis durch Patt!",
        GameState.Repition => "Remis durch dreifache Stellungswiederholung!",
        GameState.FiftyMove => "Remis durch 50-Züge-Regel!",
        GameState.InsufficientMaterial => "Remis durch unzureichendes Material!",
        _ => "Unbekannter Spielzustand.",
    };
    GD.Print("Game Over: " + message);
    boardUi.blockInputState();
}

}
