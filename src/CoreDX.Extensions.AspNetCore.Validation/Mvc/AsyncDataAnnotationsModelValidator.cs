using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;

/// <inheritdoc/>
public interface IAsyncModelValidator : IModelValidator
{
    /// <summary>
    /// Validates the model value.
    /// </summary>
    /// <param name="context">The <see cref="ModelValidationContext"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="ModelValidationResult"/> indicating the results of validating the model value.</returns>
    Task<IEnumerable<ModelValidationResult>> ValidateAsync(ModelValidationContext context, CancellationToken cancellationToken = default);
}

internal sealed class AsyncDataAnnotationsModelValidator : IAsyncModelValidator
{
    private static readonly object _emptyValidationContextInstance = new object();
    private readonly IStringLocalizer? _stringLocalizer;
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;

    /// <summary>
    ///  Create a new instance of <see cref="AsyncDataAnnotationsModelValidator"/>.
    /// </summary>
    /// <param name="attribute">The <see cref="ValidationAttribute"/> that defines what we're validating.</param>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/> used to create messages.</param>
    /// <param name="validationAttributeAdapterProvider">The <see cref="IValidationAttributeAdapterProvider"/>
    /// which <see cref="ValidationAttributeAdapter{TAttribute}"/>'s will be created from.</param>
    public AsyncDataAnnotationsModelValidator(
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        AsyncValidationAttribute attribute,
        IStringLocalizer? stringLocalizer)
    {
        ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);
        ArgumentNullException.ThrowIfNull(attribute);

        _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
        Attribute = attribute;
        _stringLocalizer = stringLocalizer;
    }

    /// <summary>
    /// The attribute being validated against.
    /// </summary>
    public AsyncValidationAttribute Attribute { get; }

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext)
    {
        return Enumerable.Empty<ModelValidationResult>();
    }

    /// <summary>
    /// Validates the context against the <see cref="AsyncValidationAttribute"/>.
    /// </summary>
    /// <param name="validationContext">The context being validated.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An enumerable of the validation results.</returns>
    public async Task<IEnumerable<ModelValidationResult>> ValidateAsync(ModelValidationContext validationContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (validationContext.ModelMetadata == null)
        {
            throw new ArgumentException($"{nameof(validationContext.ModelMetadata)} can not be null.", nameof(validationContext));
        }
        if (validationContext.MetadataProvider == null)
        {
            throw new ArgumentException($"{nameof(validationContext.MetadataProvider)} can not be null.", nameof(validationContext));
        }

        var metadata = validationContext.ModelMetadata;
        var memberName = metadata.Name;
        var container = validationContext.Container;

        var context = new ValidationContext(
            instance: container ?? validationContext.Model ?? _emptyValidationContextInstance,
            serviceProvider: validationContext.ActionContext?.HttpContext?.RequestServices,
            items: null)
        {
            DisplayName = metadata.GetDisplayName(),
            MemberName = memberName
        };

        var result = await Attribute.GetValidationResultAsync(validationContext.Model, context, cancellationToken);
        if (result is not null)
        {
            string? errorMessage;
            if (_stringLocalizer != null &&
                !string.IsNullOrEmpty(Attribute.ErrorMessage) &&
                string.IsNullOrEmpty(Attribute.ErrorMessageResourceName) &&
                Attribute.ErrorMessageResourceType == null)
            {
                errorMessage = GetErrorMessage(validationContext) ?? result.ErrorMessage;
            }
            else
            {
                errorMessage = result.ErrorMessage;
            }

            var validationResults = new List<ModelValidationResult>();
            if (result.MemberNames != null)
            {
                foreach (var resultMemberName in result.MemberNames)
                {
                    // ModelValidationResult.MemberName is used by invoking validators (such as ModelValidator) to
                    // append construct the ModelKey for ModelStateDictionary. When validating at type level we
                    // want the returned MemberNames if specified (e.g. "person.Address.FirstName"). For property
                    // validation, the ModelKey can be constructed using the ModelMetadata and we should ignore
                    // MemberName (we don't want "person.Name.Name"). However the invoking validator does not have
                    // a way to distinguish between these two cases. Consequently we'll only set MemberName if this
                    // validation returns a MemberName that is different from the property being validated.
                    var newMemberName = string.Equals(resultMemberName, memberName, StringComparison.Ordinal) ?
                        null :
                        resultMemberName;
                    var validationResult = new ModelValidationResult(newMemberName, errorMessage);

                    validationResults.Add(validationResult);
                }
            }

            if (validationResults.Count == 0)
            {
                // result.MemberNames was null or empty.
                validationResults.Add(new ModelValidationResult(memberName: null, message: errorMessage));
            }

            return validationResults;
        }

        return Enumerable.Empty<ModelValidationResult>();
    }

    private string? GetErrorMessage(ModelValidationContextBase validationContext)
    {
        var adapter = _validationAttributeAdapterProvider.GetAttributeAdapter(Attribute, _stringLocalizer);
        return adapter?.GetErrorMessage(validationContext);
    }
}
