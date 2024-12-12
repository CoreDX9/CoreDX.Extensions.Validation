namespace CoreDX.Extensions.AspNetCore.Http.Validation.Localization;

/// <summary>
/// Provides programmatic configuration for the endpoint parameter validation localization.
/// </summary>
public sealed class EndpointParameterValidationLocalizationOptions
{
    /// <summary>
    /// Gets the <see cref="AttributeLocalizationAdapters"/>.
    /// </summary>
    public AttributeLocalizationAdapters Adapters { get; } = new();
}
