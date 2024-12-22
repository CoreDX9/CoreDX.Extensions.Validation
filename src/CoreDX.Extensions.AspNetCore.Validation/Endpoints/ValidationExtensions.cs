using CoreDX.Extensions.AspNetCore.Http.Validation;
using CoreDX.Extensions.AspNetCore.Http.Validation.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using static CoreDX.Extensions.AspNetCore.Http.Validation.EndpointBindingParametersValidationMetadata;
using static CoreDX.Extensions.AspNetCore.Http.Validation.EndpointBindingParametersValidationMetadata.ParameterValidationMetadata;

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
    /// <param name="throwOnValidationException">Should an exception be thrown when an exception occurs during validation.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> AddEndpointParameterDataAnnotations<TBuilder>(
        this TBuilder endpointConvention,
        bool throwOnValidationException = false)
        where TBuilder : IEndpointConventionBuilder
    {
        endpointConvention.Add(endpointBuilder =>
        {
            var loggerFactory = endpointBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(_filterLoggerName);

            if (endpointBuilder.Metadata.Any(static md => md is ActionDescriptor))
            {
                logger.LogDebug("Cannot add parameter data annotations validation filter to MVC controller or Razor pages endpoint {actionName}.", endpointBuilder.DisplayName);
                return;
            }

            if (endpointBuilder.Metadata.Any(static md => md is EndpointBindingParametersValidationMetadata))
            {
                logger.LogDebug("Already has a parameter data annotations validation filter on endpoint {actionName}.", endpointBuilder.DisplayName);
                return;
            }

            if (endpointBuilder.Metadata.Any(static md => md is EndpointBindingParametersValidationMetadataMark))
            {
                logger.LogDebug("Already called method AddEndpointParameterDataAnnotations before on endpoint {actionName}.", endpointBuilder.DisplayName);
                return;
            }

            endpointBuilder.Metadata.Add(new EndpointBindingParametersValidationMetadataMark(throwOnValidationException));

            endpointBuilder.FilterFactories.Add((filterFactoryContext, next) =>
            {
                var loggerFactory = filterFactoryContext.ApplicationServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(_filterLoggerName);

                var parameters = filterFactoryContext.MethodInfo.GetParameters();

                var isServicePredicate = filterFactoryContext.ApplicationServices.GetService<IServiceProviderIsService>();
                List<int> bindingParameterIndexs = new(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo? parameter = parameters[i];
                    if (IsRequestDelegateFactorySpecialBoundType(parameter.ParameterType)) continue;
                    if (parameter.GetCustomAttribute<FromServicesAttribute>() is not null) continue;
#if NET8_0_OR_GREATER
                    if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is not null) continue;
#endif
                    if (isServicePredicate?.IsService(parameter.ParameterType) is true) continue;

                    bindingParameterIndexs.Add(i);
                }

                if (bindingParameterIndexs.Count is 0)
                {
                    logger.LogDebug("Route handler method '{methodName}' does not contain any validatable parameters, skipping adding validation filter.", filterFactoryContext.MethodInfo.Name);
                }

                EndpointBindingParametersValidationMetadata? validationParameterMetadata = null;
                try
                {
                    List<ParameterValidationMetadata> validatableBindingParameters = new(bindingParameterIndexs.Count);
                    foreach (var parameterIndex in bindingParameterIndexs)
                    {
                        var parameter = parameters[parameterIndex];
                        if (!HasValidatableTarget(parameter)) continue;

                        validatableBindingParameters.Add(new(parameter));
                    }

                    if (validatableBindingParameters.Count > 0)
                    {
                        validationParameterMetadata = new(filterFactoryContext.MethodInfo, validatableBindingParameters);
                    }
                }
                catch (Exception e)
                {
                    validationParameterMetadata = null;
                    logger.LogError(e, "Build parameter validation metadate failed for route handler method '{methodName}', skipping adding validation filter.", filterFactoryContext.MethodInfo.Name);
                }

                if (validationParameterMetadata?.Any() is not true) return invocationContext => next(invocationContext);

                endpointBuilder.Metadata.Add(validationParameterMetadata);

                var problemResultMark = endpointBuilder.Metadata
                    .FirstOrDefault(static md => md is EndpointParameterDataAnnotationsValidationProblemResultMark)
                    as EndpointParameterDataAnnotationsValidationProblemResultMark;
                if (problemResultMark is not null)
                {
                    if (!endpointBuilder.Metadata.Any(md =>
                        md is IProducesResponseTypeMetadata pr
                        && (pr.Type?.IsAssignableTo(typeof(HttpValidationProblemDetails))) is true
                        && pr.StatusCode == problemResultMark.StatusCode
                        && pr.ContentTypes.Contains("application/problem+json"))
                    )
                    {
                        endpointBuilder.Metadata.Add(
                            new ProducesResponseTypeMetadata(
                                problemResultMark.StatusCode,
                                typeof(HttpValidationProblemDetails),
                                ["application/problem+json"]
                            )
                        );
                    }
                }

                return async invocationContext =>
                {
                    var endpoint = invocationContext.HttpContext.GetEndpoint();
                    var validationMetadata = endpoint?.Metadata.GetMetadata<EndpointBindingParametersValidationMetadata>();

                    if (validationMetadata is null) return await next(invocationContext);

                    Dictionary<string, object?> arguments = new(validationMetadata.Count);
                    foreach (var parameter in validationMetadata)
                    {
                        arguments.Add(parameter.Value.ParameterName!, invocationContext.Arguments[parameter.Value.ParameterIndex]);
                    }

                    try
                    {
                        var results = await validationMetadata.ValidateAsync(arguments);
                        if (results != null) invocationContext.HttpContext.Items.Add(_validationResultItemName, results);
                    }
                    catch (Exception e)
                    {
                        var loggerFactory = invocationContext.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger(_filterLoggerName);
                        logger.LogError(e, "Validate parameter failed for route handler method '{methodName}'.", validationMetadata.EndpointMethod.Name);

                        var validationMetadataMark = endpoint?.Metadata.GetMetadata<EndpointBindingParametersValidationMetadataMark>();

                        if (validationMetadataMark?.ThrowOnValidationException is true) throw;
                    }

                    var problemResultMark = endpoint?.Metadata.GetMetadata<EndpointParameterDataAnnotationsValidationProblemResultMark>();
                    if (problemResultMark is not null)
                    {
                        var errors = invocationContext.HttpContext.GetEndpointParameterDataAnnotationsProblemDetails();

                        if (errors is { Count: > 0 }) return Results.ValidationProblem(errors, statusCode: problemResultMark.StatusCode);
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
    /// <param name="validationEndpointBuilder">The endpoint convention builder.</param>
    /// <param name="statusCode">The http status code.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddValidationProblemResult<TBuilder>(
        this EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> validationEndpointBuilder,
        int statusCode = StatusCodes.Status400BadRequest)
        where TBuilder : IEndpointConventionBuilder
    {
        validationEndpointBuilder.InnerBuilder.Add(endpointBuilder =>
        {
            var loggerFactory = endpointBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(_filterLoggerName);

            if (endpointBuilder.Metadata.Any(static md => md is EndpointParameterDataAnnotationsValidationProblemResultMark))
            {
                logger.LogDebug("Already has a parameter data annotations validation problem result filter on endpoint {actionName}.", endpointBuilder.DisplayName);
                return;
            }

            endpointBuilder.Metadata.Add(new EndpointParameterDataAnnotationsValidationProblemResultMark(statusCode));
        });

        return validationEndpointBuilder.InnerBuilder;
    }

    /// <summary>
    /// Restore <see cref="EndpointParameterDataAnnotationsRouteHandlerBuilder{TBuilder}"/> to original builder of type <typeparamref name="TBuilder"/>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of original builder.</typeparam>
    /// <param name="validationEndpointBuilder">The <see cref="EndpointParameterDataAnnotationsRouteHandlerBuilder{TBuilder}"/> to restore.</param>
    /// <returns>The original builder.</returns>
    public static TBuilder RestoreToOriginalBuilder<TBuilder>(this EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder> validationEndpointBuilder)
        where TBuilder : IEndpointConventionBuilder
    {
        return validationEndpointBuilder.InnerBuilder;
    }

    /// <summary>
    /// Validate parameters using given values.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <param name="arguments">Key value pair collection of parameter name and value.</param>
    /// <returns>
    /// Return <see langword="true"/> if endpoint has validatable parameter and <paramref name="arguments"/> has correct parameter name and type.
    /// Otherwise, return <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<bool> TryValidateEndpointParametersAsync(
        this HttpContext httpContext,
        params IEnumerable<KeyValuePair<string, object?>> arguments)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var metadata = httpContext.GetEndpointBindingParametersValidationMetadata();
        if (metadata is null) return false;

        if (!arguments.Any()) throw new ArgumentException("There are no elements in the sequence.", nameof(arguments));

        HashSet<string> names = [];
        foreach (var name in arguments.Select(static arg => arg.Key))
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Argument's name cannot be null or empty.", nameof(arguments));
            if (!names.Add(name)) throw new ArgumentException("Argument's name must be unique.", nameof(arguments));
        }

        var currentResults = httpContext.GetEndpointParameterDataAnnotationsValidationResultsCore();
        var newResults = await metadata.ValidateAsync(arguments.ToDictionary(static arg => arg.Key, static arg => arg.Value));

        if (newResults is null) // 本次验证结果没有任何错误
        {
            if (currentResults != null)
            {
                // 移除本次验证结果中没有验证错误数据的参数项
                foreach (var argument in arguments) currentResults.Remove(argument.Key);
                // 如果移除后变成空集，直接清除结果集
                if (currentResults.Count is 0) httpContext.Items.Remove(_validationResultItemName);
            }
        }
        else
        {
            if (currentResults != null)
            {
                // 如果上次的验证结果中有同名参数的数据，但本次验证结果中没有，移除该参数的过时的旧结果数据
                foreach (var argument in arguments)
                {
                    if (!newResults.Keys.Any(key => key == argument.Key)) currentResults.Remove(argument.Key);
                }
            }
            else
            {
                // 上次验证结果显示没有任何错误，新建错误结果集
                httpContext.Items.Remove(_validationResultItemName);

                currentResults = [];
                httpContext.Items.Add(_validationResultItemName, currentResults);
            }
            // 添加上次验证中没有错误数据的参数项，或者更新同名参数项的验证错误数据
            foreach (var newResult in newResults) currentResults[newResult.Key] = newResult.Value;
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
                static r => r.Key,
                static r => new ArgumentPropertiesValidationResults(r.Value.ToDictionary(
                    static fr => fr.Key.ToString()!,
                    static fr => fr.Value.ToImmutableList())
                )
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
        if (validationResult?.Any(static vrp => vrp.Value.Any()) is true)
        {
            var localizerFactory = httpContext.RequestServices.GetService<IStringLocalizerFactory>();

            EndpointParameterValidationLocalizationOptions? localizationOptions = null;
            AttributeLocalizationAdapters? adapters = null;
            if (localizerFactory != null)
            {
                localizationOptions = httpContext.RequestServices
                    .GetService<IOptions<EndpointParameterValidationLocalizationOptions>>()
                    ?.Value;

                adapters = localizationOptions?.Adapters;
            }

            var metadatas = httpContext.GetEndpointBindingParametersValidationMetadata();
            Debug.Assert(metadatas != null);
            var endpointHandlerType = metadatas.EndpointMethod.ReflectedType;
            Debug.Assert(endpointHandlerType != null);

            var errors = validationResult.SelectMany(static vrp => vrp.Value);
            result = localizerFactory is null || !(adapters?.Count > 0)
                ? errors
                    .ToDictionary(
                        static fvr => fvr.Key.ToString()!,
                        static fvr => fvr.Value.Select(ToErrorMessage).ToArray()
                    )
                : errors
                    .ToDictionary(
                        static fvr => fvr.Key.ToString()!,
                        fvr => fvr.Value
                            .Select(vr =>
                                ToLocalizedErrorMessage(
                                    vr,
                                    fvr.Key.ModelIsTopLevelFakeObject
                                        ? new KeyValuePair<Type, ParameterValidationMetadata>(
                                            endpointHandlerType,
                                            (metadatas?.TryGetValue(fvr.Key.FieldName!, out var metadata)) is true
                                                ? metadata
                                                : null! /* never null */)
                                        : null,
                                    adapters,
                                    localizerFactory
                                )
                            )
                            .ToArray()
                    );
        }

        return result;

        static string ToErrorMessage(ValidationResult result)
        {
            return result.ErrorMessage!;
        }

        string ToLocalizedErrorMessage(
            ValidationResult result,
            KeyValuePair<Type, ParameterValidationMetadata>? parameterMetadata,
            AttributeLocalizationAdapters adapters,
            IStringLocalizerFactory localizerFactory)
        {
            if (result is LocalizableValidationResult localizable)
            {
                var localizer = localizerFactory.Create(localizable.InstanceObjectType);

                string displayName;
                if (!string.IsNullOrEmpty(parameterMetadata?.Value.DisplayName))
                {
                    var parameterLocalizer = localizerFactory.Create(parameterMetadata.Value.Key);
                    displayName = parameterLocalizer[parameterMetadata.Value.Value.DisplayName];
                }
                else displayName = GetDisplayName(localizable, localizer);

                var adapter = adapters.FirstOrDefault(ap => localizable.Attribute.GetType().IsAssignableTo(ap.CanProcessAttributeType));
                if (adapter != null
                    && !string.IsNullOrEmpty(localizable.Attribute.ErrorMessage)
                    && string.IsNullOrEmpty(localizable.Attribute.ErrorMessageResourceName)
                    && localizable.Attribute.ErrorMessageResourceType == null)
                {
                    return localizer
                    [
                        localizable.Attribute.ErrorMessage,
                        [displayName, .. adapter.GetLocalizationArguments(localizable.Attribute) ?? []]
                    ];
                }

                return localizable.Attribute.FormatErrorMessage(displayName);
            }

            return result.ErrorMessage!;

            static string GetDisplayName(LocalizableValidationResult localizable, IStringLocalizer localizer)
            {
                string? displayName = null;
                ValidationAttributeStore store = ValidationAttributeStore.Instance;
                DisplayAttribute? displayAttribute = null;
                DisplayNameAttribute? displayNameAttribute = null;

                if (string.IsNullOrEmpty(localizable.MemberName))
                {
                    displayAttribute = store.GetTypeDisplayAttribute(localizable.Context);
                    displayNameAttribute = store.GetTypeDisplayNameAttribute(localizable.Context);
                }
                else if (store.IsPropertyContext(localizable.Context))
                {
                    displayAttribute = store.GetPropertyDisplayAttribute(localizable.Context);
                    displayNameAttribute = store.GetPropertyDisplayNameAttribute(localizable.Context);
                }

                if (displayAttribute != null)
                {
                    displayName = displayAttribute.GetName();
                }
                else if (displayNameAttribute != null)
                {
                    displayName = displayNameAttribute.DisplayName;
                }

                return string.IsNullOrEmpty(displayName)
                    ? localizable.DisplayName
                    : localizer[displayName];
            }
        }
    }

    internal static Dictionary<string, ValidationResultStore>? GetEndpointParameterDataAnnotationsValidationResultsCore(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Items.TryGetValue(_validationResultItemName, out var result);
        return result as Dictionary<string, ValidationResultStore>;
    }

    internal static EndpointBindingParametersValidationMetadata? GetEndpointBindingParametersValidationMetadata(this HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        return endpoint?.Metadata
            .FirstOrDefault(static md => md is EndpointBindingParametersValidationMetadata) as EndpointBindingParametersValidationMetadata;
    }

    /// <summary>
    /// A endpoint convention builder that configured parameter validation.
    /// </summary>
    public sealed class EndpointParameterDataAnnotationsRouteHandlerBuilder<TBuilder>
        where TBuilder : IEndpointConventionBuilder
    {
        private readonly TBuilder _builder;

        internal EndpointParameterDataAnnotationsRouteHandlerBuilder(TBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        internal TBuilder InnerBuilder => _builder;
    }
}

/// <summary>
/// A class to mark endpoint parameter data annotations validation filer has been configured.
/// </summary>
public sealed class EndpointBindingParametersValidationMetadataMark
{
    /// <summary>
    /// Gets should an exception be thrown when an exception occurs during validation.
    /// </summary>
    public bool ThrowOnValidationException { get; }

    /// <summary>
    /// Initialize a new instance of the class <see cref="EndpointBindingParametersValidationMetadataMark"/>.
    /// </summary>
    /// <param name="throwOnValidationException">Should an exception be thrown when an exception occurs during validation.</param>
    public EndpointBindingParametersValidationMetadataMark(bool throwOnValidationException)
    {
        ThrowOnValidationException = throwOnValidationException;
    }
}

/// <summary>
/// A class to mark endpoint parameter data annotations validation problem result filer has been configured.
/// </summary>
public sealed class EndpointParameterDataAnnotationsValidationProblemResultMark
{
    /// <summary>
    /// Gets the status code of the validation problem result.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Initialize a new instance of the class <see cref="EndpointParameterDataAnnotationsValidationProblemResultMark"/>.
    /// </summary>
    /// <param name="statusCode">Status code of the validation problem result.</param>
    public EndpointParameterDataAnnotationsValidationProblemResultMark(int statusCode)
    {
        StatusCode = statusCode;
    }
}
