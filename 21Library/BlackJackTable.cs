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
	public class BlackJackTable : IBlackJackTable {
		//Fields
		private bool hasGameStarted;
		private Random random;
		private Dictionary<string, IPlayerCallBack> playerCallBacks;
		private List<string> messages = new List<string>();
		private List<Card> shoe;
		private const int numberOfDecks = 5;
		private double payoutRate = 1.5; // 3-to-2
		private Dealer dealer;
		private Player[] players;
		private List<Player> playersWaitingToPlay;

		//Constructor
		public BlackJackTable() {
			players = new Player[5];
			playersWaitingToPlay = new List<Player>(5);
			dealer = new Dealer();
			random = new Random();
			shoe = new List<Card>(numberOfDecks * 52);
			playerCallBacks = new Dictionary<string, IPlayerCallBack>();

			FillShoeAndShuffle();
			CheckForPlayers();
		}

		//Public Methods
		public bool AddPlayer(string name) {
			bool success = false;

			if (!playerCallBacks.ContainsKey(name.ToUpper())) {
				//Add player to Call back dictionary.
				IPlayerCallBack playerCallBack = OperationContext.Current.GetCallbackChannel<IPlayerCallBack>();
				playerCallBacks.Add(name.ToUpper(), playerCallBack);

				//Random seat.
				Player player = new Player(name);
				if (hasGameStarted) {
					playersWaitingToPlay.Add(player);
					playerCallBack.UpdateGUI(GetAllPlayersAndDealer(), null);
				} else {
					//Create list of available seats.
					List<int> availableSeats = new List<int>();
					for (int i = 0; i < players.Length; ++i)
						if (players[i] == null)
							availableSeats.Add(i);

					int seat = availableSeats[random.Next(availableSeats.Count)];
					players[seat] = player;
				}

				success = true;
			}

			return success;
		}
		public void RemovePlayer(string name) {
			if (playerCallBacks.ContainsKey(name.ToUpper())) {
				playerCallBacks.Remove(name.ToUpper());
				for (int i = 0; i < players.Length; ++i) {
					if (players[i] != null)
						if (players[i].Name == name) {
							players[i] = null;
							break;
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
				NextTurn();
			}
		}
		public void NextTurn() {
			bool dealersTurn = true;

			//Advance Player turn.
			bool lookingForNextTurnPlayer = true;
			for (int i = 0; i < players.Length && lookingForNextTurnPlayer; ++i)
				if (players[i] != null && players[i].IsItMyTurn) {
					players[i].IsItMyTurn = false;
					for (int ii = i + 1; ii < players.Length && lookingForNextTurnPlayer; ++ii)
						if (players[ii] != null) {
							players[ii].IsItMyTurn = true;
							lookingForNextTurnPlayer = false;
							dealersTurn = false;
							UpdateAllPlayers(true, true, null);
						}
				}

			int playersatTable = players.Count(p => p != null);
			if (dealersTurn && playersatTable != 0) {
				//--------Dealers turn--------
				bool handOver = false;
				string dealersDecision = "";
				//Show dealer's hidden card.
				dealer.FirstCard = Visibility.Visible;
				UpdateAllPlayers(false, true, new string[] { "Dealer reveals " + dealer.GetCardNameByIndex(0) });
				Thread.Sleep(3000);

				//Deal cards to Dealer and analyze outcome.
				for (int i = 2; !handOver; ++i) {
					//Get a list of active players who haven't busted.
					var playersExistAndAlive = players.Where(p => (p != null && p.CardTotal <= 21));
					//Check how many players the dealer has beat.
					int playersBeat = playersExistAndAlive.Where(p => (dealer.CardTotal >= p.CardTotal)).Count();

					//Decide if dealer should stay and payout or attempt to beat more players.
					if (playersExistAndAlive.Count() == 0) {
						//Everybody has busted. Dealer has won, no need to deal any cards to the dealer.
						handOver = true;
						dealersDecision = "Dealer stays";
					} else if (playersBeat != 0) {
						double beatPercentage = (((double)playersBeat) / playersExistAndAlive.Count()) * 100.0;
						if (beatPercentage >= 50.0) {
							//Dealer has beat at least 50% of alive players. Good Enough.
							handOver = true;
							dealersDecision = "Dealer stays";
						}
					}

					//Deal a card to the deal if the hand isn't over.
					if (!handOver) {
						//Dealer hasn't beat 50% of alive players, deal more cards to Dealer.
						//Deal card
						Card card = DealOneCard();
						dealer.Cards[i] = card;
						dealersDecision = "Dealer takes " + card.ToString();

						//Check if dealer has busted
						if (dealer.HasBusted()) {
							//Dealer has busted.
							handOver = true;
							dealersDecision = "Dealer Busted and lost";
						}
					}
					UpdateAllPlayers(false, true, new string[] { dealersDecision });

					//Delay for users to notice graphical update.
					Thread.Sleep(2000);
				}

				CalculateWinnings();
				DealHand(true);
				foreach (var playerCallBack in playerCallBacks)
					playerCallBack.Value.StartHand(GetAllPlayersAndDealer());

			} else if (playersatTable == 0)  {
				//No players in game check for players.
				CheckForPlayers();
			}
		}

		//Private Methods
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
		private async void CheckForPlayers() {
			bool continueLoop = true;

			while (continueLoop) {
				//Check every second if a player has joined the game.
				await Task.Delay(1000); 
										
				int playerCount = players.Count(p => p != null);
				if (playerCount > 0)
					continueLoop = false;
			}

			hasGameStarted = true;
			DealHand(false);
			foreach (var playerCallBack in playerCallBacks)
				playerCallBack.Value.StartHand(GetAllPlayersAndDealer());
		}
		private void UpdateAllPlayers(bool updatePlayers, bool updateDealer, string[] messages) {
			//Inform all clients of the changes.
			if (updatePlayers && updateDealer) {
				foreach (var playerCallBack in playerCallBacks)
					playerCallBack.Value.UpdateGUI(GetAllPlayersAndDealer(), messages);
			} else if (!updatePlayers && updateDealer) {
				foreach (var playerCallBack in playerCallBacks)
					playerCallBack.Value.UpdateDealerGUI(dealer, messages);
			}
		}
	}
}