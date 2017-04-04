using System;
using System.ServiceModel;
using _21Library;

namespace _21Service {
	class Program {
		static void Main(string[] args) {
			ServiceHost servHost = null;
			try {
				// Address
				servHost = new ServiceHost(typeof(BlackJackTable), new Uri("net.tcp://localhost:12000/21Library/"));

				// Service contract and binding
				servHost.AddServiceEndpoint(typeof(IBlackJackTable), new NetTcpBinding(), "BlackJackTable");

				// Manage the service’s life cycle
				servHost.Open();
				Console.WriteLine("Service started. Press a key to quit.");

				Console.ReadKey();

			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
			} finally {
				
				if (servHost != null)
					servHost.Close();
			}

		}
	}
}
