using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static CoreDX.Extensions.AspNetCore.Http.Validation.EndpointBindingParameterValidationMetadata;
using static Microsoft.AspNetCore.Http.EndpointParameterValidationExtensions;

namespace CoreDX.Extensions.AspNetCore.Http.Validation;

internal sealed class EndpointBindingParameterValidationMetadata : IEnumerable<ParameterValidationMetadata>
{
    private readonly HashSet<ParameterValidationMetadata> _metadatas = [];

    public EndpointBindingParameterValidationMetadata(params IEnumerable<ParameterValidationMetadata> metadatas)
    {
        HashSet<string> names = [];
        foreach (var metadata in metadatas)
        {
            if (!names.Add(metadata.ParameterName)) throw new ArgumentException("metadata's parameter name must be unique.", nameof(metadatas));

            _metadatas.Add(metadata);
        }
    }

    public async ValueTask<Dictionary<string, ValidationResultStore>> ValidateAsync(IDictionary<string, object?> arguments)
    {
        Dictionary<string, ValidationResultStore> result = [];
        foreach (var argument in arguments)
        {
            var metadata = _metadatas.FirstOrDefault(md => md.ParameterName == argument.Key);
            if (metadata is null) throw new InvalidOperationException($"Parameter named {argument.Key} does not exist.");
            result.TryAdd(metadata.ParameterName, await metadata.ValidateAsync(arguments));
        }
        return result;
    }

    public IEnumerator<ParameterValidationMetadata> GetEnumerator()
    {
        return _metadatas.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal sealed class ParameterValidationMetadata
    {
        private ParameterInfo _parameterInfo;
        private string? _displayName;
        private RequiredAttribute? _requiredAttribute;
        private ImmutableList<ValidationAttribute> _otherValidationAttributes;

        public ParameterValidationMetadata(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));

            if (string.IsNullOrEmpty(parameterInfo.Name)) throw new ArgumentException("Parameter must be have name.", nameof(parameterInfo));

            _displayName = parameterInfo.GetCustomAttribute<DisplayAttribute>()?.Name
                ?? parameterInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

            _requiredAttribute = parameterInfo.GetCustomAttribute<RequiredAttribute>();
            _otherValidationAttributes = parameterInfo
                .GetCustomAttributes<ValidationAttribute>()
                .Where(attr => attr is not RequiredAttribute)
                .ToImmutableList();
        }

        public string ParameterName => _parameterInfo.Name!;
        public string? DisplayName => _displayName;

        public async ValueTask<ValidationResultStore> ValidateAsync(object? argument, CancellationToken cancellationToken = default)
        {
            if (argument is not null && !argument.GetType().IsAssignableTo(_parameterInfo.ParameterType))
            {
                throw new InvalidCastException($"Object cannot assign to {ParameterName} of type {_parameterInfo.ParameterType}.");
            }

            var topName = ParameterName ?? $"<argumentSelf({argument?.GetType()?.Name})>";
            ValidationResultStore resultStore = new();
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
                        var result = (await asyncValidation.GetValidationResultAsync(argument, validationContext, cancellationToken))!;
                        result = new LocalizableValidationResult(result.ErrorMessage, result.MemberNames, validation, validationContext);
                        results.Add(result);
                    }
                    else
                    {
                        var result = validation.GetValidationResult(argument, validationContext)!;
                        result = new LocalizableValidationResult(result.ErrorMessage, result.MemberNames, validation, validationContext);
                        results.Add(result);
                    }
                }

                await ObjectGraphValidator.TryValidateObjectAsync(
                    argument,
                    new ValidationContext(argument),
                    resultStore,
                    true,
                    static type => !IsRequestDelegateFactorySpecialBoundType(type),
                    topName,
                    cancellationToken);
            }

            var id = FieldIdentifier.GetFakeTopLevelObjectIdentifier(topName);

            resultStore.Add(id, results);

            return resultStore;
        }
    }
}
