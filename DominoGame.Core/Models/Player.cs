using DominoGame.Core.Interfaces;

namespace DominoGame.Core.Models
{
    public class Player : IPlayer
    {
        public int Id {get; set;}
        public string Name {get; set;}

        public Player(string name)
        {
            Name = name;
        }
    }
}