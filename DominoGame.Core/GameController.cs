using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Enums;
using DominoGame.Core.Factories;

namespace DominoGame.Core
{
    public class GameController
    {
        private int _currentPlayerIndex;
        public Dictionary<IPlayer, List<Tile>> PlayerHands {get;} = new();
        public Dictionary<IPlayer, int> PlayerScores {get;} = new();
        public List<IPlayer> Players {get;} = new();
        public IPlayer? CurrentPlayer => Players.Count > 0 ? Players[_currentPlayerIndex] : null;
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public IBoard Board {get; private set;} = new DominoBoard();
        public IBoneyard Boneyard {get; private set;} = new Boneyard(new List<Tile>());
        public bool CanDraw {get; private set;}
        public bool CanPass {get; private set;}
        public int TargetScore {get; private set;}
        public int CurrentRound {get; private set;}
        public GameStatus Status {get; private set;} = GameStatus.NotStarted;
        public RoundStatus RoundStatus {get; private set;} = RoundStatus.NotStarted;
        public List<IPlayer> RoundWinners {get; set;} = new List<IPlayer>();
        public IPlayer? MatchWinner {get; private set;}

        public event Action<List<IPlayer>>? GameStarted;
        public event Action<IPlayer, Tile, BoardEnd>? TilePlayed;
        public event Action<IPlayer>? PlayerPassed;
        public event Action<IPlayer, int>? PlayerScored;
        public event Action<List<IPlayer>>? RoundOver;
        public event Action<IPlayer>? MatchOver;
        public event Action<IPlayer, Tile>? TileDrawn;


        public void StartNewGame(List<string> playerNames, int targetScore = 100)
        {
            // Input validation
            if(
                playerNames == null ||
                playerNames.Count < 2 || 
                playerNames.Count > 4 ||
                playerNames.Any(name => string.IsNullOrWhiteSpace(name)) ||
                targetScore <= 0)
            {
                return;
            }

            // clear state from previous game
            Players.Clear();
            PlayerHands.Clear();
            PlayerScores.Clear();
            RoundWinners.Clear();

            _currentPlayerIndex = 0;

            CanDraw = false;
            CanPass = false;
            
            MatchWinner = null;


            // Players initialization
            for(int i = 0; i < playerNames.Count; i++)
            {
                Player player = new Player(playerNames[i])
                {
                    Id = i + 1
                };

                Players.Add(player);
                PlayerHands.Add(player, new List<Tile>());
                PlayerScores.Add(player, 0);
            }

            // Prepare board, tiles, and boneyard
            Board = new DominoBoard();    
            List<Tile> tiles = DominoFactory.CreateDoubleSixSet();
            Boneyard = new Boneyard(tiles);
            ShuffleBoneyard(Boneyard);

            // Give tiles to each player
            int tilesPerPlayer = Players.Count == 4 ? 6 : 7;
            foreach(IPlayer player in Players)
            {
                for(int i = 0; i < tilesPerPlayer; i++)
                {
                    Tile tile = Boneyard.Tiles[0];

                    PlayerHands[player].Add(tile);
                    Boneyard.Tiles.RemoveAt(0);
                }
            }

            // Determine first player
            IPlayer firstPlayer = DetermineFirstPlayer(Players);
            _currentPlayerIndex = Players.IndexOf(firstPlayer);

            // Set initial game state
            TargetScore = targetScore;
            CurrentRound = 1;
            Status = GameStatus.InProgress;
            RoundStatus = RoundStatus.InProgress;

            // Set available actions for the first player
            bool hasMoves = HasValidMoves(firstPlayer);

            CanDraw = !hasMoves && !Boneyard.IsEmpty;
            CanPass = !hasMoves && Boneyard.IsEmpty;

            // Notify subscribers
            GameStarted?.Invoke(Players);
        }

        public void StartNewGame(List<IPlayer> playerNames)
        {
            // StartNewGame(playerNames, 100);
        }

        private void ShuffleBoneyard(IBoneyard boneyard)
        {
            Random random = Random.Shared;

            for(int i = boneyard.Tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (boneyard.Tiles[i], boneyard.Tiles[j]) = (boneyard.Tiles[j], boneyard.Tiles[i]);
            }
        }

        private Tile? PlayerGetHighestDouble(IPlayer player)
        {
            Tile? highestDouble = null;

            foreach(Tile tile in PlayerHands[player])
            {
                if (tile.IsDouble)
                {
                    if(highestDouble == null || tile.Left > highestDouble.Value.Left)
                    {
                        highestDouble = tile;
                    }
                }
            }

            return highestDouble;
        }

        private Tile? PlayerGetHighestTile(IPlayer player)
        {
            if (!PlayerHands.ContainsKey(player))
            {
                return null;
            }

            Tile? highestTile = null;

            foreach(Tile tile in PlayerHands[player])
            {
                if(highestTile == null || tile.Left + tile.Right > highestTile.Value.Left + highestTile.Value.Right)
                {
                    highestTile = tile;
                }
            }

            return highestTile;
        }

        public IPlayer DetermineFirstPlayer(List<IPlayer> players)
        {
            IPlayer? firstPlayer = null;
            int highestDoubleValue = -1;

            // find the player with highest double
            foreach(IPlayer player in players)
            {
                Tile? highestDouble = PlayerGetHighestDouble(player);

                if(highestDouble != null && highestDouble.Value.Left > highestDoubleValue)
                {
                    highestDoubleValue = highestDouble.Value.Left;
                    firstPlayer = player;
                }
            }

            // find player with highest tile if no player has highest double
            if(firstPlayer == null)
            {
                int highestTileValue = -1;

                foreach(IPlayer player in players)
                {
                    Tile? highestTile = PlayerGetHighestTile(player);

                    if(highestTile != null)
                    {
                        int tileValue = highestTile.Value.Left + highestTile.Value.Right;

                        if(tileValue > highestTileValue)
                        {
                            highestTileValue = tileValue;
                            firstPlayer = player;
                        }
                    }
                }
            }

            if(firstPlayer == null)
            {
                throw new InvalidOperationException("Cannot determine the first player.");
            }

            return firstPlayer;
        }

        private bool TileIsMatch(Tile tile, int targetValue)
        {
            return tile.Left == targetValue || tile.Right == targetValue;
        }

        public bool CanPlayTile(Tile tile)
        {
            if (Board.IsEmpty)
            {
                return true;
            }

            return 
                tile.Left == Board.LeftEndValue ||
                tile.Right == Board.LeftEndValue ||
                tile.Left == Board.RightEndValue ||
                tile.Right == Board.RightEndValue;
        }

        public bool CanPlayTile(int tileIndex)
        {
            IPlayer? player = CurrentPlayer;

            if(player == null || !PlayerHands.ContainsKey(player))
            {
                return false;
            }

            if(tileIndex < 0 || tileIndex >= PlayerHands[player].Count)
            {
                return false;
            }

            return CanPlayTile(PlayerHands[player][tileIndex]);
        }

        public bool HasValidMoves(IPlayer player)
        {
            if (Board.IsEmpty)
            {
                return true;
            }

            foreach(Tile tile in PlayerHands[player])
            {
                if (CanPlayTile(tile))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsValidMove(IPlayer player, Tile tile, BoardEnd end)
        {
            if(player == null || !PlayerHands.ContainsKey(player))
            {
                return false;
            }

            if(!PlayerHasTile(player, tile))
            {
                return false;
            }

            if (Board.IsEmpty)
            {
                return true;
            }

            return IsValidMove(tile, end);
        }

        public bool IsValidMove(int tileIndex, BoardEnd end)
        {
            IPlayer? player = CurrentPlayer;

            if(player == null || !PlayerHands.ContainsKey(player))
            {
                return false;
            }

            if(tileIndex < 0 || tileIndex >= PlayerHands[player].Count)
            {
                return false;
            }

            Tile tile = PlayerHands[player][tileIndex];

            return IsValidMove(player, tile, end);
        }

        public bool IsValidMove(Tile tile, BoardEnd end)
        {
            if (Board.IsEmpty)
            {
                return true;
            }

            int? targetValue = end == BoardEnd.Left ? Board.LeftEndValue : Board.RightEndValue;

            if(targetValue == null)
            {
                return false;
            }

            return TileIsMatch(tile, targetValue.Value);
        }

        private Tile TileFlip(Tile tile)
        {
            return new Tile(tile.Right, tile.Left);
        }

        private Tile TryMatchAndOrientTile(Tile tile, int targetValue, BoardEnd end)
        {
            if(!TileIsMatch(tile, targetValue))
            {
                throw new InvalidOperationException("Tile does not match the target value");
            }

            if(end == BoardEnd.Left)
            {
                if(tile.Right == targetValue)
                {
                    return tile;
                }

                return TileFlip(tile);
            }

            if(tile.Left == targetValue)
            {
                return tile;
            }

            return TileFlip(tile);
        }

        private bool ValidateBasicPlacement(Tile tile, BoardEnd end)
        {
            if (Board.IsEmpty)
            {
                return true;
            }

            int? targetValue = end == BoardEnd.Left ? Board.LeftEndValue : Board.RightEndValue;

            if(targetValue == null)
            {
                return false;
            }

            return TileIsMatch(tile, targetValue.Value);
        }

        private void PlaceTile(Tile tile, BoardEnd end)
        {
            if(!IsValidMove(tile, end))
            {
                throw new InvalidOperationException("Tile cannot be placed at the selected end");
            }

            if (Board.IsEmpty)
            {
                Board.PlayedTiles.Add(tile);
                return;
            }
            
            int targetValue = end == BoardEnd.Left ? Board.LeftEndValue!.Value : Board.RightEndValue!.Value;

            Tile orientedTile = TryMatchAndOrientTile(tile, targetValue, end);

            if(end == BoardEnd.Left)
            {
                Board.PlayedTiles.Insert(0, orientedTile);
            }
            else
            {
                Board.PlayedTiles.Add(orientedTile);
            }
        }

        private void AdvanceMove()
        {
            if(Players.Count == 0)
            {
                return;
            }

            _currentPlayerIndex = (_currentPlayerIndex + 1) % Players.Count;
        }

        public void PlayTurn(IPlayer player, Tile tile, BoardEnd end)
        {
            // Check whether the game is in progress
            if(Status != GameStatus.InProgress || RoundStatus != RoundStatus.InProgress)
            {
                return;
            }

            // Check whether it is this player's turn
            if(!Equals(player, CurrentPlayer))
            {
                return;
            }

            // Check whether the move is valid
            if(!IsValidMove(player, tile, end))
            {
                return;
            }

            // Place tile on the board
            PlaceTile(tile, end);

            // Remove tile from player's hand
            PlayerRemoveTile(player, tile);

            // Notify subscriber that the tile was played
            TilePlayed?.Invoke(player, tile, end);

            // Determine round winner
            List<IPlayer> winners = DetermineWinner();

            if(winners.Count > 0)
            {
                RoundWinners.Clear();
                RoundWinners.AddRange(winners);

                // Get all players who did not win
                List<IPlayer> losers = Players.Where(p => !RoundWinners.Contains(p)).ToList();
                
                // Calculate the round score
                int roundScore = CalculateRoundScore(losers);

                // Add points to each winner
                foreach(IPlayer winner in RoundWinners)
                {
                    PlayerScores[winner] += roundScore;

                    // Notify subscribers about the score
                    PlayerScored?.Invoke(winner, roundScore);
                }

                // Mark the round as complete
                RoundStatus = RoundStatus.Completed;

                // Check whether the match has winner
                IPlayer? matchWinner = DetermineMatchWinner();

                if(matchWinner != null)
                {
                    MatchWinner = matchWinner;
                    Status = GameStatus.Completed;
                }

                // Notify subscribers that the round is over
                RoundOver?.Invoke(RoundWinners);

                // Notify subscriber if the match is over
                if(matchWinner != null)
                {                 
                    MatchOver?.Invoke(matchWinner);
                }

                CanDraw = false;
                CanPass = false;

                return;
            }

            // No winner yet, advance to the next player
            AdvanceMove();

            // Determine available actions for the next player
            IPlayer? nextPlayer = CurrentPlayer;

            if(nextPlayer != null)
            {
                bool hasMoves = HasValidMoves(nextPlayer);

                CanDraw = !hasMoves && !Boneyard.IsEmpty;
                CanPass = !hasMoves && Boneyard.IsEmpty;
            }
            else
            {
                CanDraw = false;
                CanPass = false;
            }
        }

        public void PlayTurn(IPlayer player, int tileIndex, BoardEnd end)
        {
            if (!PlayerHands.ContainsKey(player))
            {
                return;
            }

            List<Tile> hand = PlayerHands[player];

            if(tileIndex < 0 || tileIndex >= hand.Count)
            {
                return;
            }

            Tile tile = hand[tileIndex];

            PlayTurn(player, tile, end);
        }

        public void PassTurn()
        {
            if(Status != GameStatus.InProgress || RoundStatus != RoundStatus.InProgress)
            {
                return;
            }

            IPlayer? player = CurrentPlayer;

            if(player == null)
            {
                return;
            }

            if(!CanPass || HasValidMoves(player))
            {
                return;
            }

            PlayerPassed?.Invoke(player);

            // Check whether the round has ended
            List<IPlayer> winners = DetermineWinner();

            if(winners.Count > 0)
            {
                RoundWinners.Clear();
                RoundWinners.AddRange(winners);

                List<IPlayer> losers = Players.Where(p => !RoundWinners.Contains(p)).ToList();

                int roundScore = CalculateRoundScore(losers);

                foreach(IPlayer winner in RoundWinners)
                {
                    PlayerScores[winner] += roundScore;
                    PlayerScored?.Invoke(winner, roundScore);
                }

                // Mark the round is complete
                RoundStatus = RoundStatus.Completed;

                // Check whether the match has winner
                IPlayer? matchWinner = DetermineMatchWinner();

                if(matchWinner != null)
                {
                    MatchWinner = matchWinner;
                    Status = GameStatus.Completed;
                }

                // Notify subscribers
                RoundOver?.Invoke(RoundWinners);

                if(matchWinner != null)
                {
                    MatchOver?.Invoke(matchWinner);
                }

                CanDraw = false;
                CanPass = false;

                return;
            }

            AdvanceMove();

            // Set the available actions for the next player
            IPlayer? nextPlayer = CurrentPlayer;

            if(nextPlayer != null)
            {
                bool hasMoves = HasValidMoves(nextPlayer);

                CanDraw = !hasMoves && !Boneyard.IsEmpty;
                CanPass = !hasMoves && Boneyard.IsEmpty;
            }
            else
            {
                CanDraw = false;
                CanPass = false;
            }
        }

        private bool PlayerHasTile(IPlayer player, Tile tile)
        {
            return PlayerHands.ContainsKey(player) && PlayerHands[player].Contains(tile);
        }

        private void PlayerRemoveTile(IPlayer player, Tile tile)
        {
            if(PlayerHasTile(player, tile))
            {
                PlayerHands[player].Remove(tile);
            }
        }

        private void PlayerAddTile(IPlayer player, Tile tile)
        {
            if (PlayerHands.ContainsKey(player))
            {
                PlayerHands[player].Add(tile);
            }
        }

        private Tile DrawFromBoneyard(Boneyard boneyard)
        {
            if (Boneyard.IsEmpty)
            {
                throw new InvalidOperationException("Cannot draw a tile from an empty boneyard.");
            }

            Tile tile = boneyard.Tiles[0];
            boneyard.Tiles.RemoveAt(0);

            return tile;
        }

        public Tile? DrawTile(IPlayer player)
        {
            if(Status != GameStatus.InProgress || RoundStatus != RoundStatus.InProgress)
            {
                return null;
            }

            if(!Equals(CurrentPlayer, player))
            {
                return null;
            }

            if (!CanDraw || Boneyard.IsEmpty)
            {
                return null;
            }

            Tile tile = DrawFromBoneyard((Boneyard) Boneyard);
            PlayerAddTile(player, tile);

            TileDrawn?.Invoke(player, tile);

            // Update available actions after drawing
            bool hasMoves = HasValidMoves(player);

            CanDraw = !hasMoves && !Boneyard.IsEmpty;
            CanPass = !hasMoves && Boneyard.IsEmpty;

            return tile;
        }

        private int PlayerTotalPips(IPlayer player)
        {
            if (!PlayerHands.ContainsKey(player))
            {
                return 0;
            }

            int total = 0;

            foreach(Tile tile in PlayerHands[player])
            {
                total += tile.Left + tile.Right;
            }

            return total;
        }

        public int CalculateRoundScore(List<IPlayer> losers)
        {
            int score = 0;

            foreach(IPlayer player in losers)
            {
                score += PlayerTotalPips(player);
            }

            return score;
        }

        public void StartNewRound()
        {
            if(Status != GameStatus.InProgress || RoundStatus != RoundStatus.Completed)
            {
                return;
            }

            // Clear the previous round's state
            PlayerHands.Clear();
            Board = new DominoBoard();

            // Reset round state
            RoundWinners.Clear();
            CanDraw = false;
            CanPass = false;

            // Initialize a new boneyard
            List<Tile> tiles = DominoFactory.CreateDoubleSixSet();
            Boneyard = new Boneyard(tiles);
            ShuffleBoneyard(Boneyard);

            // Initialize each player's hand
            foreach(IPlayer player in Players)
            {
                PlayerHands[player] = new List<Tile>();
            }

            // Give tiles to each player
            int tilesPerPlayer = Players.Count == 4 ? 6 : 7;

            foreach(IPlayer player in Players)
            {
                for(int i = 0; i < tilesPerPlayer; i++)
                {
                    Tile tile = Boneyard.Tiles[0];
                    PlayerHands[player].Add(tile);
                    Boneyard.Tiles.RemoveAt(0);
                }
            }

            // Determine the first player
            IPlayer firstPlayer = DetermineFirstPlayer(Players);
            _currentPlayerIndex = Players.IndexOf(firstPlayer);

            // Update round state
            CurrentRound++;
            RoundStatus = RoundStatus.InProgress;

            // Set available actions for the first player
            bool hasMoves = HasValidMoves(firstPlayer);

            CanDraw = !hasMoves && !Boneyard.IsEmpty;
            CanPass = !hasMoves && Boneyard.IsEmpty;
        }

        public List<IPlayer> DetermineWinner()
        {
            List<IPlayer> winners = new List<IPlayer>();

            // Check whether a player had emptied their hand
            foreach(IPlayer player in Players)
            {
                if(PlayerHands.ContainsKey(player) && PlayerHands[player].Count == 0)
                {
                    winners.Add(player);
                    return winners;
                }
            }

            // If the boneyard still has tile, the round cannot end due to blocked game
            if (!Boneyard.IsEmpty)
            {
                return winners;
            }

            // Check whether any player can still play a tile
            foreach(IPlayer player in Players)
            {
                if (HasValidMoves(player))
                {
                    return winners;
                }
            }

            // If the game is blocked, calculate each player's comparison score
            Dictionary<IPlayer, int> comparisonScores = new Dictionary<IPlayer, int>();

            foreach(IPlayer player in Players)
            {
                int playerPips = PlayerTotalPips(player);
                int comparisonScore = 0;

                foreach(IPlayer opponent in Players)
                {
                    if(!Equals(player, opponent))
                    {
                        comparisonScore += PlayerTotalPips(opponent) - playerPips;
                    }
                }

                comparisonScores[player] = comparisonScore;
            }

            // Find the highest comparison score
            int highestScore = int.MinValue;

            foreach(KeyValuePair<IPlayer, int> entry in comparisonScores)
            {
                if(entry.Value > highestScore)
                {
                    highestScore = entry.Value;
                }
            }

            // Include every player tied for the highest score
            foreach(KeyValuePair<IPlayer, int> entry in comparisonScores)
            {
                if(entry.Value == highestScore)
                {
                    winners.Add(entry.Key);
                }
            }

            return winners;
        }

        public IPlayer? DetermineMatchWinner()
        {
            foreach(IPlayer player in Players)
            {
                if(PlayerScores.ContainsKey(player) && PlayerScores[player] >= TargetScore)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
