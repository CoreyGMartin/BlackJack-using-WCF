using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _21Library {
	[ServiceContract(CallbackContract = typeof(IPlayerCallBack))]
	public interface IBlackJackTable {
		//Methods
		[OperationContract]
		bool AddPlayer(string player);
		[OperationContract(IsOneWay = true)]
		void RemovePlayer(string name);
		[OperationContract(IsOneWay = true)]
		void SetPlayersTurnBet(int bet);
		[OperationContract(IsOneWay = true)]
		void DealCardToTurnPlayer();
		[OperationContract(IsOneWay = true)]
		void NextTurn();
	}
}
