// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Benchmarks.Framework;
using Benchmarks.Utility.Helpers;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Tests.Performance
{
    public class PublishTests : BenchmarkTestBase, IClassFixture<SampleManager>
    {
        private readonly SampleManager _sampleManager;

        public PublishTests(SampleManager sampleManager)
        {
            _sampleManager = sampleManager;
        }

        [Benchmark(Iterations = 5)]
        [BenchmarkVariation("DotnetPublish_BasicKestrel", "BasicKestrel")]
        [BenchmarkVariation("DotnetPublish_StarterMvc", "StarterMvc")]
        public void DotnetPublish(string sampleName)
        {
            var framework = PlatformServices.Default.Runtime.RuntimeType;
            var testName = $"{sampleName}.{framework}.{nameof(DotnetPublish)}";
            var testProject = _sampleManager.GetRestoredSample(sampleName);
            Assert.True(testProject != null, $"Fail to set up test project.");

            var testOutput = Path.Combine(PathHelper.GetNewTempFolder(), testName);
            Directory.CreateDirectory(testOutput);

            using (Collector.StartCollection())
            {
                DotnetHelper.GetDefaultInstance().Publish(
                    workingDir: testProject,
                    outputDir: testOutput);
            }
        }
    }
}

