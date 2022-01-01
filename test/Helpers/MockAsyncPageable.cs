//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2022 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Moq;

namespace DotNetCore.Azure.Configuration.KvCerfificates.Tests.Helpers
{
    internal class MockAsyncPageable<T> : AsyncPageable<T>
    {
        private readonly List<T> _page;

        public MockAsyncPageable(List<T> page)
        {
            _page = page;
        }

        public override async IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
        {
            yield return Page<T>.FromValues(_page, null, Mock.Of<Response>());
            await Task.CompletedTask;
        }
    }
}
