using DominoGame.Core.Interfaces;

namespace DominoGame.Core.Models
{
    public class Player : IPlayer
    {
        public int Id {get;}
        public string Name {get; private set;}

        public Player(string name)
        {
            Name = name;
        }
    }
}