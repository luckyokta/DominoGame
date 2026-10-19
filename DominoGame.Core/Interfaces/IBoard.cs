using System.Runtime.CompilerServices;
using DominoGame.Core.Models;

namespace DominoGame.Core.Interfaces
{
    public interface IBoard
    {
        List<Tile> PlayedTiles {get;}
        int? LeftEndValue {get;}
        int? RightEndValue {get;}
        bool IsEmpty {get;}
    }
}