// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Serves as the base class for all async validation attributes.
/// </summary>
public abstract class AsyncValidationAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class.
    /// </summary>
    protected AsyncValidationAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class by using the function that enables to access to validation resources.
    /// </summary>
    /// <param name="errorMessageAccessor">The function that enables to access to validation resources.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="errorMessageAccessor"/> is null.</exception>
    protected AsyncValidationAttribute(Func<string> errorMessageAccessor) : base(errorMessageAccessor) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidationAttribute"/> class by using the error message to associate with a validation control.
    /// </summary>
    /// /// <param name="errorMessage">The error message to associate with a validation control.</param>
    protected AsyncValidationAttribute(string errorMessage) : base(errorMessage) { }

    /// <summary>
    /// Don't use this.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the operation.</param>
    /// <returns>An instance of the <see cref="ValidationResult"/> class.</returns>
    /// <exception cref="InvalidOperationException">always</exception>
    protected override sealed ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        throw new InvalidOperationException("Async validation called synchronously.");
    }

    /// <summary>
    /// Validates the specified value with respect to the current validation attribute.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the operation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An instance of the <see cref="ValidationResult"/> class.</returns>
    protected abstract ValueTask<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests whether the given <paramref name="value"/> is valid with respect to the current
    /// validation attribute without throwing a <see cref="ValidationException"/>
    /// </summary>
    /// <remarks>
    /// If this method returns <see cref="ValidationResult.Success"/>, then validation was successful, otherwise
    /// an instance of <see cref="ValidationResult"/> will be returned with a guaranteed non-null
    /// <see cref="ValidationResult.ErrorMessage"/>.
    /// </remarks>
    /// <param name="value">The value to validate</param>
    /// <param name="validationContext">A <see cref="ValidationContext"/> instance that provides
    /// context about the validation operation, such as the object and member being validated.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// When validation is valid, <see cref="ValidationResult.Success"/>.
    /// <para>
    /// When validation is invalid, an instance of <see cref="ValidationResult"/>.
    /// </para>
    /// </returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="InvalidOperationException"> is thrown when <see cref="IsValid(object, ValidationContext)" />
    /// is called.
    /// </exception>
    public async Task<ValidationResult?> GetValidationResultAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken = default)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        ValidationResult? result = await IsValidAsync(value, validationContext, cancellationToken);

        // If validation fails, we want to ensure we have a ValidationResult that guarantees it has an ErrorMessage
        if (result != null)
        {
            if (string.IsNullOrEmpty(result.ErrorMessage))
            {
                var errorMessage = FormatErrorMessage(validationContext.DisplayName);
                result = new ValidationResult(errorMessage, result?.MemberNames);
            }
        }

        return result;
    }

    /// <summary>
    /// Validates the specified <paramref name="value"/> and throws <see cref="ValidationException"/> if it is not.
    /// </summary>
    /// <remarks>This method invokes the <see cref="IsValidAsync(object, ValidationContext, CancellationToken)"/> method 
    /// to determine whether or not the <paramref name="value"/> is acceptable given the <paramref name="validationContext"/>.
    /// If that method doesn't return <see cref="ValidationResult.Success"/>, this base method will throw
    /// a <see cref="ValidationException"/> containing the <see cref="ValidationResult"/> describing the problem.
    /// </remarks>
    /// <param name="value">The value to validate</param>
    /// <param name="validationContext">Additional context that may be used for validation.  It cannot be null.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ValidationException"> is thrown if <see cref="IsValidAsync(object, ValidationContext, CancellationToken)"/> 
    /// doesn't return <see cref="ValidationResult.Success"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    /// <exception cref="NotImplementedException"> is thrown when <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" />
    /// has not been implemented by a derived class.
    /// </exception>
    public async Task ValidateAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken = default)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        ValidationResult? result = await GetValidationResultAsync(value, validationContext, cancellationToken: cancellationToken);

        if (result != null)
        {
            // Convenience -- if implementation did not fill in an error message,
            throw new ValidationException(result, this, value);
        }
    }
}
