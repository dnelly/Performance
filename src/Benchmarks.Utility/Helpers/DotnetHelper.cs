// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace Benchmarks.Utility.Helpers
{
    public class DotnetHelper
    {
        private static readonly DotnetHelper _default = new DotnetHelper();
        private readonly string _executablePath = Path.Combine("cli", "bin", "dotnet.exe");
        private readonly string _defaultDotnetHome = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "Microsoft", "dotnet");

        public static DotnetHelper GetDefaultInstance() => _default;

        private DotnetHelper() { }

        public ProcessStartInfo BuildStartInfo(
            string appbasePath,
            string argument)
        {
            var dotnetPath = GetDotnetExecutable();
            var psi = new ProcessStartInfo(dotnetPath, argument)
            {
                WorkingDirectory = appbasePath,
                UseShellExecute = false
            };

            return psi;
        }

        public bool Restore(string workingDir, bool quiet = false)
        {
            var psi = new ProcessStartInfo(GetDotnetExecutable())
            {
                Arguments = "restore" + (quiet ? " --quiet" : ""),
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            var proc = Process.Start(psi);

            var exited = proc.WaitForExit(300 * 1000);

            return exited && proc.ExitCode == 0;
        }

        public bool Publish(string workingDir, string outputDir, string framework)
        {
            var psi = new ProcessStartInfo(GetDotnetExecutable())
            {
                Arguments = $"publish --output {outputDir}",
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            if (!string.IsNullOrEmpty(framework))
            {
                psi.Arguments = $"{psi.Arguments} --framework {framework}";
            }

            var proc = Process.Start(psi);
            var exited = proc.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);

            return exited && proc.ExitCode == 0;
        }

        public bool Publish(string workingDir, string outputDir)
        {
            return Publish(workingDir, outputDir, framework: null);
        }

        public string GetDotnetPath()
        {
            var envDotnetHome = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            var dotnetHome = envDotnetHome != null ? Environment.ExpandEnvironmentVariables(envDotnetHome) : _defaultDotnetHome;

            if (Directory.Exists(dotnetHome))
            {
                return dotnetHome;
            }
            else
            {
                return null;
            }
        }

        public string GetDotnetExecutable()
        {
            var dotnetPath = GetDotnetPath();
            if (dotnetPath != null)
            {
                return Path.Combine(dotnetPath, _executablePath);
            }
            else
            {
                return null;
            }
        }

        public string BuildGlobalJson()
        {
            return JsonConvert.SerializeObject(new
            {
                projects = new[]
                {
                    "."
                }
            });
        }
    }
}
