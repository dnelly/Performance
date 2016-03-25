// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Benchmarks.Utility.Helpers
{
    public class SampleManager
    {
        private readonly Dictionary<Type, List<SampleEntry>> _samples = new Dictionary<Type, List<SampleEntry>>();

        public string GetUnrestoredSample(string name)
        {
            throw new NotImplementedException();
        }

        public string GetRestoredSample(string name)
        {
            var sample = GetOrAdd(name, RestoredSample.Create);
            sample.Initialize();

            return sample.Valid ? sample.SamplePath : null;
        }

        public string GetDotNetPublishedSample(string name, string framework)
        {
            var sample = GetOrAdd(DotNetPublishedSample.GetUniqueName(name, framework), DotNetPublishedSample.Create);
            sample.Initialize();

            return sample.Valid ? sample.SamplePath : null;
        }

        private SampleEntry GetOrAdd<T>(string name, Func<string, T> factory) where T : SampleEntry
        {
            List<SampleEntry> samples;
            if (!_samples.TryGetValue(typeof(T), out samples))
            {
                samples = new List<SampleEntry>();
                _samples[typeof(T)] = samples;
            }

            var sample = samples.FirstOrDefault(entry => string.Equals(name, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (sample == null)
            {
                sample = factory(name);
                samples.Add(sample);
            }

            return sample;
        }

        private abstract class SampleEntry
        {
            private bool _initialized;

            public SampleEntry(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public string SourcePath { get; protected set; }

            public string SamplePath { get; protected set; }

            public bool Valid => SamplePath != null && Directory.Exists(SamplePath);

            public void Initialize()
            {
                if (!_initialized)
                {
                    _initialized = doInitialization();
                }
            }

            protected abstract bool doInitialization();
        }

        private class RestoredSample : SampleEntry
        {
            private RestoredSample(string name) : base(name) { }

            public static RestoredSample Create(string name) => new RestoredSample(name);

            protected override bool doInitialization()
            {
                SourcePath = PathHelper.GetTestAppFolder(Name);
                if (SourcePath == null)
                {
                    return false;
                }

                var target = Path.Combine(PathHelper.GetNewTempFolder(), Name);
                Directory.CreateDirectory(target);

                CommandLineRunner.GetDefaultInstance().Execute($"robocopy {SourcePath} {target} /E /S /XD node_modules /XF project.lock.json");
                if (!DotnetHelper.GetDefaultInstance().Restore(target, quiet: true))
                {
                    Directory.Delete(target, recursive: true);
                    return false;
                }

                SamplePath = target;
                return true;
            }
        }

        private class DotNetPublishedSample : SampleEntry
        {
            private const char _separator = '|';

            private DotNetPublishedSample(string name) : base(name) { }

            public static DotNetPublishedSample Create(string name) => new DotNetPublishedSample(name);

            public static string GetUniqueName(string sampleName, string framework) => $"{sampleName}{_separator}{framework}";

            protected override bool doInitialization()
            {
                var parts = Name.Split(_separator);
                SourcePath = PathHelper.GetTestAppFolder(parts[0]);
                if (SourcePath == null)
                {
                    return false;
                }

                var dotnet = DotnetHelper.GetDefaultInstance();
                if (!dotnet.Restore(SourcePath, quiet: true))
                {
                    return false;
                }

                var target = Path.Combine(PathHelper.GetNewTempFolder(), parts[0]);
                Directory.CreateDirectory(target);

                if (!dotnet.Publish(SourcePath, target, parts[1]))
                {
                    Directory.Delete(target, recursive: true);
                    return false;
                }
                
                SamplePath = target;
                return true;
            }
        }
    }
}
