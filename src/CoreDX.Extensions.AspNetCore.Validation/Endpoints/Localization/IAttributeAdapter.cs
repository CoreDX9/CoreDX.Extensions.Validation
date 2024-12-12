using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.AspNetCore.Http.Validation.Localization;

/// <summary>
/// An interface of <see cref="AttributeAdapterBase{TAttribute}"/> which wraps up all the required interfaces for the adapters.
/// </summary>
public interface IAttributeAdapter
{
    /// <summary>
    /// Gets the type of adapter can process.
    /// </summary>
    Type CanProcessAttributeType { get; }

    /// <summary>
    /// Get localization arguments from <see cref="ValidationAttribute"/>.
    /// </summary>
    /// <param name="attribute">The validation attribute.</param>
    /// <returns>Arguments to localize error message.</returns>
    object[]? GetLocalizationArguments(ValidationAttribute attribute);
}
