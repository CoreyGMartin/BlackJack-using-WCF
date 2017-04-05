using System;
using System.ServiceModel;

namespace _21Library {
	public interface IPlayerCallBack {
		[OperationContract(IsOneWay = true)]
		void UpdatePlayersAndDealer(Tuple<Player[], Dealer> playersAndDealer, string[] messages);
		[OperationContract(IsOneWay = true)]
		void UpdateDealer(Dealer dealer, string[] dealersDecision);
		[OperationContract(IsOneWay = true)]
		void UpdatePlayersWithMessage(string[] messages);
		[OperationContract(IsOneWay = true)]
		void UpdatePlayerWithMessage(string message);
	}
}
