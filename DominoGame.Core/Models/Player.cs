using DominoGame.Core.Interfaces;

namespace DominoGame.Core.Models
{
    public class Player : IPlayer
    {
        public int Id {get; internal set;}
        public string Name {get;}

        public Player(string name)
        {
            Name = name;
        }
    }
}