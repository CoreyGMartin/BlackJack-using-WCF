using _21Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
				if (table.AddPlayer(name)) {
					client.Name = name;
					client.Title = "BlackJack - " + name;
					client.Show();
					Close();
				} else {
					//Failed to join game
					MessageBox.Show("Name has been taken, please choose a different name", "Name Already In Use", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
}
