using System.Windows;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace _21Library {
	[DataContract]
	public class Player : INotifyPropertyChanged {
		//Fields
		public event PropertyChangedEventHandler PropertyChanged;
		private int amountWonLost;
		private int bet;
		private int money;

		//Properties
		[DataMember]
		public int AmountWonLost {
			get { return amountWonLost; }
			set {
				amountWonLost = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AmountWonLost"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AmountWonLostLabel"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AmountWonLostTextColor"));
			}
		}
		[DataMember]
		public string AmountWonLostLabel { get { return AmountWonLost.ToString("C"); } private set { } }
		[DataMember]
		public string AmountWonLostTextColor { get { if (AmountWonLost > 0) return "Green"; return "Red"; } private set { } }
		[DataMember]
		public Visibility AmountWonLostLabelVisibility { get; set; }
		[DataMember]
		public int Bet {
			get { return bet; }
			set { bet = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bet"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BetLabel"));
			}
		}
		[DataMember]
		public string BetLabel { get { return Bet.ToString("C"); } private set { } }
		[DataMember]
		public bool CanStartGame { get; set; }
		[DataMember]
		public Card[] Cards { get; set; }
		[DataMember]
		public int CardTotal {
			get {
				int total = 0;
				int numberOfAces = 0;
				for (int i = 0; i < Cards.Length; ++i)
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
		[DataMember]
		public bool IsItMyTurn { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Money {
			get { return money; }
			set {
				money = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Money"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MoneyLabel"));
			}
		}
		[DataMember]
		public string MoneyLabel { get { return Money.ToString("C"); } private set { } }

		//Constructor
		public Player(string name) {
			Cards = new Card[8];
			IsItMyTurn = false;
			CanStartGame = false;
			AmountWonLostLabelVisibility = Visibility.Hidden;
			Name = name;
			money = 20000; //$20,000
		}

		//Public Methods
		public bool HasBusted() {
			if (CardTotal > 21)
				return true;
			else
				return false;
		}
		public void ResetHand() {
			Cards = new Card[8];
			IsItMyTurn = false;
		}
		public void LostBet() {
			AmountWonLost = Bet * -1;
			AmountWonLostLabelVisibility = Visibility.Visible;
		}
		public void WonBet(double rate) {
			AmountWonLost = (int)(Bet * rate);
			Money += AmountWonLost;
			AmountWonLostLabelVisibility = Visibility.Visible;
		}
		public void ClearBet() {
			AmountWonLost = 0;
			Bet = 0;
			AmountWonLostLabelVisibility = Visibility.Hidden;
		}
    }
}
