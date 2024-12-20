using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Reflection;
using System.Security.Claims;
using static CoreDX.Extensions.AspNetCore.Http.Validation.EndpointBindingParametersValidationMetadata;

namespace CoreDX.Extensions.AspNetCore.Http.Validation;

internal sealed class EndpointBindingParametersValidationMetadata : IReadOnlyDictionary<string, ParameterValidationMetadata>
{
    private readonly MethodInfo _endpointMethod;
    private readonly IReadOnlyDictionary<string, ParameterValidationMetadata> _metadatas;

    public MethodInfo EndpointMethod => _endpointMethod;

    public EndpointBindingParametersValidationMetadata(MethodInfo endpointMethod, params IEnumerable<ParameterValidationMetadata> metadatas)
    {
        ArgumentNullException.ThrowIfNull(endpointMethod);

        if (!metadatas.Any()) throw new ArgumentException("Argument does not have any element.", nameof(metadatas));

        Dictionary<string, ParameterValidationMetadata> tempMetadatas = [];
        HashSet<string> names = [];
        foreach (var metadata in metadatas)
        {
            if (!names.Add(metadata.ParameterName)) throw new ArgumentException("metadata's parameter name must be unique.", nameof(metadatas));

            tempMetadatas.Add(metadata.ParameterName, metadata);
        }

        _metadatas = tempMetadatas.AsReadOnly();
        _endpointMethod = endpointMethod;
    }

    public async ValueTask<Dictionary<string, ValidationResultStore>?> ValidateAsync(IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        Dictionary<string, ValidationResultStore> result = [];
        foreach (var argument in arguments)
        {
            if (!_metadatas.TryGetValue(argument.Key, out var metadata))
            {
                throw new InvalidOperationException($"Parameter named {argument.Key} does not exist.");
            }

            var argumentResults = await metadata.ValidateAsync(argument.Value, cancellationToken);
            if (argumentResults is not null) result.TryAdd(metadata.ParameterName, argumentResults);
        }
        return result.Count > 0 ? result : null;
    }

    #region IReadOnlyDictionary<TKey, TValue> members

    public IEnumerable<string> Keys => _metadatas.Keys;

    public IEnumerable<ParameterValidationMetadata> Values => _metadatas.Values;

    public int Count => _metadatas.Count;

    public ParameterValidationMetadata this[string key] => _metadatas[key];

    public bool ContainsKey(string key) => _metadatas.ContainsKey(key);

    public bool TryGetValue(
        string key,
        [MaybeNullWhen(false)] out ParameterValidationMetadata value)
        => _metadatas.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, ParameterValidationMetadata>> GetEnumerator() => _metadatas.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    internal sealed class ParameterValidationMetadata
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly int _parameterIndex;
        private readonly string? _displayName;
        private readonly RequiredAttribute? _requiredAttribute;
        private readonly ImmutableList<ValidationAttribute> _otherValidationAttributes;

        public ParameterValidationMetadata(ParameterInfo parameterInfo, int parameterIndex)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));

            if (string.IsNullOrEmpty(parameterInfo.Name)) throw new ArgumentException("Parameter must be have name.", nameof(parameterInfo));

            if (!HasValidatableTarget(parameterInfo)) throw new ArgumentException("Parameter does not have any validatable target.", nameof(parameterInfo));

            _parameterIndex = parameterIndex;
            _displayName = parameterInfo.GetCustomAttribute<DisplayAttribute>()?.Name
                ?? parameterInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

            _requiredAttribute = parameterInfo.GetCustomAttribute<RequiredAttribute>();

            if (_requiredAttribute is null && !IsOptionalParameter(parameterInfo))
            {
                _requiredAttribute = new() { ErrorMessage = "The parameter {0} is required" };
            }

            _otherValidationAttributes = parameterInfo
                .GetCustomAttributes<ValidationAttribute>()
                .Where(attr => attr is not RequiredAttribute)
                .ToImmutableList();
        }

        public string ParameterName => _parameterInfo.Name!;

        public int ParameterIndex => _parameterIndex;

        public string? DisplayName => _displayName;

        public ParameterInfo Parameter => _parameterInfo;

        public async ValueTask<ValidationResultStore?> ValidateAsync(object? argument, CancellationToken cancellationToken = default)
        {
            if (argument is not null && !argument.GetType().IsAssignableTo(_parameterInfo.ParameterType))
            {
                throw new InvalidCastException($"Object cannot assign to {ParameterName} of type {_parameterInfo.ParameterType}.");
            }

            ValidationResultStore resultStore = [];
            List<ValidationResult> results = [];

            var validationContext = new ValidationContext(argument ?? new())
            {
                MemberName = ParameterName
            };

            if (DisplayName is not null) validationContext.DisplayName = DisplayName;

            if (argument is null && _requiredAttribute is not null)
            {
                var result = _requiredAttribute.GetValidationResult(argument, validationContext)!;
                result = new LocalizableValidationResult(result.ErrorMessage, result.MemberNames, _requiredAttribute, validationContext);
                results.Add(result);
            }

            if (argument is not null)
            {
                foreach (var validation in _otherValidationAttributes)
                {
                    if (validation is AsyncValidationAttribute asyncValidation)
                    {
                        var result = await asyncValidation.GetValidationResultAsync(argument, validationContext, cancellationToken);
                        if (result != ValidationResult.Success)
                        {
                            result = new LocalizableValidationResult(result!.ErrorMessage, result.MemberNames, validation, validationContext);
                            results.Add(result);
                        }
                    }
                    else
                    {
                        var result = validation.GetValidationResult(argument, validationContext);
                        if (result != ValidationResult.Success)
                        {
                            result = new LocalizableValidationResult(result!.ErrorMessage, result.MemberNames, validation, validationContext);
                            results.Add(result);
                        }
                    }
                }

                await ObjectGraphValidator.TryValidateObjectAsync(
                    argument,
                    new ValidationContext(argument),
                    resultStore,
                    true,
                    static type => !IsRequestDelegateFactorySpecialBoundType(type),
                    ParameterName,
                    cancellationToken
                );
            }

            if (results.Count > 0)
            {
                var id = FieldIdentifier.GetFakeTopLevelObjectIdentifier(ParameterName);
                resultStore.Add(id, results);
            }

            return resultStore.Any() ? resultStore : null;
        }

        public static bool HasValidatableTarget(ParameterInfo parameter)
        {
            if (parameter.GetCustomAttributes<ValidationAttribute>().Any()) return true;

            if (!IsOptionalParameter(parameter)) return true;

            return ObjectGraphValidator.HasValidatableTarget(
                parameter.ParameterType,
                false,
                static type => !IsRequestDelegateFactorySpecialBoundType(type)
            );
        }

        private static bool IsOptionalParameter(ParameterInfo parameter)
        {
            var nullableInfo = new NullabilityInfoContext().Create(parameter);
            var isNullable = parameter.HasDefaultValue || nullableInfo.ReadState != NullabilityState.NotNull;

            return isNullable;
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
    }
}
