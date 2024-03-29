﻿using System.ServiceModel;

namespace _21Library {
	[ServiceContract(CallbackContract = typeof(IPlayerCallBack))]
	public interface IBlackJackTable : I21Service {
		//Methods
		[OperationContract(IsOneWay = true)]
		void StartGame();
		[OperationContract]
		string AddPlayer(string player);
		[OperationContract(IsOneWay = true)]
		void RemovePlayer(string name);
		[OperationContract(IsOneWay = true)]
		void SetPlayersTurnBet(int bet);
		[OperationContract(IsOneWay = true)]
		void DealCardToTurnPlayer();
		[OperationContract(IsOneWay = true)]
		void NextTurn(bool removeCurrentTurnPlayer);
	}
}
