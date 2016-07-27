// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class MaxRequestLineSizeTests
    {
        [Fact]
        public async Task ServerReturnsBadRequestWhenRequestLineExceedsLimit()
        {
            var maxRequestLineSize = "GET / HTTP/1.1\r\n".Length - 1; // stop short of the '\n'

            using (var host = StartWebHost(maxRequestLineSize))
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}");
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        private IWebHost StartWebHost(int maxRequestLineSize)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.MaxRequestLineSize = maxRequestLineSize;
                })
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app => app.Run(async context =>
                {
                    await context.Response.WriteAsync("hello, world");
                }))
                .Build();
            host.Start();

            return host;
        }
    }
}
