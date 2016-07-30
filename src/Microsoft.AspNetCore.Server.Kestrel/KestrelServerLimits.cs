// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServerLimits
    {
        // Matches the default large_client_header_buffers in nginx.
        private int _maxRequestLineSize = 8 * 1024;

        /// <summary>
        /// Gets or sets the maximum allowed size for the HTTP request line.
        /// </summary>
        /// <remarks>
        /// Defaults to 8,192 bytes (8 KB).
        /// </remarks>
        public int MaxRequestLineSize
        {
            get
            {
                return _maxRequestLineSize;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be a positive integer.");
                }
                _maxRequestLineSize = value;
            }
        }
    }
}
