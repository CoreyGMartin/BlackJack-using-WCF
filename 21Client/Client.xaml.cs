using System;
using System.Linq;
using System.Windows;
using _21Library;
using System.ServiceModel;

namespace _21Client {
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
	public partial class Client : Window, IPlayerCallBack {
		//Fields
		public IBlackJackTable table;
		private Player[] players;
		private Dealer dealer;
		//private Dictionary<int, TurnIdentifier> turnIdentifierPositionColRow = new Dictionary<int, TurnIdentifier>();

		//Properties
		public new string Name { get; set; }
		public Client() {
			InitializeComponent();

			// Configure the ABCs of using the BlackJackTable service.
			DuplexChannelFactory<IBlackJackTable> channel = new DuplexChannelFactory<IBlackJackTable>(this, new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:12000/21Library/BlackJackTable"));

			// Activate a BlackJackTable instance.
			table = channel.CreateChannel();

			Join join = new Join(table, this);
			join.Show();
			Hide();

			player0Zone.Visibility = Visibility.Hidden;
			player1Zone.Visibility = Visibility.Hidden;
			player2Zone.Visibility = Visibility.Hidden;
			player3Zone.Visibility = Visibility.Hidden;
			player4Zone.Visibility = Visibility.Hidden;

			//turnIdentifierPositionColRow.Add(0, new TurnIdentifier(4, 1));
			//turnIdentifierPositionColRow.Add(1, new TurnIdentifier(3, 1));
			//turnIdentifierPositionColRow.Add(2, new TurnIdentifier(2, 2));
			//turnIdentifierPositionColRow.Add(3, new TurnIdentifier(1, 1));
			//turnIdentifierPositionColRow.Add(4, new TurnIdentifier(0, 1));
			//turnIdentifierPositionColRow.Add(5, new TurnIdentifier(1, 0)); //Dealer
		}

		//---------------------------------------------------Callback Implementation---------------------------------------------------
		private delegate void GuiUpdatePlayersAndDealer(Tuple<Player[], Dealer> playersAndDealer, string[] dealersDecision);
		private delegate void GuiUpdateDealer(Dealer dealer, string[] dealersDecision);
		private delegate void GuiUpdateMessages(string[] messages);
		private delegate void GuiUpdateMessage(string message);

		public void UpdatePlayerWithMessage(string message) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				if (message == "kicked") {
					Hide();
					statusTextBox.Text = "";
					Join join = new Join(table, this);
					join.Show();
					MessageBox.Show("Out of Money!", "No Money", MessageBoxButton.OK, MessageBoxImage.Information);
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

				AddTextToGameLog(messages);

				//Show player zones of seats taken
				if (players[0] != null) player0Zone.Visibility = Visibility.Visible; else player0Zone.Visibility = Visibility.Hidden;
				if (players[1] != null) player1Zone.Visibility = Visibility.Visible; else player1Zone.Visibility = Visibility.Hidden;
				if (players[2] != null) player2Zone.Visibility = Visibility.Visible; else player2Zone.Visibility = Visibility.Hidden;
				if (players[3] != null) player3Zone.Visibility = Visibility.Visible; else player3Zone.Visibility = Visibility.Hidden;
				if (players[4] != null) player4Zone.Visibility = Visibility.Visible; else player4Zone.Visibility = Visibility.Hidden;

				//Checks if player is waiting or currently playing.
				if (players.Any(p => p != null && p.Name == Name)) {
					Player player = players.First(p => p != null && p.Name == Name);
					//Check if it is my turn and if I have placed a bet
					if (player.IsItMyTurn && player.Bet == 0) {
						//My turn, haven't bet bet enable controls
						betBtn.IsEnabled = true;
						betSlider.IsEnabled = true;
						hitMeBtn.IsEnabled = false;
						stayBtn.IsEnabled = false;
					} else if (player.IsItMyTurn && !player.HasBusted()) {
						//Player has placed bet
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

			} else
				Dispatcher.BeginInvoke(new GuiUpdatePlayersAndDealer(UpdatePlayersAndDealer), new object[] { playersAndDealer, messages });
		}
		public void UpdateDealerGUI(Dealer dealer, string[] dealersDecision) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				//Update controls
				hitMeBtn.IsEnabled = false;
				stayBtn.IsEnabled = false;
				betBtn.IsEnabled = false;

				//Bind data from service to wpf
				DataContext = new { Players = players, Dealer = dealer };
				AddTextToGameLog(dealersDecision);
			} else
				Dispatcher.BeginInvoke(new GuiUpdateDealer(UpdateDealerGUI), new object[] { dealer, dealersDecision });
		}

		//---------------------------------------------------WPF Event Handlers---------------------------------------------------
		private void hitMeBtn_Click(object sender, RoutedEventArgs e) {
			table.DealCardToTurnPlayer();
		}
		private void betBtn_Click(object sender, RoutedEventArgs e) {
			table.SetPlayersTurnBet((int)betSlider.Value);
			//Minimum bet is 50
			betSlider.Value = 50; 
		}
		private void stayBtn_Click(object sender, RoutedEventArgs e) {
			table.NextTurn(false);
		}
		private void client_Closed(object sender, EventArgs e) {
			table.RemovePlayer(Name);
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
