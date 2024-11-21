using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// A default <see cref="IModelValidatorProvider"/>.
/// </summary>
/// <remarks>
/// The <see cref="DefaultAsyncModelValidatorProvider"/> provides validators from <see cref="IModelValidator"/>
/// </remarks>
internal sealed class DefaultAsyncModelValidatorProvider : IMetadataBasedModelValidatorProvider
{
    /// <inheritdoc />
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        //Perf: Avoid allocations here
        for (var i = 0; i < context.Results.Count; i++)
        {
            var validatorItem = context.Results[i];

            // Don't overwrite anything that was done by a previous provider.
            if (validatorItem.Validator != null)
            {
                continue;
            }

            if (validatorItem.ValidatorMetadata is IAsyncModelValidator validator)
            {
                validatorItem.Validator = validator;
                validatorItem.IsReusable = true;
            }
        }
    }

    public bool HasValidators(Type modelType, IList<object> validatorMetadata)
    {
        for (var i = 0; i < validatorMetadata.Count; i++)
        {
            if (validatorMetadata[i] is IAsyncModelValidator)
            {
                return true;
            }
        }

        return false;
    }
}