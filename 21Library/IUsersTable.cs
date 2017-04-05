using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace _21Library {
	public enum Status { Success, GameFull, NameTaken}
	[ServiceContract]
	public interface IUsersTable : I21Service {
		[OperationContract]
		Status JoinTable(string name);
	}
}
