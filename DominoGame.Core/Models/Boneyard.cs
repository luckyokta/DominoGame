using System.Collections;
using DominoGame.Core.Interfaces;

namespace DominoGame.Core.Models
{
    public class Boneyard : IBoneyard
    {
        public List<Tile> Tiles {get;} = new();
        public bool IsEmpty => Tiles.Count == 0;
        public int RemainingCount => Tiles.Count;
    }
}