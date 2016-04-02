// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.FileProviders;

namespace RazorCodeGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                var dump = false;
                var iterations = 15;
                if (args.Length > 1 && args[1] == "--dump")
                {
                    dump = true;
                    iterations = 1;
                }
                else if (args.Length > 1)
                {
                    iterations = int.Parse(args[1]);
                }

                GenerateCodeFile(Path.GetFullPath(args[0]), "Test", iterations, dump);

                Console.WriteLine("Press the ANY key to exit.");
                Console.ReadLine();
                return 0;
            }
            else
            {
                Console.WriteLine("usage: dnx run <file.cshtml>");
                return -1;
            }
        }

        private static void GenerateCodeFile(string file, string @namespace, int iterations, bool dump)
        {
            var basePath = Path.GetDirectoryName(file);
            var fileName = Path.GetFileName(file);

            var fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
            var codeLang = new CSharpRazorCodeLanguage();

            var chunkTreeCache = new DefaultChunkTreeCache(new PhysicalFileProvider(basePath));
            var resolver = new TagHelperDescriptorResolver(designTime: false);
            var host = new MvcRazorHost(chunkTreeCache, resolver);
            var engine = new RazorTemplateEngine(host);

            Console.WriteLine("Press the ANY key to start.");
            Console.ReadLine();

            Console.WriteLine($"Starting Code Generation: {file}");
            var timer = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                using (var fileStream = File.OpenText(file))
                {
                    var code = engine.GenerateCode(
                        input: fileStream,
                        className: fileNameNoExtension,
                        rootNamespace: Path.GetFileName(@namespace),
                        sourceFileName: fileName);
                    
                    if (dump)
                    {
                        File.WriteAllText(Path.ChangeExtension(file, ".cs"), code.GeneratedCode);
                    }
                }

                Console.WriteLine("Completed iteration: " + (i + 1));
            }
            Console.WriteLine($"Completed after {timer.Elapsed}");
        }
    }
}