// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Serves as the base class for all async validation attributes.
/// </summary>
public abstract class AsyncValidationAttribute : ValidationAttribute
{
    private volatile bool _hasBaseIsValidAsync;

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

    private protected ValidationResult CreateFailedValidationResult(ValidationContext validationContext)
    {
        string[]? memberNames = validationContext.MemberName is { } memberName
            ? new[] { memberName }
            : null;

        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
    }

    /// <summary>
    /// Don't use this. Use <see cref="IsValidAsync(object, ValidationContext, CancellationToken)"/> instead of this.
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
    /// Don't use this. Use <see cref="IsValidAsync(object, CancellationToken)"/> instead of this.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>An instance of the <see cref="ValidationResult"/> class.</returns>
    /// <exception cref="InvalidOperationException">always</exception>
    public override sealed bool IsValid(object? value)
    {
        throw new InvalidOperationException("Async validation called synchronously.");
    }

    /// <summary>
    ///     Gets the value indicating whether or not the specified <paramref name="value" /> is valid
    ///     with respect to the current validation attribute.
    ///     <para>
    ///         Derived classes should not override this method as it is only available for backwards compatibility.
    ///         Instead, implement <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     The preferred public entry point for clients requesting validation is the <see cref="GetValidationResultAsync" />
    ///     method.
    /// </remarks>
    /// <param name="value">The value to validate</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <paramref name="value" /> is acceptable, <c>false</c> if it is not acceptable</returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    /// <exception cref="NotImplementedException">
    ///     is thrown when neither overload of IsValid has been implemented
    ///     by a derived class.
    /// </exception>
    public virtual async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken = default)
    {
        if (!_hasBaseIsValidAsync)
        {
            // track that this method overload has not been overridden.
            _hasBaseIsValidAsync = true;
        }

        // call overridden method.
        // The IsValid method without a validationContext predates the one accepting the context.
        // This is theoretically unreachable through normal use cases.
        // Instead, the overload using validationContext should be called.
        return await IsValidAsync(value, null!) == ValidationResult.Success;
    }

    /// <summary>
    ///     Protected virtual method to override and implement validation logic.
    ///     <para>
    ///         Derived classes should override this method instead of <see cref="IsValidAsync(object, CancellationToken)" />, which is deprecated.
    ///     </para>
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">
    ///     A <see cref="ValidationContext" /> instance that provides
    ///     context about the validation operation, such as the object and member being validated.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     When validation is valid, <see cref="ValidationResult.Success" />.
    ///     <para>
    ///         When validation is invalid, an instance of <see cref="ValidationResult" />.
    ///     </para>
    /// </returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    /// <exception cref="NotImplementedException">
    ///     is thrown when <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" />
    ///     has not been implemented by a derived class.
    /// </exception>
    protected virtual async ValueTask<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken = default)
    {
        if (_hasBaseIsValidAsync)
        {
            // this means neither of the IsValidAsync methods has been overridden, throw.
            throw new NotImplementedException("IsValidAsync(object value, CancellationToken cancellationToken) has not been implemented by this class.  The preferred entry point is GetValidationResultAsync() and classes should override IsValidAsync(object value, ValidationContext context, CancellationToken cancellationToken).");
        }

        // call overridden method.
        return await IsValidAsync(value, cancellationToken)
            ? ValidationResult.Success
            : CreateFailedValidationResult(validationContext);
    }

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
    ///     Validates the specified <paramref name="value" /> and throws <see cref="ValidationException" /> if it is not.
    ///     <para>
    ///         The overloaded <see cref="ValidateAsync(object, ValidationContext, CancellationToken)" /> is the recommended entry point as it
    ///         can provide additional context to the <see cref="ValidationAttribute" /> being validated.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     This base method invokes the <see cref="IsValidAsync(object, CancellationToken)" /> method to determine whether or not the
    ///     <paramref name="value" /> is acceptable.  If <see cref="IsValidAsync(object, CancellationToken)" /> returns <c>false</c>, this base
    ///     method will invoke the <see cref="ValidationAttribute.FormatErrorMessage" /> to obtain a localized message describing
    ///     the problem, and it will throw a <see cref="ValidationException" />
    /// </remarks>
    /// <param name="value">The value to validate</param>
    /// <param name="name">The string to be included in the validation error message if <paramref name="value" /> is not valid</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ValidationException">
    ///     is thrown if <see cref="IsValidAsync(object, CancellationToken)" /> returns <c>false</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    public async Task ValidateAsync(object? value, string name, CancellationToken cancellationToken = default)
    {
        if (!(await IsValidAsync(value, cancellationToken)))
        {
            throw new ValidationException(FormatErrorMessage(name), this, value);
        }
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
