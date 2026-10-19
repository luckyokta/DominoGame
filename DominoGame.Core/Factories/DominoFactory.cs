using DominoGame.Core.Models;

namespace DominoGame.Core.Factories
{
    public static class DominoFactory
    {
        public static List<Tile> CreateDoubleSixSet()
        {
            List<Tile> tiles = new();

            for(int left = 0; left <= 6; left++)
            {
                for(int right = left; right <= 6; right++)
                {
                    tiles.Add(new Tile(left, right));
                }
            }

            return tiles;
        }
    }
}