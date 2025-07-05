using Chess;
using Chess.Bot;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class GameManager : Control
{
    [Export]
    PackedScene boardUIPackedScene;
    [Export]
    SubViewport SubViewport;
    VBoxContainer sideBar;
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
        SubViewport.AddChild(boardUi);
        boardUi.AttempMakeMove += OnMoveAttempted;
        foreach (Node child in GetNode<VBoxContainer>("HBoxContainer/MarginContainer/SideBar").GetChildren())
        {
            if (child is Button startGameButton)
            {
                startGameButton.Pressed += () => HandleStartButtonPressed(startGameButton);
            }
        }

        random = new Random();

        PositionFen = FenUtil.StartFen;
        gameId = 0;
        SetPlayers(1);
        NewGame();
    }



    public override void _Process(double delta)
    {
        HandleInput();
    }

    void HandleInput()
    {
    }

    private void OnMoveAttempted(int fromIndex, int toIndex)
    {
        if (playerToMove != PlayerType.Human || gameState != GameState.Playing)
        {
            return;
        }

        Move legalMove = new();
        bool isLegal = false;

        var legalMoves = board.GenerateLegalMoves();
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
            UpdateGameState();
            SwitchTurn();
            boardUi.OnMoveAccepted(legalMove);
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

    void NewGame()
    {
        gameId++;
        colorToMove = Chess.Piece.White;
        playerToMove = whitePlayerType;
        board = new();
        board.LoadPosition(PositionFen);
        ai = new Searcher(board);
        boardUi.SetBoard(board);
        boardUi.SetPerspective(blackPlayerType == PlayerType.Human ? Chess.Piece.Black : Chess.Piece.White);

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
        else
        {
            whitePlayerType = PlayerType.Bot;
            blackPlayerType = PlayerType.Bot;
        }
    }

    async void MakeAiMove()
    {
        int thisGameId = gameId;
        await Task.Delay(200);
        if (thisGameId != gameId)
        {
            return;
        }
        ai.StartSearch(4);
        Move move = ai.bestMove;

        board.MakeMove(move);
        boardUi.UpdatePosition();
        boardUi.OnAiTurn(move);
        UpdateGameState();

        SwitchTurn();
    }

    void UpdateGameState()
    {
        var legalMoves = board.GenerateLegalMoves();
        if (legalMoves.Count == 0)
        {
            if (board.IsKingInCheck(-colorToMove))
            {
                gameState = (colorToMove == Chess.Piece.White) ? GameState.WhiteIsMated : GameState.BlackIsMated;
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

        if (board.IsInsufficientMaterial()) {
            gameState = GameState.InsufficientMaterial;
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

    // Optional: UI anzeigen
    // Spielinteraktion stoppen
}

}
