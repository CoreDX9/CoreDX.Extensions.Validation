using CoreDX.Extensions.AspNetCore.Http.Validation;
using CoreDX.Extensions.AspNetCore.Http.Validation.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Reflection;
using System.Security.Claims;
using static CoreDX.Extensions.AspNetCore.Http.Validation.EndpointBindingParameterValidationMetadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Helper class for Add parameter validation to endpoint pipeline.
/// </summary>
public static class EndpointParameterValidationExtensions
{
    private const string _filterLoggerName = "CoreDX.Extensions.AspNetCore.Http.Validation.EndpointParameterDataAnnotations";
    private const string _validationResultItemName = $"{_filterLoggerName}-ValidationResult";

    /// <summary>
    /// Adds a endpoint filter to validate route handler parameters automatically.
    /// </summary>
    /// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
    /// <param name="endpointConvention">The endpoint convention builder.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> AddEndpointParameterDataAnnotations<TBuilder>(
        this TBuilder endpointConvention)
        where TBuilder : IEndpointConventionBuilder
    {
        endpointConvention.Add(static endpointBuilder =>
        {
            var loggerFactory = endpointBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(_filterLoggerName);

            if (endpointBuilder.Metadata.Any(static md => md is ActionDescriptor))
            {
                logger.LogDebug("Cannot add parameter data annotations validation filter to MVC controller or Razor pages endpoint {actionName}.", endpointBuilder.DisplayName);
                return;
            }

            endpointBuilder.FilterFactories.Add((filterFactoryContext, next) =>
            {
                var loggerFactory = filterFactoryContext.ApplicationServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(_filterLoggerName);

                var parameters = filterFactoryContext.MethodInfo.GetParameters();

                List<int> bindingParameterIndexs = new(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo? parameter = parameters[i];
                    if (IsRequestDelegateFactorySpecialBoundType(parameter.ParameterType)) continue;
                    if (parameter.GetCustomAttribute<FromServicesAttribute>() is not null) continue;
#if NET8_0_OR_GREATER
                    if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is not null) continue;
#endif
                    var isServicePredicate = filterFactoryContext.ApplicationServices.GetService<IServiceProviderIsService>();
                    if (isServicePredicate?.IsService(parameter.ParameterType) is true) continue;

                    bindingParameterIndexs.Add(i);
                }

                if (bindingParameterIndexs.Count is 0)
                {
                    logger.LogDebug("Route handler method '{methodName}' does not contain any validatable parameters, skipping adding validation filter.", filterFactoryContext.MethodInfo.Name);
                }

                EndpointBindingParameterValidationMetadata? validationMetadata;
                try
                {
                    List<ParameterValidationMetadata> bindingParameters = new(bindingParameterIndexs.Count);
                    foreach (var argumentIndex in bindingParameterIndexs)
                    {
                        bindingParameters.Add(new(parameters[argumentIndex]));
                    }
                    validationMetadata = new(bindingParameters);
                }
                catch (Exception e)
                {
                    validationMetadata = null;
                    logger.LogError(e, "Build parameter validation metadate failed for route handler method '{methodName}', skipping adding validation filter.", filterFactoryContext.MethodInfo.Name);
                }

                if (validationMetadata?.Any() is not true) return invocationContext => next(invocationContext);

                endpointBuilder.Metadata.Add(validationMetadata);

                return async invocationContext =>
                {
                    var endpoint = invocationContext.HttpContext.GetEndpoint();
                    var metadata = endpoint?.Metadata
                        .FirstOrDefault(md => md is EndpointBindingParameterValidationMetadata) as EndpointBindingParameterValidationMetadata;

                    if (metadata is null) return await next(invocationContext);

                    Dictionary<string, object?> arguments = new(bindingParameterIndexs.Count);
                    foreach (var argumentIndex in bindingParameterIndexs)
                    {
                        arguments.Add(parameters[argumentIndex].Name!, invocationContext.Arguments[argumentIndex]);
                    }

                    try
                    {
                        var result = await metadata.ValidateAsync(arguments);
                        if (result != null) invocationContext.HttpContext.Items.Add(_validationResultItemName, result);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Validate parameter failed for route handler method '{methodName}'.", filterFactoryContext.MethodInfo.Name);
                    }

                    return await next(invocationContext);
                };
            });
        });

        return new(endpointConvention);
    }

    /// <summary>
    /// Adds a endpoint filter to return validation problem automatically when paramater has validation error.
    /// </summary>
    /// <param name="endpointConvention">The endpoint convention builder.</param>
    /// <param name="statusCode">The http status code.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddValidationProblemResult<TBuilder>(
        this EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> endpointConvention,
        int statusCode = StatusCodes.Status400BadRequest)
        where TBuilder : IEndpointConventionBuilder
    {
        endpointConvention.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(
                new ProducesResponseTypeMetadata(
                    statusCode,
                    typeof(HttpValidationProblemDetails),
                    ["application/problem+json", "application/json"]
                )
            );
        });

        endpointConvention.AddEndpointFilter(static async (endpointFilterInvocationContext, next) =>
        {
            var errors = endpointFilterInvocationContext.HttpContext.GetEndpointParameterDataAnnotationsProblemDetails();

            if (errors != null) return Results.ValidationProblem(errors);

            return await next(endpointFilterInvocationContext);
        });

        return endpointConvention.InnerBuilder;
    }

    /// <summary>
    /// Validate parameters using given values.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <param name="arguments">Key value pair of parameter name and value.</param>
    /// <returns>
    /// Return <see langword="true"/> if endpoint has validatable parameter and <paramref name="arguments"/> has correct parameter name and type.
    /// Otherwise, return <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<bool> TryValidateEndpointParameters(
        this HttpContext httpContext,
        params IEnumerable<KeyValuePair<string, object?>> arguments)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var endpoint = httpContext.GetEndpoint();
        var metadata = endpoint?.Metadata
            .FirstOrDefault(md => md is EndpointBindingParameterValidationMetadata) as EndpointBindingParameterValidationMetadata;
        if (metadata is null) return false;

        HashSet<string> names = [];
        foreach (var name in arguments.Select(arg => arg.Key))
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Argument's name cannot be null or empty.", nameof(arguments));
            if (!names.Add(name)) throw new ArgumentException("Argument's name must be unique.", nameof(arguments));
        }

        var newResults = await metadata.ValidateAsync(arguments.ToDictionary(arg => arg.Key, arg => arg.Value));
        if (newResults != null)
        {
            var currentResults = httpContext.GetEndpointParameterDataAnnotationsValidationResultsCore();
            if (currentResults is null)
            {
                httpContext.Items.Remove(_validationResultItemName);
                currentResults = new();
                httpContext.Items.Add(_validationResultItemName, currentResults);
            }

            foreach (var result in newResults) currentResults[result.Key] = result.Value;
        }

        return true;
    }

    /// <summary>
    /// Gets validation results.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <returns>The validation results.</returns>
    public static EndpointArgumentsValidationResults? GetEndpointParameterDataAnnotationsValidationResults(this HttpContext httpContext)
    {
        var results = httpContext.GetEndpointParameterDataAnnotationsValidationResultsCore();
        if (results is null) return null;

        return new(results
            .ToDictionary(
                r => r.Key,
                r => new ArgumentPropertiesValidationResults(r.Value.ToDictionary(
                    fr => fr.Key.ToString()!,
                    fr => fr.Value.ToImmutableList()))
            )
        );
    }

    /// <summary>
    /// Gets validation problem details.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <returns>The problem details.</returns>
    public static Dictionary<string, string[]>? GetEndpointParameterDataAnnotationsProblemDetails(this HttpContext httpContext)
    {
        Dictionary<string, string[]>? result = null;

        var validationResult = httpContext.GetEndpointParameterDataAnnotationsValidationResultsCore();
        if (validationResult?.Any(vrp => vrp.Value.Any()) is true)
        {
            var localizerFactory = httpContext.RequestServices.GetService<IStringLocalizerFactory>();
            var localizationOptions = httpContext.RequestServices.GetService<IOptions<EndpointParameterValidationLocalizationOptions>>()?.Value;
            var adapters = localizationOptions?.Adapters;

            var errors = validationResult.SelectMany(vrp => vrp.Value);
            result = localizerFactory is null || !(adapters?.Count > 0)
                ? errors
                    .ToDictionary(
                        fvr => fvr.Key.ToString()!,
                        fvr => fvr.Value.Select(ToErrorMessage).ToArray())
                : errors
                    .ToDictionary(
                        fvr => fvr.Key.ToString()!,
                        fvr => fvr.Value.Select(vr => ToLocalizedErrorMessage(vr, adapters, localizerFactory)).ToArray());
        }

        return result;

        static string ToErrorMessage(ValidationResult result)
        {
            return result.ErrorMessage!;
        }

        string ToLocalizedErrorMessage(
            ValidationResult result,
            AttributeLocalizationAdapters adapters,
            IStringLocalizerFactory localizerFactory)
        {
            if (result is LocalizableValidationResult localizable)
            {
                var adapter = adapters.FirstOrDefault(ap => localizable.Attribute.GetType().IsAssignableTo(ap.CanProcessAttributeType));
                var localizer = localizerFactory.Create(localizable.InstanceObjectType);
                return localizer
                [
                    localizable.Attribute.ErrorMessage ?? result.ErrorMessage ?? "The field {0} is invalid.",
                    [localizable.DisplayName, .. adapter?.GetLocalizationArguments(localizable.Attribute) ?? []]
                ];
            }

            return result.ErrorMessage!;
        }
    }

    internal static Dictionary<string, ValidationResultStore>? GetEndpointParameterDataAnnotationsValidationResultsCore(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Items.TryGetValue(_validationResultItemName, out var result);
        return result as Dictionary<string, ValidationResultStore>;
    }

    internal static bool IsRequestDelegateFactorySpecialBoundType(Type type) =>
        type.IsAssignableTo(typeof(HttpContext))
        || type.IsAssignableTo(typeof(HttpRequest))
        || type.IsAssignableTo(typeof(HttpResponse))
        || type.IsAssignableTo(typeof(ClaimsPrincipal))
        || type.IsAssignableTo(typeof(CancellationToken))
        || type.IsAssignableTo(typeof(IFormFile))
        || type.IsAssignableTo(typeof(IEnumerable<IFormFile>))
        || type.IsAssignableTo(typeof(Stream))
        || type.IsAssignableTo(typeof(PipeReader));

    /// <summary>
    /// A endpoint convention builder that configured parameter validation.
    /// </summary>
    public sealed class EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> : IEndpointConventionBuilder
        where TBuilder : IEndpointConventionBuilder
    {
        private readonly TBuilder _builder;

        internal EndpointParameterDataAnnotationsRouteHandlerBuilder(TBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        internal TBuilder InnerBuilder => _builder;

        /// <inheritdoc/>
        public void Add(Action<EndpointBuilder> convention)
        {
            _builder.Add(convention);
        }
    }
}
