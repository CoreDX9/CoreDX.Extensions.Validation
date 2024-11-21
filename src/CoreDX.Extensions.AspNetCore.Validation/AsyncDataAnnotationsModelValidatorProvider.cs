using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// An implementation of <see cref="IModelValidatorProvider"/> which provides validators
/// for attributes which derive from <see cref="AsyncValidationAttribute"/>.
/// </summary>
public sealed class AsyncDataAnnotationsModelValidatorProvider : IMetadataBasedModelValidatorProvider
{
    private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _options;
    private readonly IStringLocalizerFactory? _stringLocalizerFactory;
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;

    /// <summary>
    /// Create a new instance of <see cref="AsyncDataAnnotationsModelValidatorProvider"/>.
    /// </summary>
    /// <param name="validationAttributeAdapterProvider">The <see cref="IValidationAttributeAdapterProvider"/>
    /// that supplies <see cref="IAttributeAdapter"/>s.</param>
    /// <param name="options">The <see cref="IOptions{MvcDataAnnotationsLocalizationOptions}"/>.</param>
    /// <param name="stringLocalizerFactory">The <see cref="IStringLocalizerFactory"/>.</param>
    /// <remarks><paramref name="options"/> and <paramref name="stringLocalizerFactory"/>
    /// are nullable only for testing ease.</remarks>
    public AsyncDataAnnotationsModelValidatorProvider(
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        IOptions<MvcDataAnnotationsLocalizationOptions> options,
        IStringLocalizerFactory? stringLocalizerFactory)
    {
        ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);
        ArgumentNullException.ThrowIfNull(options);

        _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
        _options = options;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        IStringLocalizer? stringLocalizer = null;
        if (_stringLocalizerFactory != null && _options.Value.DataAnnotationLocalizerProvider != null)
        {
            stringLocalizer = _options.Value.DataAnnotationLocalizerProvider(
                context.ModelMetadata.ContainerType ?? context.ModelMetadata.ModelType,
                _stringLocalizerFactory);
        }

        var results = context.Results;
        // Read interface .Count once rather than per iteration
        var resultsCount = results.Count;
        for (var i = 0; i < resultsCount; i++)
        {
            var validatorItem = results[i];
            if (validatorItem.Validator != null)
            {
                continue;
            }

            if (validatorItem.ValidatorMetadata is not AsyncValidationAttribute attribute)
            {
                continue;
            }

            var validator = new AsyncDataAnnotationsModelValidator(
                _validationAttributeAdapterProvider,
                attribute,
                stringLocalizer);

            validatorItem.Validator = validator;
            validatorItem.IsReusable = true;

            // NEVER TRUE
            //// Inserts validators based on whether or not they are 'required'. We want to run
            //// 'required' validators first so that we get the best possible error message.
            //if (attribute is RequiredAttribute)
            //{
            //    context.Results.Remove(validatorItem);
            //    context.Results.Insert(0, validatorItem);
            //}
        }

        //// Produce a validator if the type supports IValidatableObject
        //if (typeof(IValidatableObject).IsAssignableFrom(context.ModelMetadata.ModelType))
        //{
        //    context.Results.Add(new ValidatorItem
        //    {
        //        Validator = new ValidatableObjectAdapter(),
        //        IsReusable = true
        //    });
        //}
    }

    /// <inheritdoc/>
    public bool HasValidators(Type modelType, IList<object> validatorMetadata)
    {
        if (typeof(IValidatableObject).IsAssignableFrom(modelType))
        {
            return true;
        }

        // Read interface .Count once rather than per iteration
        var validatorMetadataCount = validatorMetadata.Count;
        for (var i = 0; i < validatorMetadataCount; i++)
        {
            if (validatorMetadata[i] is AsyncValidationAttribute)
            {
                return true;
            }
        }

        return false;
    }
}