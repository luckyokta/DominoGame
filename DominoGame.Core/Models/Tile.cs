namespace DominoGame.Core.Models
{
    public struct Tile
    {
        public int Left {get;}
        public int Right {get;}
        public bool IsDouble => Left == Right;

        public Tile(int left, int right)
        {
            Left = left;
            Right = right;
        }
        
        public override string ToString()
        {
            return $"[{Left}|{Right}]";
        }
    }
}