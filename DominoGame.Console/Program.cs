using Spectre.Console;
using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;

namespace DominoGame.ConsoleApp;

internal class Program
{
    private static readonly GameController Controller = new();

    static void Main(string[] args)
    {
        bool exit = false;

        while (!exit)
        {
            AnsiConsole.Clear();

            AnsiConsole.Write(
                new FigletText("DOMINO GAME")
                    .Centered()
                    .Color(Color.Blue));

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Main Menu[/]")
                    .AddChoices(
                        "Start New Game",
                        "Exit"));

            switch (choice)
            {
                case "Start New Game":
                    StartGame();
                    break;

                case "Exit":
                    exit = true;
                    break;
            }
        }

        AnsiConsole.MarkupLine("[grey]Thanks for playing![/]");
    }

    
    private static void StartGame()
    {
        AnsiConsole.Clear();

        AnsiConsole.MarkupLine("[bold blue]NEW GAME[/]");
        AnsiConsole.WriteLine();

        int playerCount = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("How many players?")
                .AddChoices(2, 3, 4));

        List<string> playerNames = new();

        for (int i = 1; i <= playerCount; i++)
        {
            string name = AnsiConsole.Prompt(
                new TextPrompt<string>($"Enter name for player {i}:")
                    .Validate(name =>
                        !string.IsNullOrWhiteSpace(name)
                            ? ValidationResult.Success()
                            : ValidationResult.Error(
                                "[red]Name cannot be empty.[/]")));

            playerNames.Add(name.Trim());
        }

        int targetScore = AnsiConsole.Prompt(
            new TextPrompt<int>("Enter the target score:")
                .DefaultValue(100)
                .Validate(score =>
                    score > 0
                        ? ValidationResult.Success()
                        : ValidationResult.Error(
                            "[red]Target score must be greater than zero.[/]")));

        Controller.StartNewGame(playerNames, targetScore);

        PlayGame();
    }

    private static void PlayGame()
    {
        while (Controller.Status == GameStatus.InProgress)
        {
            PlayRound();

            if (Controller.Status == GameStatus.Completed)
            {
                ShowMatchResult();
                return;
            }

            if (Controller.RoundStatus == RoundStatus.Completed)
            {
                ShowRoundResult();

                AnsiConsole.MarkupLine(
                    $"[grey]Starting round {Controller.CurrentRound + 1}...[/]");

                AnsiConsole.Prompt(
                    new TextPrompt<string>(
                        "Press Enter to continue")
                        .AllowEmpty());

                Controller.StartNewRound();
            }
        }
    }

    private static void PlayRound()
    {
        while (
            Controller.Status == GameStatus.InProgress &&
            Controller.RoundStatus == RoundStatus.InProgress)
        {
            AnsiConsole.Clear();

            ShowGameState();

            IPlayer? currentPlayer = Controller.CurrentPlayer;

            if (currentPlayer == null)
            {
                AnsiConsole.MarkupLine(
                    "[red]Unable to determine the current player.[/]");

                return;
            }

            AnsiConsole.MarkupLine(
                $"[bold yellow]{Markup.Escape(currentPlayer.Name)}'s turn[/]");

            List<Tile> hand = Controller.PlayerHands[currentPlayer];

            ShowPlayerHand(hand);

            List<string> actions = new()
            {
                "Play a tile"
            };

            if (Controller.CanDraw)
            {
                actions.Add("Draw a tile");
            }

            if (Controller.CanPass)
            {
                actions.Add("Pass turn");
            }

            string action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an action:")
                    .AddChoices(actions));

            switch (action)
            {
                case "Play a tile":
                    PlayTile(currentPlayer, hand);
                    break;

                case "Draw a tile":
                    Controller.DrawTile(currentPlayer);
                    break;

                case "Pass turn":
                    Controller.PassTurn();
                    break;
            }
        }
    }

    private static void ShowGameState()
    {
        AnsiConsole.Write(
            new Panel(
                $"[bold]Round:[/] {Controller.CurrentRound}\n" +
                $"[bold]Target score:[/] {Controller.TargetScore}")
                .Header("Game Information")
                .BorderColor(Color.Blue));

        AnsiConsole.WriteLine();

        Table scoreTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Player")
            .AddColumn(new TableColumn("Score").RightAligned());

        foreach (IPlayer player in Controller.Players)
        {
            scoreTable.AddRow(
                Markup.Escape(player.Name),
                Controller.PlayerScores[player].ToString());
        }

        AnsiConsole.Write(scoreTable);

        AnsiConsole.WriteLine();

        string boardText;

        if (Controller.Board.PlayedTiles.Count == 0)
        {
            boardText = "[grey]The board is empty.[/]";
        }
        else
        {
            boardText = string.Join(
                "  ",
                Controller.Board.PlayedTiles.Select(
                    tile => Markup.Escape($"[{tile.Left}|{tile.Right}]")));
        }

        AnsiConsole.Write(
            new Panel(boardText)
                .Header("Domino Board")
                .BorderColor(Color.Green));

        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine(
            $"[grey]Tiles remaining in boneyard: " +
            $"{Controller.Boneyard.Tiles.Count}[/]");

        AnsiConsole.WriteLine();
    }

    private static void ShowPlayerHand(List<Tile> hand)
    {
        Table handTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("#")
            .AddColumn("Tile");

        for (int i = 0; i < hand.Count; i++)
        {
            Tile tile = hand[i];

            handTable.AddRow(
                (i + 1).ToString(),
                Markup.Escape($"[{tile.Left}|{tile.Right}]"));
        }

        AnsiConsole.Write(
            new Panel(handTable)
                .Header("Your Hand")
                .BorderColor(Color.Yellow));
    }

    private static void PlayTile(IPlayer player, List<Tile> hand)
    {
        if (hand.Count == 0)
        {
            return;
        }

        int tileNumber = AnsiConsole.Prompt(
            new TextPrompt<int>("Choose a tile number:")
                .Validate(number =>
                    number >= 1 && number <= hand.Count
                        ? ValidationResult.Success()
                        : ValidationResult.Error(
                            $"[red]Enter a number from 1 to {hand.Count}.[/]")));

        Tile selectedTile = hand[tileNumber - 1];

        BoardEnd end = BoardEnd.Left;

        if (Controller.Board.PlayedTiles.Count > 0)
        {
            string selectedEnd = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which end of the board?")
                    .AddChoices("Left", "Right"));

            end = selectedEnd == "Left"
                ? BoardEnd.Left
                : BoardEnd.Right;
        }

        int previousTileCount = hand.Count;

        Controller.PlayTurn(player, selectedTile, end);

        if (hand.Count == previousTileCount)
        {
            AnsiConsole.MarkupLine(
                "[red]Invalid move. Please try again.[/]");

            AnsiConsole.Prompt(
                new TextPrompt<string>("Press Enter to continue")
                    .AllowEmpty());
        }
    }

    private static void ShowRoundResult()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new Rule("[bold yellow]Round Result[/]")
                .RuleStyle("yellow"));

        if (Controller.RoundWinner == null)
        {
            AnsiConsole.MarkupLine(
                "[yellow]The round ended in a tie. No points awarded.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"[green]Round winner: " +
                $"{Markup.Escape(Controller.RoundWinner.Name)}[/]");
        }

        AnsiConsole.WriteLine();

        ShowScores();
    }

    private static void ShowMatchResult()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new Rule("[bold green]Match Completed[/]")
                .RuleStyle("green"));

        if (Controller.MatchWinner != null)
        {
            AnsiConsole.Write(
                new FigletText(Controller.MatchWinner.Name)
                    .Centered()
                    .Color(Color.Green));

            AnsiConsole.MarkupLine(
                "[bold green]Congratulations! You won the match![/]");
        }

        AnsiConsole.WriteLine();

        ShowScores();

        AnsiConsole.Prompt(
            new TextPrompt<string>("Press Enter to return to the main menu")
                .AllowEmpty());
    }

    private static void ShowScores()
    {
        Table scoreTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Player")
            .AddColumn(new TableColumn("Score").RightAligned());

        foreach (IPlayer player in Controller.Players)
        {
            scoreTable.AddRow(
                Markup.Escape(player.Name),
                Controller.PlayerScores[player].ToString());
        }

        AnsiConsole.Write(scoreTable);
    }
}
