using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;

Console.WriteLine("=== DOMINO GAME ===");
Console.WriteLine();

Console.Write("Enter the number of players (2-4): ");
string? playerCountInput = Console.ReadLine();

if(!int.TryParse(playerCountInput, out int playerCount) || playerCount < 2 || playerCount> 4)
{
    Console.WriteLine("The number of player must be between 2 and 4");
    return;
}

List<string> playerNames = new List<string>();

for(int i = 0; i < playerCount; i++)
{
    Console.Write($"Enter the name of player {i + 1}: ");
    string? name = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("Player name cannot be empty.");
        return;
    }

    playerNames.Add(name.Trim());
}

Console.Write("Target Score (default target score is 100): ");
string? targetScoreInput = Console.ReadLine();

int targetScore = 100;

if (!string.IsNullOrWhiteSpace(targetScoreInput))
{
    if(!int.TryParse(targetScoreInput, out targetScore) || targetScore <= 0)
    {
        Console.WriteLine("The target score must be above 0");
        return;
    }
}

Console.WriteLine();
Console.WriteLine("=== GAME CONFIGURATION ===");
Console.WriteLine($"Number of players: {playerCount}");
Console.WriteLine($"Players: {string.Join(", ", playerNames)}");
Console.WriteLine($"Target score: {targetScore}");

Console.WriteLine();
Console.WriteLine("Starting game...");
Console.WriteLine();

GameController game = new GameController();
game.StartNewGame(playerNames, targetScore);

Console.WriteLine("=== GAME STARTED ===");
Console.WriteLine($"Round: {game.CurrentRound}");
Console.WriteLine($"Target score: {game.TargetScore}");
Console.WriteLine($"First player: {game.CurrentPlayer?.Name}");

Console.WriteLine();
Console.WriteLine("=== PLAYER HANDS ===");

foreach(IPlayer player in game.Players)
{
    Console.WriteLine();
    Console.WriteLine($"{player.Name}'s hand:");

    List<Tile> hand = game.PlayerHands[player];

    for(int i = 0; i < hand.Count; i++)
    {
        Console.WriteLine($"[{i}] {hand[i]}");
    }
}

Console.WriteLine();
Console.WriteLine("=== BOARD ===");

if (game.Board.IsEmpty)
{
    Console.WriteLine("Board is empty");
}
else
{
    Console.WriteLine(string.Join(" | ", game.Board.PlayedTiles));
}

Console.WriteLine();
Console.WriteLine($"Tiles remaining in Boneyard = {game.Boneyard.RemainingCount}");

while(game.Status == GameStatus.InProgress)
{
    IPlayer? currentPlayer = game.CurrentPlayer;

    if(currentPlayer == null)
    {
        Console.WriteLine("No current player is available");
        break;
    }

    Console.WriteLine();
    Console.WriteLine($"=== {currentPlayer.Name}'s TURN ===");
    Console.WriteLine($"Board: {string.Join(" | ", game.Board.PlayedTiles)}");
    Console.WriteLine();

    List<Tile> hand = game.PlayerHands[currentPlayer];

    Console.WriteLine("Your hand:");
    for(int i = 0; i < hand.Count; i++)
    {
        Console.WriteLine($"[{i}] {hand[i]}");
    }

    Console.WriteLine();
    Console.WriteLine("1. Play a tile");

    if (game.CanDraw)
    {
        Console.WriteLine("2. Draw a tile");
    }

    if (game.CanPass)
    {
        Console.WriteLine("3. Pass");
    }

    Console.Write("Choose an action: ");
    string? choice = Console.ReadLine();

    if(choice == "1")
    {
        Console.Write("Enter the index of the tile you wanna play: ");
        string? tileIndexInput = Console.ReadLine();

        if(!int.TryParse(tileIndexInput, out int tileIndex) || tileIndex < 0 || tileIndex >= hand.Count)
        {
            Console.WriteLine("Invalid tile index.");
            continue;
        }

        Tile selectedTile = hand[tileIndex];
        
        BoardEnd end = BoardEnd.Left;

        if (!game.Board.IsEmpty)
        {
            Console.WriteLine("1. Left end");
            Console.WriteLine("2. Right end");
            Console.Write("Choose the board end: ");

            string? endChoice = Console.ReadLine();

            if(endChoice == "1")
            {
                end = BoardEnd.Left;
            }
            else if(endChoice == "2")
            {
                end = BoardEnd.Right;
            }
            else
            {
                Console.WriteLine("Invalid board end");
                continue;
            }
        }

        if(!game.IsValidMove(currentPlayer, selectedTile, end))
        {
            Console.WriteLine("That tile cannot be played at the selected end");
            continue;
        }

        game.PlayTurn(currentPlayer, tileIndex, end);

        Console.WriteLine($"{currentPlayer.Name} played {selectedTile} on the {end.ToString().ToLower()} end.");
    }
    else if(choice == "2")
    {
        if (!game.CanDraw)
        {
            Console.WriteLine("You cannot draw a tile right now");
            continue;
        }

        Tile? drawnTile = game.DrawTile(currentPlayer);

        if(drawnTile == null)
        {
            Console.WriteLine("Unable to draw a tile");
            continue;
        }

        Console.WriteLine($"{currentPlayer.Name} drew a tile.");
    }
    else if(choice == "3")
    {
        if (!game.CanPass)
        {
            Console.WriteLine("You cannot pass right now");
            continue;
        }

        game.PassTurn();

        Console.WriteLine($"{currentPlayer.Name} passed");
    }
    else
    {
        Console.WriteLine("Invalid action. Please choose an available option.");
    }

    if(game.RoundStatus == RoundStatus.Completed)
    {
        Console.WriteLine();
        Console.WriteLine("=== ROUND OVER ===");

        foreach(IPlayer winner in game.RoundWinners)
        {
            Console.WriteLine($"{winner.Name} won the round!");
            Console.WriteLine($"Score: {game.PlayerScores[winner]}");
        }

        Console.WriteLine();
        Console.WriteLine("=== MATCH SCORES ===");

        foreach(IPlayer player in game.Players)
        {
            Console.WriteLine($"{player.Name}: {game.PlayerScores[player]} points");
        }

        if(game.Status == GameStatus.Completed)
        {
            Console.WriteLine();
            Console.WriteLine("=== MATCH OVER ===");
            Console.WriteLine($"{game.MatchWinner?.Name} won the match");
            break;
        }

        Console.WriteLine();
        Console.Write("Start the next round? (y/n): ");
        string? nextRoundChoice = Console.ReadLine();

        if(nextRoundChoice?.Trim().ToLower() == "y")
        {
            game.StartNewRound();

            Console.WriteLine();
            Console.WriteLine($"=== ROUND {game.CurrentRound} ===");
            Console.WriteLine($"First player: {game.CurrentPlayer?.Name}");
        }
        else
        {
            Console.WriteLine("Game session ended.");
            break;
        }
    }
}