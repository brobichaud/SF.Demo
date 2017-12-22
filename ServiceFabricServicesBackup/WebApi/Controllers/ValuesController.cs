using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using StatelessWorker;

namespace WebApi.Controllers
{
	public class ValuesController : ApiController
	{
		// GET api/values 
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/values/5 
		public string Get(int id)
		{
			IWorkerComm worker = ServiceProxy.Create<IWorkerComm>(new Uri("fabric:/SFDemo/StatelessWorker"));
			string env = worker.GetEnvironment().Result;
			bool ena = worker.GetEnableState().Result;
			DateTime start = worker.GetStartTime().Result;
			TimeSpan diff = DateTime.UtcNow - start;

			return string.Format("Enabled: {0}, Environment: {1}, Runtime: {2}", ena, env, diff);
		}

		// POST api/values 
		public void Post([FromBody]string value)
		{
		}

		// PUT api/values/5 
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/values/5 
		public void Delete(int id)
		{
		}
	}
}
