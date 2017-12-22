using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Health;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace StatelessWorker
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class StatelessWorker : StatelessService, IWorkerComm
	{
		private DateTime _startTime = DateTime.UtcNow;

		public StatelessWorker(StatelessServiceContext context)
			 : base(context)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new[] { new ServiceInstanceListener(context => this.CreateServiceRemotingListener(context)) };
			//return new ServiceInstanceListener[0];
		}

		/// <summary>
		/// This is the main entry point for your service instance.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			// TODO: Replace the following sample code with your own logic 
			//       or remove this RunAsync override if it's not needed in your service.
			long iteration = 0;
			this.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

			while (true)
			{
				iteration++;
				cancellationToken.ThrowIfCancellationRequested();
				bool enabled = GetEnableState();
				string env = GetEnvironment();
				TimeSpan diff = DateTime.UtcNow - _startTime;

				// report warning on odd iterations
				var state = ReportHealth(iteration);
				ServiceEventSource.Current.ServiceMessage(this, "StatelessWorker loop: {0}, enabled: {1}, env: {2}, runtime: {3}, health: {4}", iteration, enabled, env, diff, state);

				await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
			}
		}

		private HealthState ReportHealth(long iteration)
		{
			HealthState state = ((iteration % 2) != 0) ? HealthState.Warning : HealthState.Ok;
			HealthInformation health = new HealthInformation("StatelessWorker", "Iteration", state);
			if (state != HealthState.Ok)
				health.Description = "Odd iteration, unhealthy state";
			else
				health.Description = "Even iteration, healthy state";
			health.SequenceNumber = iteration;
			health.TimeToLive = TimeSpan.FromSeconds(60);
			health.RemoveWhenExpired = true;
			Partition.ReportInstanceHealth(health);

			return state;
		}

		/// <summary>
		/// Handle notifications of setting changes
		/// </summary>
		private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
		{
			var env = e.NewPackage.Settings.Sections["WorkerConfig"].Parameters["Environment"];
			ServiceEventSource.Current.ServiceMessage(this, "New Config, environment: {0}, path: {1}", env.Value, e.NewPackage.Path);
		}

		private bool GetEnableState()
		{
			var cfgPkg = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
			var enabled = cfgPkg.Settings.Sections["WorkerConfig"].Parameters["Enabled"];
			return Convert.ToBoolean(enabled.Value ?? "false");

			// Access custom configuration file:
			//using (StreamReader reader = new StreamReader(Path.Combine(cfgPkg.Path, "CustomConfig.json")))
			//{
			//	MySettings settings = JsonConvert.DeserializeObject<MySettings>(reader.ReadToEnd());
			//}
		}

		private string GetEnvironment()
		{
			var cfgPkg = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
			var env = cfgPkg.Settings.Sections["WorkerConfig"].Parameters["Environment"];
			return env.Value;
		}

		Task<string> IWorkerComm.GetEnvironment()
		{
			string env = GetEnvironment();
			return Task.FromResult(env);
		}

		Task<bool> IWorkerComm.GetEnableState()
		{
			bool ena = GetEnableState();
			return Task.FromResult(ena);
		}

		Task<DateTime> IWorkerComm.GetStartTime()
		{
			return Task.FromResult(_startTime);
		}
	}
}
