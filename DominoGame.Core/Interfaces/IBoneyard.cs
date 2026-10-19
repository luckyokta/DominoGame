using DominoGame.Core.Models;

namespace DominoGame.Core.Interfaces
{
    public interface IBoneyard
    {
        List<Tile> Tiles {get;}
        bool IsEmpty {get;}
        int RemainingCount {get;}
    }
}