using System;
using System.Linq;
using System.Windows;
using _21Library;
using System.ServiceModel;

namespace _21Client {
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
	public partial class Client : Window, IPlayerCallBack {
		//Fields
		private IBlackJackTable blackJackTable;
		private IUsersTable usersTable;
		private Player[] players;
		private Dealer dealer;
		private string name;
		private string endpoint;

		//Properties
		public static readonly DependencyProperty TurnIndicatorColProperty = DependencyProperty.Register("TurnIndicatorCol", typeof(int), typeof(Client));
		public static readonly DependencyProperty TurnIndicatorRowProperty = DependencyProperty.Register("TurnIndicatorRow", typeof(int), typeof(Client));
		public int TurnIndicatorCol { get { return (int)GetValue(TurnIndicatorColProperty); } set { SetValue(TurnIndicatorColProperty, value); } }
		public int TurnIndicatorRow { get { return (int)GetValue(TurnIndicatorRowProperty); } set { SetValue(TurnIndicatorRowProperty, value); } }

		//Constructor
		public Client(IUsersTable usersTable, string name, string endpoint) {
			InitializeComponent();

			this.usersTable = usersTable;
			this.name = name;
			this.endpoint = endpoint;
			Title = "BlackJack - " + name;

			// Configure the ABCs of using the BlackJackTable service.
			string endpointAddress = "net.tcp://" + endpoint + ":12000/21Library/BlackJackTable";
			NetTcpBinding tcpBinding = new NetTcpBinding();
			tcpBinding.Security.Mode = SecurityMode.None;
			DuplexChannelFactory<IBlackJackTable> channel = new DuplexChannelFactory<IBlackJackTable>(this, tcpBinding, new EndpointAddress(endpointAddress));

			// Activate a BlackJackTable instance.
			blackJackTable = channel.CreateChannel();

			//Add player to table
			blackJackTable.AddPlayer(name);
		}

		//---------------------------------------------------Callback Implementation----------------------------------------------
		private delegate void GuiUpdatePlayersAndDealer(Tuple<Player[], Dealer> playersAndDealer, string[] dealersDecision);
		private delegate void GuiUpdateDealer(Dealer dealer, string[] dealersDecision);
		private delegate void GuiUpdateMessages(string[] messages);
		private delegate void GuiUpdateMessage(string message);
		public void UpdatePlayerWithMessage(string message) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				if (message == "kicked") {
					MessageBox.Show("Out of Money!", "No Money", MessageBoxButton.OK, MessageBoxImage.Information);
					Join join = new Join(usersTable, name, endpoint);
					join.Show();
					Close();
				}
			} else
				Dispatcher.BeginInvoke(new GuiUpdateMessage(UpdatePlayerWithMessage), new object[] { message });
		}
		public void UpdatePlayersWithMessage(string[] messages) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				AddTextToGameLog(messages);
			} else
				Dispatcher.BeginInvoke(new GuiUpdateMessages(UpdatePlayersWithMessage), new object[] { messages });
		}
		public void UpdatePlayersAndDealer(Tuple<Player[], Dealer> playersAndDealer, string[] messages) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				//Bind data from service to wpf
				players = playersAndDealer.Item1;
				dealer = playersAndDealer.Item2;
				DataContext = new { Players = players, Dealer = dealer };

				//Add messages
				AddTextToGameLog(messages);

				//Show player zones of seats taken and set turn indicator to hidden
				turnIndicatorEllipse.Visibility = Visibility.Hidden;
				if (players[0] != null) player0Zone.Visibility = Visibility.Visible; else player0Zone.Visibility = Visibility.Hidden;
				if (players[1] != null) player1Zone.Visibility = Visibility.Visible; else player1Zone.Visibility = Visibility.Hidden;
				if (players[2] != null) player2Zone.Visibility = Visibility.Visible; else player2Zone.Visibility = Visibility.Hidden;
				if (players[3] != null) player3Zone.Visibility = Visibility.Visible; else player3Zone.Visibility = Visibility.Hidden;
				if (players[4] != null) player4Zone.Visibility = Visibility.Visible; else player4Zone.Visibility = Visibility.Hidden;

				if (dealer.HasGameStarted) {
					//Disable startgame button since game has already started
					startGameBtn.IsEnabled = false;

					//Find out where the yellow circle turn indicator chip should be.
					for (int i = 0; i < players.Length; ++i) {
						if (players[i] != null && players[i].IsItMyTurn) {
							if (i == 0) {
								TurnIndicatorCol = 4;
								TurnIndicatorRow = 1;
							} else if (i == 1) {
								TurnIndicatorCol = 3;
								TurnIndicatorRow = 1;
							} else if (i == 2) {
								TurnIndicatorCol = 2;
								TurnIndicatorRow = 2;
							} else if (i == 3) {
								TurnIndicatorCol = 1;
								TurnIndicatorRow = 1;
							} else if (i == 4) {
								TurnIndicatorCol = 0;
								TurnIndicatorRow = 1;
							}
							turnIndicatorEllipse.Visibility = Visibility.Visible;
							break;
						}	
					}

					//Checks if player is waiting or currently playing.
					if (players.Any(p => p != null && p.Name == name)) {
						Player player = players.First(p => p != null && p.Name == name);
						//Check if it is my turn and if I have placed a bet
						if (player.IsItMyTurn && player.Bet == 0) {
							//My turn, haven't bet yet, enable controls
							betBtn.IsEnabled = true;
							betSlider.IsEnabled = true;
							hitMeBtn.IsEnabled = false;
							stayBtn.IsEnabled = false;
						} else if (player.IsItMyTurn && !player.HasBusted()) {
							//My turn, Player has placed bet, time to stay or hit
							betBtn.IsEnabled = false;
							betSlider.IsEnabled = false;
							hitMeBtn.IsEnabled = true;
							stayBtn.IsEnabled = true;
						} else {
							//Not my turn, disable controls
							hitMeBtn.IsEnabled = false;
							stayBtn.IsEnabled = false;
							betBtn.IsEnabled = false;
							betSlider.IsEnabled = false;
						}
						betSlider.Maximum = player.Money;
						availableMoneyToBetTextBlock.Text = player.Money.ToString();
					}
				} else {
					Player player = players.First(p => p != null && p.CanStartGame == true);
					if (player.Name == name) {
						startGameBtn.IsEnabled = true;
					} else {
						startGameBtn.IsEnabled = false;
					}
				}

			} else
				Dispatcher.BeginInvoke(new GuiUpdatePlayersAndDealer(UpdatePlayersAndDealer), new object[] { playersAndDealer, messages });
		}
		public void UpdateDealer(Dealer dealer, string[] dealersDecision) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				//Dealers turn disable controls.
				hitMeBtn.IsEnabled = false;
				stayBtn.IsEnabled = false;
				betBtn.IsEnabled = false;
				turnIndicatorEllipse.Visibility = Visibility.Hidden;

				//Bind data from service to wpf
				DataContext = new { Players = players, Dealer = dealer };
				AddTextToGameLog(dealersDecision);
			} else
				Dispatcher.BeginInvoke(new GuiUpdateDealer(UpdateDealer), new object[] { dealer, dealersDecision });
		}

		//---------------------------------------------------WPF Event Handlers---------------------------------------------------
		private void hitMeBtn_Click(object sender, RoutedEventArgs e) {
			try {
				blackJackTable.DealCardToTurnPlayer();
			} catch {
				MessageBox.Show("Problem connecting to Server. Please close the client and try again.", "Connection Problem", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void betBtn_Click(object sender, RoutedEventArgs e) {
			try {
				blackJackTable.SetPlayersTurnBet((int)betSlider.Value);
				//Minimum bet is 50
				betSlider.Value = 50;
			} catch {
				MessageBox.Show("Problem connecting to Server. Please close the client and try again.", "Connection Problem", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void stayBtn_Click(object sender, RoutedEventArgs e) {
			try {
				blackJackTable.NextTurn(false);
			} catch {
				MessageBox.Show("Problem connecting to Server. Please close the client and try again.", "Connection Problem", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void client_Closed(object sender, EventArgs e) {
			try {
				blackJackTable.RemovePlayer(name);
			} catch {
				//No need to inform client, They were exiting the application anyways.
			}
		}
		private void startGameBtn_Click(object sender, RoutedEventArgs e) {
			try {
				blackJackTable.StartGame();
			} catch {
				MessageBox.Show("Problem connecting to Server. Please close the client and try again.", "Connection Problem", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		//---------------------------------------------------Helper Methods---------------------------------------------------
		public void AddTextToGameLog(string[] messages) {
			if (messages != null) {
				foreach (string message in messages)
					if (message != null)
						statusTextBox.AppendText(message + "\n");

				//Auto scroll vertical scroll bar.
				statusTextBox.Focus();
				statusTextBox.CaretIndex = statusTextBox.Text.Length;
				statusTextBox.ScrollToEnd();
			}
		}
		public void AddTextToGameLogNoSapces(string message) {
			if (message != null) {
				statusTextBox.AppendText(message);

				//Auto scroll vertical scroll bar.
				statusTextBox.Focus();
				statusTextBox.CaretIndex = statusTextBox.Text.Length;
				statusTextBox.ScrollToEnd();
			}
		}
	}
}
