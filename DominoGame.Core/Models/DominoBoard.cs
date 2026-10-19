using DominoGame.Core.Interfaces;

namespace DominoGame.Core.Models
{
    public class DominoBoard : IBoard
    {
        public List<Tile> PlayedTiles {get;} = new();
        public int? LeftEndValue => IsEmpty ? null : PlayedTiles[0].Left;
        public int? RightEndValue => IsEmpty ? null : PlayedTiles[^1].Right;
        public bool IsEmpty => PlayedTiles.Count == 0;

        public override string ToString()
        {
            return string.Join("-", PlayedTiles);
        }
    }
}