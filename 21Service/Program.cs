using System;
using System.ServiceModel;
using _21Library;

namespace _21Service {
	class Program {
		static void Main(string[] args) {
			ServiceHost _21Service = null;	//port = 12000
			try {
				// Address
				_21Service = new ServiceHost(typeof(BlackJackTable), new Uri("net.tcp://localhost:12000/21Library/"));

				// Service contracts and binding
				NetTcpBinding tcpBinding = new NetTcpBinding();
				tcpBinding.Security.Mode = SecurityMode.None;
				_21Service.AddServiceEndpoint(typeof(IBlackJackTable), tcpBinding, "BlackJackTable");
				_21Service.AddServiceEndpoint(typeof(IUsersTable), tcpBinding, "UsersTable");

				// Manage the service’s life cycle
				_21Service.Open();
				Console.WriteLine("21 service has started. Press a key to quit.");
				Console.ReadKey();

			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
			} finally {
				if (_21Service != null)
					_21Service.Close();
			}
		}
	}
}
