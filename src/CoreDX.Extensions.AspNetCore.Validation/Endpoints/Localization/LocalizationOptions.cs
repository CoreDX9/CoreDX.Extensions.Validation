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

    /// <summary>
    /// Creates a new instance of <see cref="EndpointParameterValidationLocalizationOptions"/>.
    /// </summary>
    public EndpointParameterValidationLocalizationOptions()
    {
        Adapters.Add(new CompareAttributeAdapter());
        Adapters.Add(new DataTypeAttributeAdapter());
        Adapters.Add(new FileExtensionsAttributeAdapter());
        Adapters.Add(new MaxLengthAttributeAdapter());
        Adapters.Add(new MinLengthAttributeAdapter());
        Adapters.Add(new RangeAttributeAdapter());
        Adapters.Add(new RegularExpressionAttributeAdapter());
        Adapters.Add(new RequiredAttributeAdapter());
        Adapters.Add(new StringLengthAttributeAdapter());
        Adapters.Add(new AttributeAdapter());
    }
}
