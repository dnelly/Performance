// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benchmarks.Utility.Helpers;
using Benchmarks.Utility.Logging;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Stress.Framework
{
    public class StressTestServer : IDisposable
    {
        private readonly string _testName;
        private readonly int _port;
        private readonly IStressMetricCollector _metricCollector;
        private ILogger _logger;
        private readonly string _testMethodName;
        private readonly ServerType _serverType;

        private IApplicationDeployer _applicationDeployer;

        public StressTestServer(
            ServerType serverType,
            string testName,
            string testMethodName,
            int port,
            IStressMetricCollector metricCollector)
        {
            _serverType = serverType;
            _testName = testName;
            _testMethodName = testMethodName;
            _port = port;
            _metricCollector = metricCollector;
        }

        public async Task<StressTestServerStartResult> StartAsync()
        {
            var framework = PlatformServices.Default.Runtime.RuntimeType;
            var fullTestName = $"{_testMethodName}.{_testName}.{framework}";
            fullTestName = fullTestName.Replace('_', '.');

            _logger = LogUtility.LoggerFactory.CreateLogger(fullTestName);

            var baseAddress = $"http://localhost:{_port}/";

            var p = new DeploymentParameters(PathHelper.GetTestAppFolder(_testName), _serverType, RuntimeFlavor.Clr, RuntimeArchitecture.x86)
            {
                SiteName = _testName,
                ApplicationBaseUriHint = baseAddress,
                PublishTargetFramework = "net451"
            };

            ILogger deployerLogger;
            if (StressConfig.Instance.DeployerLogging)
            {
                deployerLogger = _logger;
            }
            else
            {
                deployerLogger = new NullLogger();
            }

            _applicationDeployer = ApplicationDeployerFactory.Create(p, deployerLogger);
            var deploymentResult = _applicationDeployer.Deploy();
            baseAddress = deploymentResult.ApplicationBaseUri;

            _logger.LogInformation($"Test project is set up at {deploymentResult.WebRootLocation}");

            var result = new StressTestServerStartResult
            {
                ServerHandle = this
            };
            var serverVerificationClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };

            HttpResponseMessage response = null;
            for (int i = 0; i < 20; ++i)
            {
                try
                {
                    _logger.LogInformation($"Pinging {serverVerificationClient.BaseAddress} to ensure server booted properly");
                    response = await serverVerificationClient.GetAsync(serverVerificationClient.BaseAddress);
                    break;
                }
                catch (TimeoutException)
                {
                    _logger.LogError("Http client timeout.");
                    break;
                }
                catch (Exception)
                {
                    _logger.LogInformation("Failed to ping server. Retrying...");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }
            }

            result.SuccessfullyStarted = false;
            if (response != null)
            {
                _logger.LogInformation($"Response {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Server started successfully");
                    result.SuccessfullyStarted = true;
                    ClientFactory = () => new RequestTrackingHttpClient(baseAddress, _metricCollector);
                }
            }

            return result;
        }

        public void Dispose()
        {
            ClientFactory = null;
            _applicationDeployer?.Dispose();
        }

        public Func<HttpClient> ClientFactory { get; private set; }

        private class RequestTrackingHttpClient : HttpClient
        {
            public RequestTrackingHttpClient(string baseAddress, IStressMetricCollector metricCollector)
                : base(new RequestTrackingHandler(metricCollector))
            {
                BaseAddress = new Uri(baseAddress);
            }

            private class RequestTrackingHandler : HttpClientHandler
            {
                private readonly IStressMetricCollector _metricCollector;

                public RequestTrackingHandler(IStressMetricCollector metricCollector)
                {
                    _metricCollector = metricCollector;
                }

                protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    _metricCollector.NewRequest();
                    return base.SendAsync(request, cancellationToken);
                }
            }
        }

        private class NullLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }
    }
}
