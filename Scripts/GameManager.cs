using Chess;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public partial class GameManager : Control
{
    [Export]
    PackedScene boardUIPackedScene;
    [Export]
    SubViewport SubViewport;
    VBoxContainer sideBar;
    public BoardUI boardUi;

    public enum PlayerType { Human, Bot };

    public enum GameState { Playing, WhiteIsMated, BlackIsMated, Stalemate, Repition, FiftyMove, InsufficientMaterial }



    public GameState gameState;
    PlayerType whitePlayerType;
    PlayerType blackPlayerType;
    PlayerType playerToMove;
    int colorToMove;
    public Board board;

    Random random;

    public string PositionFen;



    public override void _Ready()
    {
        boardUi = boardUIPackedScene.Instantiate() as BoardUI;
        sideBar = GetNode<VBoxContainer>("HBoxContainer/MarginContainer/SideBar");
        SubViewport.AddChild(boardUi);
        boardUi.MoveAttempted += OnMoveAttempted;
        foreach (Node child in GetNode<VBoxContainer>("HBoxContainer/MarginContainer/SideBar").GetChildren())
        {
            if (child is Button startGameButton)
            {
                startGameButton.Pressed += () => HandleStartButtonPressed(startGameButton);
            }
        }

        random = new Random();

        PositionFen = FenUtil.StartFen;
        board = new();
        board.LoadPosition(PositionFen);
        boardUi.UpdatePosition(board);
    }

    private void OnMoveAttempted(Vector2 from, Vector2 to)
    {
        if (playerToMove != PlayerType.Human || gameState != GameState.Playing)
            return;

        var legalMoves = board.GenerateLegalMoves();
        Move chosenMove;
        foreach (var move in legalMoves) {
            if (move.From == BoardRepresentation.IndexFromCoord((int)from.X, (int)from.Y) && move.To == BoardRepresentation.IndexFromCoord((int)to.X, (int)to.Y)) {
                chosenMove = move;
                board.MakeMove(chosenMove);
                boardUi.UpdatePosition(board);
                UpdateGameState();
                break;
            }
        }
        SwitchTurn();
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
        colorToMove = colorToMove == Chess.Piece.White ? Chess.Piece.Black : Chess.Piece.White;
        if (colorToMove == Chess.Piece.White && whitePlayerType == PlayerType.Bot) MakeAiMove();
        if (colorToMove == Chess.Piece.Black && blackPlayerType == PlayerType.Bot) MakeAiMove();

    }

    void HandleStartButtonPressed(Button button)
    {
        SetPlayers((int)button.GetMeta("GameModeCode"));
        NewGame();
    }

    void NewGame()
    {
        colorToMove = Chess.Piece.White;
        playerToMove = whitePlayerType;
        board = new();
        board.LoadPosition(PositionFen);
        boardUi.UpdatePosition(board);
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
        await Task.Delay(20);
        List<Move> moves = board.GenerateLegalMoves();



        Move move = moves[random.Next(moves.Count - 1)];
        board.MakeMove(move);
        boardUi.UpdatePosition(board);
        UpdateGameState();

        SwitchTurn();
    }

    void UpdateGameState()
    {
        var legalMoves = board.GenerateLegalMoves();
        if (legalMoves.Count == 0)
        {
            if (board.IsKingInCheck(colorToMove))
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
