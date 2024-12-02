// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET6_0_OR_GREATER
#endif

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Behavior when synchronously validating <see cref="AsyncValidationAttribute"/>.
/// </summary>
public enum AsyncValidationBehavior
{
    /// <summary>
    /// Throw exception on validating <see cref="AsyncValidationAttribute"/>.
    /// </summary>
    Throw = 1,

    /// <summary>
    /// Ignore <see cref="AsyncValidationAttribute"/> on validating.
    /// </summary>
    Ignore = 2,

    /// <summary>
    /// Try validate <see cref="AsyncValidationAttribute"/> synchronously.
    /// </summary>
    /// <remarks>Possible deadlock, please use with caution.</remarks>
    TrySynchronously = 3
}