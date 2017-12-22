using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace StatelessWorker
{
	public interface IWorkerComm : IService
	{
		Task<string> GetEnvironment();
		Task<bool> GetEnableState();
		Task<DateTime> GetStartTime();
	}
}
