// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime;
using Microsoft.Extensions.Logging;

namespace HelloWorldMvc
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseDefaultHostingConfiguration(args)
                .UseIIS()
                .UseStartup<Startup>()
                .Build();

            if(GCSettings.IsServerGC)
            {
               Console.WriteLine("Server GC");
            }
            else
            {
                Console.WriteLine("Workstation GC");
            }

            host.Run();
        }
    }
}

