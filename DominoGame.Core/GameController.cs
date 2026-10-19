using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Enums;
using DominoGame.Core.Factories;

namespace DominoGame.Core
{
    public class GameController
    {
        int _currentPlayerIndex;
        public Dictionary<IPlayer, List<Tile>> PlayerHands {get;} = new();
        public Dictionary<IPlayer, int> PlayerScores {get;} = new();
        public List<IPlayer> Players {get;} = new();
        public IPlayer? CurrentPlayer => Players.Count > 0 ? Players[_currentPlayerIndex] : null;
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public IBoard Board {get; private set;} = new DominoBoard();
        public IBoneyard Boneyard {get; private set;} = new Boneyard();
        public bool CanDraw => !Boneyard.IsEmpty;
        public bool CanPass {get; private set;}
        public int TargetScore {get; private set;}
        public int CurrentRound {get; private set;}
        public GameStatus Status {get; private set;} = GameStatus.NotStarted;
        public RoundStatus RoundStatus {get; private set;} = RoundStatus.NotStarted;
        public IPlayer? RoundWinner {get; private set;}
        public IPlayer? MatchWinner {get; private set;}

        public void StartNewGame(List<string> playerNames, int targetScore)
        {
            if(playerNames == null || playerNames.Count < 2 || playerNames.Count > 4)
            {
                return;
            }

            if(playerNames.Any(name => string.IsNullOrWhiteSpace(name)))
            {
                return;
            }

            if(targetScore <= 0)
            {
                return;
            }

            Players.Clear();
            PlayerHands.Clear();
            PlayerScores.Clear();

            foreach(string playerName in playerNames)
            {
                Player player = new Player(playerName);

                Players.Add(player);
                PlayerHands.Add(player, new List<Tile>());
                PlayerScores.Add(player, 0);
            }
        }
    }
}
