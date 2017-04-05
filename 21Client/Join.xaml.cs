using _21Library;
using System;
using System.ServiceModel;
using System.Windows;

namespace _21Client {
	public partial class Join : Window {
		//fields
		IUsersTable usersTable;

		//Constructors
		public Join() {
			InitializeComponent();
			usersTable = null;
		}
		public Join(IUsersTable usersTable, string name, string endpoint) {
			InitializeComponent();
			this.usersTable = usersTable;
			nameTextBox.Text = name;
			endpointTextBox.Text = endpoint;
		}

		//------WPF Event Handlers-------
		private void joinBtn_Click(object sender, RoutedEventArgs e) {
			string name = nameTextBox.Text;
			string endpoint = endpointTextBox.Text;

			//Check for valid iput
			if (name != "" && endpoint != "") {
				try {
					//Establish a connection if one isn't already established.
					if (usersTable == null) {
						// Configure the ABCs of using the UsersTable service.
						string endpointAddress = "net.tcp://" + endpoint + ":12000/21Library/UsersTable";
						NetTcpBinding tcpBinding = new NetTcpBinding();
						tcpBinding.Security.Mode = SecurityMode.None;
						ChannelFactory<IUsersTable> channel = new ChannelFactory<IUsersTable>(tcpBinding, new EndpointAddress(endpointAddress));

						// Activate a UsersTable instance.
						usersTable = channel.CreateChannel();
					}

					//Attempt to check name in UsersTable service
					Status status = usersTable.JoinTable(name);
					if (status == Status.Success) {
						Client client = new Client(usersTable, name, endpoint);
						client.Show();
						Close();
					} else if (status == Status.NameTaken) {
						//Failed to join game
						MessageBox.Show("Name has been taken, please choose a different name", "Name Already In Use", MessageBoxButton.OK, MessageBoxImage.Error);
					} else if (status == Status.GameFull) {
						//Failed to join game
						MessageBox.Show("The table is currently full please try again later.", "Game Full", MessageBoxButton.OK, MessageBoxImage.Error);
					} else {
						//Failed to join game
						MessageBox.Show("Error", "Unknown Error occurred", MessageBoxButton.OK, MessageBoxImage.Error);
					}

				} catch (Exception ex) {
					//server address is incorrect
					MessageBox.Show("Please enter a valid server.", "Invalid Server", MessageBoxButton.OK, MessageBoxImage.Error);
					usersTable = null;
					//MessageBox.Show(ex.Message);
				}
			} else {
				MessageBox.Show("Please completely fill in the required inputs.", "Informaton Needed", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
	}
}
