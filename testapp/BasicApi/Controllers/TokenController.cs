﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BasicApi.Controllers
{
    public class TokenController : ControllerBase
    {
        private static readonly Dictionary<string, ClaimsIdentity> _identities;

        static TokenController()
        {
            _identities = new Dictionary<string, ClaimsIdentity>(StringComparer.Ordinal);

            var reader = new ClaimsIdentity();
            reader.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, "reader@example.com"));
            reader.AddClaim(new Claim("scope", "pet-store-reader"));
            _identities.Add("reader@example.com", reader);

            var writer = new ClaimsIdentity();
            writer.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, "writer@example.com"));
            writer.AddClaim(new Claim("scope", "pet-store-reader"));
            writer.AddClaim(new Claim("scope", "pet-store-writer"));
            _identities.Add("writer@example.com", writer);
        }

        private readonly SigningCredentials _credentials;
        private readonly JwtBearerOptions _options;

        public TokenController(
            IOptions<JwtBearerOptions> options,
            SigningCredentials credentials)
        {
            _options = options.Value;
            _credentials = credentials;
        }

        [HttpGet("/token")]
        public IActionResult GetToken(string username)
        {
            ClaimsIdentity identity;
            if (username == null || !_identities.TryGetValue(username, out identity))
            {
                return new StatusCodeResult(403);
            }

            var handler = _options.SecurityTokenValidators.OfType<JwtSecurityTokenHandler>().First();

            var securityToken = handler.CreateToken(
                issuer: _options.TokenValidationParameters.ValidIssuer,
                audience: _options.TokenValidationParameters.ValidAudience,
                signingCredentials: _credentials,
                subject: identity);

            var token = handler.WriteToken(securityToken);
            return Content(token);
        }
    }
}
