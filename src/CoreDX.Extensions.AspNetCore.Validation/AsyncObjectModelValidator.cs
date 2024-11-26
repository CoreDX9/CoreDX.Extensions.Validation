using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Provides methods to validate an object graph.
/// </summary>
public interface IAsyncObjectModelValidator/* : IObjectModelValidator*/
{
    /// <summary>
    /// Validates the provided object.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <param name="validationState"> The <see cref="ValidationStateDictionary"/>. May be null.</param>
    /// <param name="prefix">The model prefix. Used to map the model object to entries in validationState.</param>
    /// <param name="model">The model object.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns></returns>
    Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object model,
        CancellationToken cancellationToken = default);
}

internal class AsyncObjectModelValidator : IAsyncObjectModelValidator
{
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly IModelValidatorProvider _modelValidatorProvider;
    private readonly ValidatorCache _validatorCache;
    private readonly MvcOptions _mvcOptions;

    public AsyncObjectModelValidator(
        IModelMetadataProvider modelMetadataProvider,
        IList<IModelValidatorProvider> validatorProviders,
        ValidatorCache validatorCache,
        MvcOptions mvcOptions
        )
    {
        _modelValidatorProvider = new CompositeModelValidatorProvider(validatorProviders);
        _modelMetadataProvider = modelMetadataProvider;
        _validatorCache = validatorCache;
        _mvcOptions = mvcOptions;
    }

    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object model,
        CancellationToken cancellationToken = default
        )
    {
        var visitor = GetValidationVisitor(
            actionContext,
            _modelValidatorProvider,
            _validatorCache,
            _modelMetadataProvider,
            validationState);

        var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
        return visitor.ValidateAsync(
            metadata: metadata,
            key: prefix,
            model: model,
            alwaysValidateAtTopLevel: false,
            cancellationToken: cancellationToken);
    }

    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object model,
        ModelMetadata metadata,
        CancellationToken cancellationToken = default
    ) =>
        ValidateAsync(actionContext, validationState, prefix, model, metadata, container: null, cancellationToken);

    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object model,
        ModelMetadata metadata,
        object? container,
        CancellationToken cancellationToken = default
    )
    {
        var visitor = GetValidationVisitor(
            actionContext,
            _modelValidatorProvider,
            _validatorCache,
            _modelMetadataProvider,
            validationState);

        return visitor.ValidateAsync(metadata, prefix, model, alwaysValidateAtTopLevel: metadata.IsRequired, container, cancellationToken);
    }

    public virtual AsyncValidationVisitor GetValidationVisitor(
        ActionContext actionContext,
        IModelValidatorProvider validatorProvider,
        ValidatorCache validatorCache,
        IModelMetadataProvider metadataProvider,
        ValidationStateDictionary? validationState)
    {
        return new AsyncValidationVisitor(
            actionContext,
            validatorProvider,
            validatorCache,
            metadataProvider,
            validationState)
        {
            MaxValidationDepth = _mvcOptions.MaxValidationDepth,
            ValidateComplexTypesIfChildValidationFails = _mvcOptions.ValidateComplexTypesIfChildValidationFails,
        };
    }
}
