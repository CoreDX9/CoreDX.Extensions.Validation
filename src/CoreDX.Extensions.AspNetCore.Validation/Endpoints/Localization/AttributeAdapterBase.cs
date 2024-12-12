using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.AspNetCore.Http.Validation.Localization;

/// <summary>
/// An abstract subclass of <see cref="AttributeAdapterBase{TAttribute}"/> which wraps up all the required interfaces for the adapters.
/// </summary>
/// <typeparam name="TAttribute">The type of <see cref="ValidationAttribute"/> which is being wrapped.</typeparam>
public abstract class AttributeAdapterBase<TAttribute> : IAttributeAdapter
    where TAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    public Type CanProcessAttributeType => typeof(TAttribute);

    /// <inheritdoc/>
    public object[]? GetLocalizationArguments(ValidationAttribute attribute)
    {
        return GetLocalizationArgumentsInternal((TAttribute)attribute);
    }

    /// <summary>
    /// Get localization arguments from <typeparamref name="TAttribute"/>.
    /// </summary>
    /// <param name="attribute">The validation attribute.</param>
    /// <returns>Arguments to localize error message.</returns>
    protected abstract object[]? GetLocalizationArgumentsInternal(TAttribute attribute);
}
