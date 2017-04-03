using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
			DuplexChannelFactory<IBlackJackTable> channel = new DuplexChannelFactory<IBlackJackTable>(
																this,
																new NetTcpBinding(),
																new EndpointAddress("net.tcp://localhost:12000/21Library/BlackJackTable")
															);

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
		private delegate void GuiUpdate(Tuple<Player[], Dealer> playersAndDealer);
		private delegate void GuiUpdatePlayersAndDealer(Tuple<Player[], Dealer> playersAndDealer, string[] dealersDecision);
		private delegate void GuiUpdateDealer(Dealer dealer, string[] dealersDecision);
		public async void StartHand(Tuple<Player[], Dealer> playersAndDealer) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				//Inform player the hand is about to start.
				AddTextToGameLogNoSapces("Hand Starting in");
				for (int second = 3; second > 0; --second) {
					AddTextToGameLogNoSapces(" " + second);
					await Task.Delay(1000);
				}
				AddTextToGameLog(new string[] { "" });

				//Bind data from service to wpf
				players = playersAndDealer.Item1;
				dealer = playersAndDealer.Item2;
				DataContext = new { Players = players, Dealer = dealer };

				//Show player zones of seats taken
				if (players[0] != null)
					player0Zone.Visibility = Visibility.Visible;
				if (players[1] != null)
					player1Zone.Visibility = Visibility.Visible;
				if (players[2] != null)
					player2Zone.Visibility = Visibility.Visible;
				if (players[3] != null)
					player3Zone.Visibility = Visibility.Visible;
				if (players[4] != null)
					player4Zone.Visibility = Visibility.Visible;

				//Check if it is my turn
				Player player = players.First(p => p != null && p.Name == Name);
				if (player.IsItMyTurn) {
					//My turn, enable controls
					betBtn.IsEnabled = true;
					betSlider.IsEnabled = true;
				} else {
					//Not my turn, disable controls
					hitMeBtn.IsEnabled = false;
					stayBtn.IsEnabled = false;
					betBtn.IsEnabled = false;
					betSlider.IsEnabled = false;
				}
				betSlider.Maximum = player.Money;
				availableMoneyToBetTextBlock.Text = player.Money.ToString();

			} else
				await Dispatcher.BeginInvoke(new GuiUpdate(StartHand), new object[] { playersAndDealer });
		}
		public void UpdateGUI(Tuple<Player[], Dealer> playersAndDealer, string[] messages) {
			if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
				//Bind data from service to wpf
				players = playersAndDealer.Item1;
				dealer = playersAndDealer.Item2;
				DataContext = new { Players = players, Dealer = dealer };

				AddTextToGameLog(messages);

				//Show player zones of seats taken
				if (players[0] != null)
					player0Zone.Visibility = Visibility.Visible;
				if (players[1] != null)
					player1Zone.Visibility = Visibility.Visible;
				if (players[2] != null)
					player2Zone.Visibility = Visibility.Visible;
				if (players[3] != null)
					player3Zone.Visibility = Visibility.Visible;
				if (players[4] != null)
					player4Zone.Visibility = Visibility.Visible;

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
					} else if (player.IsItMyTurn) {
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
					//Play is not waiting.
					//Player player = players.First(p => p != null && p.Name == Name);
					//if (player.HasBusted()) {
					//	//Player busted.
					//	hitMeBtn.IsEnabled = false;
					//	stayBtn.IsEnabled = false;
					//}
				}

			} else
				Dispatcher.BeginInvoke(new GuiUpdatePlayersAndDealer(UpdateGUI), new object[] { playersAndDealer, messages });
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

			betSlider.Value = 50; //Minimum bet is 50
			//betSlider.IsEnabled = false;
			//betBtn.IsEnabled = false;

			//hitMeBtn.IsEnabled = true;
			//stayBtn.IsEnabled = true;
		}

		private void stayBtn_Click(object sender, RoutedEventArgs e) {
			table.NextTurn();
			betSlider.IsEnabled = true;
			betBtn.IsEnabled = true;
			hitMeBtn.IsEnabled = false;
			stayBtn.IsEnabled = false;
		}

		private void client_Closed(object sender, EventArgs e) {
			table.RemovePlayer(Name);
		}

		//---------------------------------------------------Helper Methods---------------------------------------------------
		public void AddTextToGameLog(string[] messages) {
			if (messages != null) {
				foreach (string message in messages)
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
