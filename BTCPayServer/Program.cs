﻿using BTCPayServer.Configuration;
using BTCPayServer.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using BTCPayServer.Hosting;
using NBitcoin;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Threading;

namespace BTCPayServer
{
	class Program
	{
		static void Main(string[] args)
		{
			ServicePointManager.DefaultConnectionLimit = 100;
			IWebHost host = null;
			CustomConsoleLogProvider loggerProvider = new CustomConsoleLogProvider();

			var loggerFactory = new LoggerFactory();
			loggerFactory.AddProvider(loggerProvider);
			var logger = loggerFactory.CreateLogger("Configuration");
			try
			{
				var conf = new DefaultConfiguration() { Logger = logger }.CreateConfiguration(args);
				if(conf == null)
					return;

				host = new WebHostBuilder()
					.UseKestrel()
					.UseIISIntegration()
					.UseContentRoot(Directory.GetCurrentDirectory())
					.UseConfiguration(conf)
					.UseApplicationInsights()
					.ConfigureLogging(l =>
					{
						l.AddFilter("Microsoft", LogLevel.Error);
						l.AddProvider(new CustomConsoleLogProvider());
					})
					.UseStartup<Startup>()
					.Build();
				host.StartAsync().GetAwaiter().GetResult();
				var urls = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
				foreach(var url in urls)
				{
					logger.LogInformation("Listening on " + url);
				}
				host.WaitForShutdown();
			}
			catch(ConfigException ex)
			{
				if(!string.IsNullOrEmpty(ex.Message))
					Logs.Configuration.LogError(ex.Message);
			}
			catch(Exception exception)
			{
				logger.LogError("Exception thrown while running the server");
				logger.LogError(exception.ToString());
			}
			finally
			{
				if(host != null)
					host.Dispose();
				loggerProvider.Dispose();
			}
		}
	}
}
