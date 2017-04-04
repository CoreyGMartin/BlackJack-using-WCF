using _21Library;
using System.Windows;

namespace _21Client {
	/// <summary>
	/// Interaction logic for Join.xaml
	/// </summary>
	public partial class Join : Window {
		IBlackJackTable table;
		Client client;
		public Join(IBlackJackTable table, Client client) {
			InitializeComponent();
			this.client = client;
			this.table = table;
		}

		private void joinBtn_Click(object sender, RoutedEventArgs e) {
			string name = nameTextBox.Text;
			if (name != "") {
				//Attempt to join game.
				string response = table.AddPlayer(name);
				if (response == "success") {
					client.Name = name;
					client.Title = "BlackJack - " + name;
					client.Show();
					Close();
				} else if (response == "nameTaken") {
					//Failed to join game
					MessageBox.Show("Name has been taken, please choose a different name", "Name Already In Use", MessageBoxButton.OK, MessageBoxImage.Error);
				} else if (response == "gameFull") {
					//Failed to join game
					MessageBox.Show("The table is currently full please try again later.", "Game Full", MessageBoxButton.OK, MessageBoxImage.Error);
				} else {
					//Failed to join game
					MessageBox.Show("Error", "Unknown Error occurred", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
}
