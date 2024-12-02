// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

internal sealed class ManualResetEventSlimWithAwaiterSupport : ManualResetEventSlim
{
    private readonly Action _onCompleted;

    public ManualResetEventSlimWithAwaiterSupport()
    {
        _onCompleted = Set;
    }

#if NET6_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    public void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
    {
        awaiter.UnsafeOnCompleted(_onCompleted);
        Wait();
        Reset();
    }
}
