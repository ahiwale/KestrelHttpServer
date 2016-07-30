// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestLineSize = maxRequestLineSize;
            }))
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}");
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(int.MaxValue - 1, int.MaxValue)]
        public void ServerFailsToStartWhenMaxRequestBufferSizeIsLessThanMaxRequestLineSize(long maxRequestBufferSize, int maxRequestLineSize)
        {
            using (var host = BuildWebHost(options =>
            {
                options.MaxRequestBufferSize = maxRequestBufferSize;
                options.Limits.MaxRequestLineSize = maxRequestLineSize;
            }))
            {
                Assert.Throws<InvalidOperationException>(() => host.Start());
            }
        }

        private IWebHost BuildWebHost(Action<KestrelServerOptions> options)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options)
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app => app.Run(async context =>
                {
                    await context.Response.WriteAsync("hello, world");
                }))
                .Build();

            return host;
        }
    }
}
