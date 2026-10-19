// See https://aka.ms/new-console-template for more information
using DominoGame.Core.Models;
using DominoGame.Core.Factories;

Tile tile1 = new Tile(3, 5);
Tile tile2 = new Tile(4, 4);
Tile tile3 = new Tile(9, 5);

Console.WriteLine($"Tile 1: {tile1}");
Console.WriteLine(tile1.Left);
Console.WriteLine(tile1.Right);
Console.WriteLine(tile2.IsDouble);
Console.WriteLine($"Tile 3: {tile3}");

var board = new DominoBoard();

Console.WriteLine(board.IsEmpty);
Console.WriteLine(board.PlayedTiles);
Console.WriteLine($"Board: {board}");

var boneyard = new Boneyard();
Console.WriteLine(boneyard.IsEmpty);
boneyard.Tiles.Add(new Tile(2, 3));
Console.WriteLine(boneyard.IsEmpty);
Console.WriteLine(boneyard.RemainingCount);

var player1 = new Player("A");
Console.WriteLine(player1.Name);

List<Tile> tiles = DominoFactory.CreateDoubleSixSet();
foreach(Tile tile in tiles)
{
    Console.WriteLine(tile);
}
Console.WriteLine($"Total tile = {tiles.Count}");