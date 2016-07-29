// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class BadHttpRequestTests
    {
        // All test cases for this theory must end in '\n', otherwise the server will spin forever
        [Theory]
        // Incomplete request lines
        [InlineData("G\r\n")]
        [InlineData("GE\r\n")]
        [InlineData("GET\r\n")]
        [InlineData("GET \r\n")]
        [InlineData("GET /\r\n")]
        [InlineData("GET / \r\n")]
        [InlineData("GET / H\r\n")]
        [InlineData("GET / HT\r\n")]
        [InlineData("GET / HTT\r\n")]
        [InlineData("GET / HTTP\r\n")]
        [InlineData("GET / HTTP/\r\n")]
        [InlineData("GET / HTTP/1\r\n")]
        [InlineData("GET / HTTP/1.\r\n")]
        // Missing method
        [InlineData(" \r\n")]
        // Missing second space
        [InlineData("/ \r\n")] // This fails trying to read the '/' because that's invalid for an HTTP method
        [InlineData("GET /\r\n")]
        // Missing target
        [InlineData("GET  \r\n")]
        // Missing version
        [InlineData("GET / \r\n")]
        // Missing CR
        [InlineData("GET / \n")]
        // Unrecognized HTTP version
        [InlineData("GET / http/1.0\r\n")]
        [InlineData("GET / http/1.1\r\n")]
        [InlineData("GET / HTTP/1.1 \r\n")]
        [InlineData("GET / HTTP/1.1a\r\n")]
        [InlineData("GET / HTTP/1.0\n\r\n")]
        [InlineData("GET / HTTP/1.2\r\n")]
        [InlineData("GET / HTTP/3.0\r\n")]
        [InlineData("GET / H\r\n")]
        [InlineData("GET / HTTP/1.\r\n")]
        [InlineData("GET / hello\r\n")]
        [InlineData("GET / 8charact\r\n")]
        // Missing LF after CR
        [InlineData("GET / HTTP/1.0\rA\n")]
        // Bad HTTP Methods (invalid according to RFC)
        [InlineData("( / HTTP/1.0\r\n")]
        [InlineData(") / HTTP/1.0\r\n")]
        [InlineData("< / HTTP/1.0\r\n")]
        [InlineData("> / HTTP/1.0\r\n")]
        [InlineData("@ / HTTP/1.0\r\n")]
        [InlineData(", / HTTP/1.0\r\n")]
        [InlineData("; / HTTP/1.0\r\n")]
        [InlineData(": / HTTP/1.0\r\n")]
        [InlineData("\\ / HTTP/1.0\r\n")]
        [InlineData("\" / HTTP/1.0\r\n")]
        [InlineData("/ / HTTP/1.0\r\n")]
        [InlineData("[ / HTTP/1.0\r\n")]
        [InlineData("] / HTTP/1.0\r\n")]
        [InlineData("? / HTTP/1.0\r\n")]
        [InlineData("= / HTTP/1.0\r\n")]
        [InlineData("{ / HTTP/1.0\r\n")]
        [InlineData("} / HTTP/1.0\r\n")]
        [InlineData("get@ / HTTP/1.0\r\n")]
        [InlineData("post= / HTTP/1.0\r\n")]
        public async Task TestInvalidRequestLines(string request)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd(request);
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        // TODO: remove test once people agree to change this behavior
        /*
        [Theory]
        [InlineData(" ")]
        [InlineData("GET  ")]
        [InlineData("GET / HTTP/1.2\r")]
        [InlineData("GET / HTTP/1.0\rA")]
        // Bad HTTP Methods (invalid according to RFC)
        [InlineData("( ")]
        [InlineData(") ")]
        [InlineData("< ")]
        [InlineData("> ")]
        [InlineData("@ ")]
        [InlineData(", ")]
        [InlineData("; ")]
        [InlineData(": ")]
        [InlineData("\\ ")]
        [InlineData("\" ")]
        [InlineData("/ ")]
        [InlineData("[ ")]
        [InlineData("] ")]
        [InlineData("? ")]
        [InlineData("= ")]
        [InlineData("{ ")]
        [InlineData("} ")]
        [InlineData("get@ ")]
        [InlineData("post= ")]
        public async Task ServerClosesConnectionAsSoonAsBadRequestLineIsDetected(string request)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(request);
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }
        */

        [Theory]
        // Missing final CRLF
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n")]
        // Leading whitespace
        [InlineData(" Header-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\n Header-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\n\tHeader-2: value2\r\n\r\n")]
        // Missing LF
        [InlineData("Header-1: value1\rHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\r\n")]
        // Line folding
        [InlineData("Header-1: multi\r\n line\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: multi\r\n line\r\n\r\n")]
        // Missing ':'
        [InlineData("Header-1 value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 value2\r\n\r\n")]
        // Whitespace in header name
        [InlineData("Header 1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader 2: value2\r\n\r\n")]
        [InlineData("Header-1 : value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1\t: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 : value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2\t: value2\r\n\r\n")]
        public async Task TestInvalidHeaders(string rawHeaders)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd($"GET / HTTP/1.1\r\n{rawHeaders}");
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        [Fact]
        public async Task BadRequestWhenNameHeaderNamesContainsNonASCIICharacters()
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd(
                        "GET / HTTP/1.1",
                        "H\u00eb\u00e4d\u00ebr: value",
                        "",
                        "");
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        [Theory]
        [InlineData("\0")]
        [InlineData("%00")]
        [InlineData("/\0")]
        [InlineData("/%00")]
        [InlineData("/\0\0")]
        [InlineData("/%00%00")]
        [InlineData("/%C8\0")]
        [InlineData("/%E8%00%84")]
        [InlineData("/%E8%85%00")]
        [InlineData("/%F3%00%82%86")]
        [InlineData("/%F3%85%00%82")]
        [InlineData("/%F3%85%82%00")]
        [InlineData("/%E8%85%00")]
        [InlineData("/%E8%01%00")]
        public async Task BadRequestIfPathContainsNullCharacters(string path)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd($"GET {path} HTTP/1.1\r\n");
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        private async Task ReceiveBadRequestResponse(TestConnection connection)
        {
            await connection.Receive(
                "HTTP/1.1 400 Bad Request",
                "");
            await connection.Receive(
                "Connection: close",
                "");
            await connection.ReceiveForcedEnd(
                $"Date: {connection.Server.Context.DateHeaderValue}",
                "Content-Length: 0",
                "",
                "");
        }
    }
}
