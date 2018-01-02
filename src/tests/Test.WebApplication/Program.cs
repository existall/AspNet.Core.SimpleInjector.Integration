using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExistsForAll.SimpleInjector.AspNetCore.Integration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleInjector.Lifestyles;

namespace Test.WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
			var host = new WebHostBuilder()
		        .UseKestrel()
		        .UseContentRoot(Directory.GetCurrentDirectory())
		        .UseIISIntegration()
		        .UseSimpleInjector(o => o.DefaultScopedLifestyle = new AsyncScopedLifestyle())
		        .UseStartup<Startup>()
		        .UseApplicationInsights()
		        .Build();

	        host.Run();
		}
    }
}
