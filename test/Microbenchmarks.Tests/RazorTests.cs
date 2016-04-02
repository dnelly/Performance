// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Benchmarks.Framework;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microbenchmarks.Tests.Razor
{
    public class RazorTests : BenchmarkTestBase
    {
        [Benchmark]
        [BenchmarkVariation("Runtime", false)]
        [BenchmarkVariation("Design Time", true)]
        public void TagHelperResolution(bool designTime)
        {
            var descriptorResolver = new TagHelperDescriptorResolver(designTime);
            var errorSink = new ErrorSink();
            var addTagHelperDirective = new TagHelperDirectiveDescriptor
            {
                DirectiveText = "*, Microsoft.AspNetCore.Mvc.TagHelpers",
                DirectiveType = TagHelperDirectiveType.AddTagHelper,
                Location = SourceLocation.Zero
            };
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new[] { addTagHelperDirective },
                errorSink);
            IEnumerable<TagHelperDescriptor> descriptors;

            using (Collector.StartCollection())
            {
                descriptors = descriptorResolver.Resolve(resolutionContext);
            }

            Assert.NotEmpty(descriptors);
            Assert.Empty(errorSink.Errors);
        }

        [Benchmark]
        [BenchmarkVariation("Runtime", false)]
        [BenchmarkVariation("Design Time", true)]
        public void ViewParsing(bool designTime)
        {
            // Arrange
            var chunkTreeCache = new DefaultChunkTreeCache(new TestFileProvider());
            var resolver = new TagHelperDescriptorResolver(designTime: false);
            var razorHost = new MvcRazorHost(chunkTreeCache, resolver)
            {
                DesignTimeMode = designTime
            };
            var assembly = typeof(RazorTests).GetTypeInfo().Assembly;
            var assemblyName = assembly.GetName().Name;
            var stream = assembly.GetManifestResourceStream($"{assemblyName}.compiler.resources.RazorTests.TestFile.cshtml");
            GeneratorResults result;

            // Act
            using (Collector.StartCollection())
            {
                result = razorHost.GenerateCode("test/path", stream);
            }

            // Assert
            Assert.Empty(result.ErrorSink.Errors);
            Assert.Empty(result.ParserErrors);
            Assert.True(result.Success);
        }

        private class TestFileProvider : IFileProvider
        {
            public virtual IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotSupportedException();
            }

            public virtual IFileInfo GetFileInfo(string subpath)
            {
                return new NotFoundFileInfo();
            }

            public virtual IChangeToken Watch(string filter)
            {
                return new TestFileChangeToken();
            }

            private class NotFoundFileInfo : IFileInfo
            {
                public bool Exists => false;

                public bool IsDirectory
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public DateTimeOffset LastModified
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public long Length
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public string Name
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public string PhysicalPath
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public Stream CreateReadStream()
                {
                    throw new NotImplementedException();
                }
            }

            private class TestFileChangeToken : IChangeToken
            {
                public bool ActiveChangeCallbacks => false;

                public bool HasChanged { get; set; }

                public IDisposable RegisterChangeCallback(Action<object> callback, object state)
                {
                    return new NullDisposable();
                }

                private class NullDisposable : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }
        }
    }
}
