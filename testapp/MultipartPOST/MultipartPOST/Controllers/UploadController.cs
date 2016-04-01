// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MultipartPost.Controllers
{
    [Route("api/upload")]
    public class UploadController : Controller
    {
        public IActionResult Post()
        {
            if (!HasMultipartFormContentType(Request.ContentType))
            {
                return BadRequest("Expecting a multipart content type for the Upload POST command");
            }

            PrintLine("Processing a POST request to /api/upload");

            var form = Request.Form;
            PrintLine($"File count: { form.Files.Count }");
            foreach(var file in form.Files)
            {
                PrintLine($"Read { file.Length } bytes [{ file.FileName }]");
            }

            PrintLine("Done");

            return Ok();
        }

        private static bool HasMultipartFormContentType(string contentType)
        {
            return contentType != null && contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase);
        }

        private void PrintLine(string input, params object[] paramStrings)
        {
            Console.Write($"[{ DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }] ");
            Console.WriteLine(input, paramStrings);
        }
    }
}
