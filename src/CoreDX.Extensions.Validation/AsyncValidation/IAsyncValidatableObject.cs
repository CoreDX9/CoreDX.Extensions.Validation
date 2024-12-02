// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Provides a way for an object to be validated.
/// </summary>
public interface IAsyncValidatableObject
{
    /// <summary>
    /// Determines whether the specified object is valid.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A collection that holds failed-validation information.</returns>
    IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, CancellationToken cancellationToken = default);
}
