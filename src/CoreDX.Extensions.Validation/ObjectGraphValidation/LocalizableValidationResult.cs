// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Provide metadata for localize error message.
/// </summary>
public interface IAttributeValidationResultLocalizationMetadata
{
    /// <summary>
    /// Gets member name of validated field.
    /// </summary>
    string? MemberName { get; }

    /// <summary>
    /// Gets display name of validated field.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets <see cref="ValidationAttribute"/> of validated field.
    /// </summary>
    ValidationAttribute Attribute { get; }
}

/// <summary>
/// Represents a container for the results of a validation request that can localize error message custom.
/// </summary>
public class LocalizableValidationResult : ValidationResult, IAttributeValidationResultLocalizationMetadata
{
    /// <summary>
    /// Gets the <see cref="ValidationAttribute"/> for this validation result.
    /// </summary>
    public ValidationAttribute Attribute { get; }

    /// <summary>
    /// Gets the <see cref="ValidationContext"/> for this validation result.
    /// </summary>
    public ValidationContext Context { get; }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public string? MemberName => Context.MemberName;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName => Context.DisplayName;

    /// <summary>
    /// Gets the type of the object being validated.
    /// </summary>
    public Type InstanceObjectType => Context.ObjectType;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizableValidationResult"/> class by using an error message and a list of members that have validation errors.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="attribute">The <see cref="ValidationAttribute"/> for this validation result.</param>
    /// <param name="context">The <see cref="ValidationContext"/> for this validation result.</param>
    public LocalizableValidationResult(
        string? errorMessage,
        ValidationAttribute attribute,
        ValidationContext context)
        : this(errorMessage, null, attribute, context)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizableValidationResult"/> class by using an error message and a list of members that have validation errors.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="memberNames">The list of member names that have validation errors.</param>
    /// <param name="attribute">The <see cref="ValidationAttribute"/> for this validation result.</param>
    /// <param name="context">The <see cref="ValidationContext"/> for this validation result.</param>
    public LocalizableValidationResult(
        string? errorMessage,
        IEnumerable<string>? memberNames,
        ValidationAttribute attribute,
        ValidationContext context)
        : base(errorMessage, memberNames)
    {
        Attribute = attribute;
        Context = context;
    }

    /// <inheritdoc />
    protected LocalizableValidationResult(
        ValidationResult validationResult,
        ValidationAttribute attribute,
        ValidationContext context) : base(validationResult)
    {
        Attribute = attribute;
        Context = context;
    }
}
