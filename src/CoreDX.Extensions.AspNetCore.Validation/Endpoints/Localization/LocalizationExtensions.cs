using CoreDX.Extensions.AspNetCore.Http.Validation.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Helper class for register endpoint parameter data annotations localization services.
/// </summary>
public static class EndpointParameterValidationLocalizationExtensions
{
    /// <summary>
    /// Add endpoint parameter data annotations localization services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAction">A function to configure options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEndpointParameterDataAnnotationsLocalization(
        this IServiceCollection services,
        Action<EndpointParameterValidationLocalizationOptions>? configureAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<EndpointParameterValidationLocalizationOptions>(options =>
        {
            configureAction?.Invoke(options);

            options.Adapters.Add(new CompareAttributeAdapter());
            options.Adapters.Add(new DataTypeAttributeAdapter());
            options.Adapters.Add(new FileExtensionsAttributeAdapter());
            options.Adapters.Add(new MaxLengthAttributeAdapter());
            options.Adapters.Add(new MinLengthAttributeAdapter());
            options.Adapters.Add(new RangeAttributeAdapter());
            options.Adapters.Add(new RegularExpressionAttributeAdapter());
            options.Adapters.Add(new RequiredAttributeAdapter());
            options.Adapters.Add(new StringLengthAttributeAdapter());
            options.Adapters.Add(new AttributeAdapter());
        });

        return services;
    }
}
