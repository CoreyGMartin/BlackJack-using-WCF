using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.ServiceModel;

namespace _21Library {
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
	public class BlackJackTable : IBlackJackTable, IUsersTable {
		//Fields
		private const int numberOfDecks = 5;
		private const double payoutRate = 1.5; // 3-to-2

		private Random random;
		private Dictionary<string, IPlayerCallBack> playerCallBacks;
		private List<string> messages = new List<string>();
		private List<Card> shoe;
		private Dealer dealer;
		private Player[] players;
		private List<Player> playersWaitingToPlay;

		//Constructor
		public BlackJackTable() {
			players = new Player[5];
			playersWaitingToPlay = new List<Player>(5);
			dealer = new Dealer();
			dealer.HasGameStarted = false;
			random = new Random();
			shoe = new List<Card>(numberOfDecks * 52);
			playerCallBacks = new Dictionary<string, IPlayerCallBack>();
		}

		//Public Methods
		public Status JoinTable(string name) {
			//Check how many people are in the game
			if (playerCallBacks.Count > 4) {
				return Status.GameFull;
			//Check if a player exists with that name
			} else if (playerCallBacks.ContainsKey(name.ToUpper())) {
				return Status.NameTaken;
			} else {
				return Status.Success;
			}
		}
		public void StartGame() {
			if (!dealer.HasGameStarted) {
				dealer.HasGameStarted = true;
				FillShoeAndShuffle();
				DealHand(false);
				UpdateAllPlayers(true, true, new string[] { "New hand has started" });
			}
		}
		public string AddPlayer(string name) {
			//playersAtTable
			string status = "";
			string[] messages = new string[1];

			if (playerCallBacks.ContainsKey(name.ToUpper())) {
				status = "nameTaken";
			} else if (playersWaitingToPlay.Count + players.Count(p => p != null) == 5) {
				status = "gameFull";
			} else  { 
				//Add player to Call back dictionary.
				IPlayerCallBack playerCallBack = OperationContext.Current.GetCallbackChannel<IPlayerCallBack>();
				playerCallBacks.Add(name.ToUpper(), playerCallBack);

				//Random seat.
				Player player = new Player(name);
				if (dealer.HasGameStarted) {
					playersWaitingToPlay.Add(player);
					messages[0] = name + " has joined the game and is waiting to play";
				} else {
					//Create list of available seats.
					List<int> availableSeats = new List<int>();
					for (int i = 0; i < players.Length; ++i)
						if (players[i] == null)
							availableSeats.Add(i);

					//Check if it is the first person to start the game
					if (availableSeats.Count == 5)
						player.CanStartGame = true;
						 
					int seat = availableSeats[random.Next(availableSeats.Count)];
					players[seat] = player;

					messages[0] = name + " has joined the game";
				}
				status = "success";
			}

			UpdateAllPlayers(true, true, messages);

			return status;
		}
		public void RemovePlayer(string name) {
			if (playerCallBacks.ContainsKey(name.ToUpper())) {
				//Remover player from call back dictionary.
				playerCallBacks.Remove(name.ToUpper());

				//Inform all the players.
				foreach (var playerCallBack in playerCallBacks)
					playerCallBack.Value.UpdatePlayersWithMessage(new string[] { name + " has left the game." });

				//Check if the player was in the player array (playing the game)
				for (int i = 0; i < players.Length; ++i) {
					if (players[i] != null) {
						if (players[i].Name == name) {
							//Check if the person who left was able to start the game.
							if (players[i].CanStartGame) {
								//Check if anybody else is in the game.
								if (players.Any(p => p != null))
									//Give the first person in the array ability to start the game
									players.First(p => p != null).CanStartGame = true;
								else
									//Nobody is at the table
									dealer.HasGameStarted = false;
							}

							//Check if it was the turn of the player who left.
							//Remove player from player array and update clients.
							if (players[i].IsItMyTurn) {
								NextTurn(true);
							} else {
								players[i] = null;
								UpdateAllPlayers(true, true, null);
							}

							break;
						}
					}
				}
			}
		}
		public void SetPlayersTurnBet(int bet) {
			//Set player bet.
			Player player = players.First(p => p != null && p.IsItMyTurn == true);
			player.Bet = bet;
			player.Money -= bet;

			//Inform all clients of the changes.
			UpdateAllPlayers(true, true, new string[] { });
		}
		public void DealCardToTurnPlayer() {
			Player player = players.First(p => p != null && p.IsItMyTurn == true);
			Card card = null;
			string[] messages = null;
			for (int i = 0; i < player.Cards.Length; ++i)
				if (player.Cards[i] == null) {
					card = DealOneCard();
					player.Cards[i] = card;
					break;
				}

			//Check if busted
			if (player.HasBusted()) {
				messages = new string[2];
				messages[1] = player.Name + " busts";
			} else {
				messages = new string[1];
			}
			messages[0] = messages[0] = player.Name + " was dealt " + card.ToString();

			UpdateAllPlayers(true, true, messages);


			if (player.HasBusted()) {
				//delay for real users to notice graphical difference.
				Thread.Sleep(2000);
				NextTurn(false);
			}
		}
		public void NextTurn(bool removeCurrentTurnPlayer) {
			bool dealersTurn = true;

			//Advance Player turn.
			bool lookingForNextTurnPlayer = true;
			for (int i = 0; i < players.Length && lookingForNextTurnPlayer; ++i) {
				if (players[i] != null && players[i].IsItMyTurn) {
					players[i].IsItMyTurn = false;
					if (removeCurrentTurnPlayer)
						players[i] = null;
					for (int ii = i + 1; ii < players.Length && lookingForNextTurnPlayer; ++ii)
						if (players[ii] != null) {
							players[ii].IsItMyTurn = true;
							lookingForNextTurnPlayer = false;
							dealersTurn = false;
							UpdateAllPlayers(true, true, null);
						}
				}
			}

			//Check if it is the dealers turn.
			//if (dealersTurn && players.Count(p => p != null) != 0) {
			if (dealersTurn) {
				StartDealersTurn();
			}

			//Check if any players are left
			if (players.Count(p => p != null) == 0)  {
				dealer.HasGameStarted = false;
				//No players in game check for new players.
				if (playersWaitingToPlay.Count > 0) {
					//Give the first person who joined the game the ability to start the game
					playersWaitingToPlay.First().CanStartGame = true;
					dealer.HasGameStarted = false;
				} 
			}
		}

		//Private Methods
		private void StartDealersTurn() {
			//--------Dealers turn--------
			bool handOver = false;
			string dealersDecision = null;

			//Show dealer's hidden card and update clients.
			dealer.FirstCard = Visibility.Visible;
			UpdateAllPlayers(false, true, new string[] { "Dealer reveals " + dealer.GetCardNameByIndex(0) });
			Thread.Sleep(3000);

			//Deal cards to Dealer and analyze outcome.
			for (int i = 2; !handOver; ++i) {
				string dealersCard = null;

				//Get a list of active players who haven't busted.
				var playersExistAndAlive = players.Where(p => (p != null && p.CardTotal <= 21));
				//Check how many players the dealer has beat.
				int playersBeat = playersExistAndAlive.Where(p => (dealer.CardTotal >= p.CardTotal)).Count();

				//Decide if dealer should stay and payout or attempt to beat more players.
				if (playersExistAndAlive.Count() == 0) {
					//Everybody has busted. Dealer has won, no need to deal any cards to the dealer.
					handOver = true;
					dealersDecision = "Dealer stays at " + dealer.CardTotal;
				} else if (playersBeat != 0) {
					double beatPercentage = (((double)playersBeat) / playersExistAndAlive.Count()) * 100.0;
					if (beatPercentage >= 50.0) {
						//Dealer has beat at least 50% of alive players. Good Enough.
						handOver = true;
						dealersDecision = "Dealer stays at " + dealer.CardTotal;
					}
				}

				//Deal a card to the dealer if the hand isn't over (hasn't beat 50% of players or if the dealer busted).
				if (!handOver) {
					//Deal card
					Card card = DealOneCard();
					dealer.Cards[i] = card;
					dealersCard = "Dealer is dealt " + card.ToString();

					//Check if dealer has busted
					if (dealer.HasBusted()) {
						handOver = true;
						dealersDecision = "Dealer busts and losses";
					}
				}

				UpdateAllPlayers(false, true, new string[] { dealersCard, dealersDecision });

				//Delay for users to notice graphical update.
				Thread.Sleep(2000);
			}

			//Shows graphical view of the won and lost bets.
			CalculateWinnings();

			//Delay for users to notice graphical update.
			Thread.Sleep(3000);

			//Start a new hand
			DealHand(true);

			//Inform clients.
			UpdateAllPlayers(true, true, new string[] { "New hand has started" });
		}
		private Tuple<Player[], Dealer> GetAllPlayersAndDealer() { return Tuple.Create(players, dealer); }
		private void DealHand(bool isContinuousHand) {
			//Clear previous cards of all players and dealer
			if (isContinuousHand) {
				if (playersWaitingToPlay.Any()) {
					//Create list of available seats.
					List<int> availableSeats = new List<int>();
					for (int i = 0; i < players.Length; ++i)
						if (players[i] == null)
							availableSeats.Add(i);

					//Add Players
					foreach (Player player in playersWaitingToPlay) {
						int seat = availableSeats[random.Next(availableSeats.Count)];
						players[seat] = player;
						availableSeats.Remove(seat);
					}
					//Remove all players waiting to play
					playersWaitingToPlay.Clear();
				}

				foreach (Player player in players)
					if (player != null)
						player.ResetHand();
				dealer.ResetHand();
			}

			//Re-Fill and Re-Shuffle shoe at 50% of cards left.
			if (shoe.Count < (numberOfDecks / 2))
				FillShoeAndShuffle();

			dealer.Cards[0] = DealOneCard();
			dealer.Cards[1] = DealOneCard();

			//For every player deal two cards
			foreach (Player player in players) {
				if (player != null) {
					player.Cards[0] = DealOneCard();
					player.Cards[1] = DealOneCard();
				}
			}

			//Find first open seat
			for (int i = 0; i < players.Length; ++i) {
				if (players[i] != null) {
					players[i].IsItMyTurn = true;
					break;
				}
			}
		}
		private void CalculateWinnings() {
			//List of players
			Player[] alivePlayers = players.Where(p => p != null).ToArray();
			string[] playersWinning = new string[alivePlayers.Length];

			for (int i = 0; i < alivePlayers.Length; ++i) {
				Player player = alivePlayers[i];
				//Lost to Dealer
				if (!dealer.HasBusted() && dealer.CardTotal >= alivePlayers[i].CardTotal) {
					player.LostBet();
					playersWinning[i] = player.Name + " lost " + (player.AmountWonLost * -1).ToString("C");
				} else {
					//Check if player busted
					if (player.HasBusted()) {
						player.LostBet();
						playersWinning[i] = player.Name + " lost " + (player.AmountWonLost * -1).ToString("C");
					} else {
						//Won against dealer
						player.WonBet(payoutRate);
						playersWinning[i] = player.Name + " won " + (player.AmountWonLost).ToString("C");
					}
				}
			}

			//Check if any player has ran out of money (minimum bet is 50)
			for (int i = 0; i < players.Length; ++i) {
				if (players[i] != null && players[i].Money < 50) {
					//Kick player
					string key = players[i].Name.ToUpper();

					//Surround with a try/catch incase user rage quits.
					try {
						playerCallBacks[key].UpdatePlayerWithMessage("kicked");
					} catch (Exception ex) {
						//No need to fix this, player was going to be removed anyways.
						Console.WriteLine(ex.Message);
					}

					playerCallBacks.Remove(key);
					players[i] = null;
				}
			}

			UpdateAllPlayers(true, true, playersWinning);

			//Reset everything bet related
			foreach (Player player in alivePlayers)
				player.ClearBet();
		}
		private Card DealOneCard() {
			Card card = shoe[0];
			shoe.Remove(card);
			return card;
		}
		private void FillShoeAndShuffle() {
			//Clear shoe
			shoe.Clear();

			//Fill Deck
			for (int deck = 0; deck < numberOfDecks; ++deck)
				for (int suit = 0; suit < 4; ++suit)
					for (int value = 0; value < 13; ++value)
						shoe.Add(new Card((Suit)suit, (Value)value));

			//Shuffle
			int card = shoe.Count;
			while (card > 1) {
				card--;
				int k = random.Next(card + 1);
				Card value = shoe[k];
				shoe[k] = shoe[card];
				shoe[card] = value;
			}
		}
		private void UpdateAllPlayers(bool updatePlayers, bool updateDealer, string[] messages) {
			//Inform all clients of the changes.
			try {
					//Send Players, Dealer Data and messages
				if (updatePlayers && updateDealer) {
					foreach (var playerCallBack in playerCallBacks)
						playerCallBack.Value.UpdatePlayersAndDealer(GetAllPlayersAndDealer(), messages);
					//Send Dealer Data and messages
				} else if (!updatePlayers && updateDealer) {
					foreach (var playerCallBack in playerCallBacks)
						playerCallBack.Value.UpdateDealer(dealer, messages);
					//Send messages
				} else if (!updatePlayers && !updateDealer) {
					foreach (var playerCallBack in playerCallBacks)
						playerCallBack.Value.UpdatePlayersWithMessage(messages);
				}
			} catch (Exception ex) {
				//Incase a client gets disconnected/leaves while the service is updating all the clients.
				//Once this method returns the hand cycle will fix this using the RemovePlayer Method above.
				Console.WriteLine(ex.Message);
			}
		}
	}
}