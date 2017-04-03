using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace _21Library {
	public interface IPlayerCallBack {
		[OperationContract(IsOneWay = true)]
		void StartHand(Tuple<Player[], Dealer> playersAndDealer);
		[OperationContract(IsOneWay = true)]
		void UpdateGUI(Tuple<Player[], Dealer> playersAndDealer, string[] messages);
		[OperationContract(IsOneWay = true)]
		void UpdateDealerGUI(Dealer dealer, string[] dealersDecision);
	}
}
