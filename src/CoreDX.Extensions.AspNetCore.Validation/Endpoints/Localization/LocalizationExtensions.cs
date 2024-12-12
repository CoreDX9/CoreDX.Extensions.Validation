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
    /// <param name="configureOptions">A function to configure options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEndpointParameterDataAnnotationsLocalization(
        this IServiceCollection services,
        Action<EndpointParameterValidationLocalizationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<EndpointParameterValidationLocalizationOptions>(options =>
        {
            configureOptions?.Invoke(options);
        });

        return services;
    }
}
