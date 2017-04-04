using System.Windows;
using System.Runtime.Serialization;

namespace _21Library {
	[DataContract]
	public class Dealer {
		//Properties
		[DataMember]
		public Card[] Cards { get; set; }
		[DataMember]
		public Visibility FirstCard { get; set; }
		[DataMember]
		public int CardTotal { get {
				int total = 0;
				int numberOfAces = 0;
				int i = 1;
				if (FirstCard == Visibility.Visible)
					i = 0;
				for (; i < Cards.Length; ++i)
					if (Cards[i] != null) {
						int value = Cards[i].NumberValue;
						if (value == 11) //ace
							++numberOfAces;
						total += value;
					}

				//accounts for aces
				for (int ace = 0; ace < numberOfAces; ++ace)
					if (total > 21)
						total -= 10;

				return total;
			}
			private set { }
		}
		[DataMember]
		public string CardTotalLabel { get { if (CardTotal > 21) return "Busted"; return CardTotal.ToString(); } private set { } }

		//Constructor
		public Dealer() {
			FirstCard = Visibility.Hidden;
			Cards = new Card[8];
		}

		//Public Methods
		public bool HasBusted() {
			if (CardTotal > 21)
				return true;
			else
				return false;
		}
		public void ResetHand() {
			FirstCard = Visibility.Hidden;
			Cards = new Card[8];
		}
		public string GetCardNameByIndex(int index) {
			return Cards[index].ToString();
		}
	}
}
