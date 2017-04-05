using System.Linq;
using System.Runtime.Serialization;

namespace _21Library {
	public enum Suit { spades, hearts, clubs, diamonds }
	public enum Value { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, jack, queen, king, ace }
	[DataContract]
	public class Card {
		//Fields
		private const string CARDS_FOLDER = "images/cards/";
		private const string FILE_EXT = ".png";

		//Properties
		[DataMember]
		public Suit Suit { get; set; }
		[DataMember]
		public Value Value { get; set; }
		[DataMember]
		public string SourcePicture { get {
				//Value
				string value = Value.ToString();
				switch (Value) {
					case Value.Two:
						value = "2";
						break;
					case Value.Three:
						value = "3";
						break;
					case Value.Four:
						value = "4";
						break;
					case Value.Five:
						value = "5";
						break;
					case Value.Six:
						value = "6";
						break;
					case Value.Seven:
						value = "7";
						break;
					case Value.Eight:
						value = "8";
						break;
					case Value.Nine:
						value = "9";
						break;
					case Value.Ten:
						value = "10";
						break;
				}
				//Suit
				return CARDS_FOLDER + value + "_of_" + Suit.ToString() + FILE_EXT;
			}
			private set { }
		}
		[DataMember]
		public int NumberValue { get {
				int numberValue = 10;
				switch (Value) {
					case Value.Two:
						numberValue = 2;
						break;
					case Value.Three:
						numberValue = 3;
						break;
					case Value.Four:
						numberValue = 4;
						break;
					case Value.Five:
						numberValue = 5;
						break;
					case Value.Six:
						numberValue = 6;
						break;
					case Value.Seven:
						numberValue = 7;
						break;
					case Value.Eight:
						numberValue = 8;
						break;
					case Value.Nine:
						numberValue = 9;
						break;
					case Value.ace: //aces are handled as 1s when the total count is higher than 21.
						numberValue = 11;
						break;
				}
				return numberValue;
			}
			private set { }
		}
		
		//Constructor
		public Card(Suit suit, Value value) {
			Suit = suit;
			Value = value;
		}

		//Public Methods
		public override string ToString() {
			string value = Value.ToString();
			string suit = Suit.ToString();
			return	value.First().ToString().ToUpper() + value.Substring(1)
					+ " of " + 
					suit.First().ToString().ToUpper() + suit.Substring(1);
		}
	}
}
