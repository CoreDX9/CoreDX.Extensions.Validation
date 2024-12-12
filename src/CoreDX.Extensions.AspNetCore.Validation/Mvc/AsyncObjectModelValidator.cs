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
        object? model,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The default implementation of <see cref="IAsyncObjectModelValidator"/>.
/// </summary>
internal class DefaultAsyncObjecValidator : AsyncObjectModelValidator
{
    private readonly MvcOptions _mvcOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
    /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
    public DefaultAsyncObjecValidator(
        IModelMetadataProvider modelMetadataProvider,
        IList<IModelValidatorProvider> validatorProviders,
        MvcOptions mvcOptions)
        : base(modelMetadataProvider, validatorProviders)
    {
        _mvcOptions = mvcOptions;
    }

    public override AsyncValidationVisitor GetValidationVisitor(
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

/// <summary>
/// Provides a base <see cref="IAsyncObjectModelValidator"/> implementation for validating an object graph.
/// </summary>
public abstract class AsyncObjectModelValidator : IAsyncObjectModelValidator
{
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly ValidatorCache _validatorCache;
    private readonly CompositeModelValidatorProvider _validatorProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncObjectModelValidator"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
    public AsyncObjectModelValidator(
        IModelMetadataProvider modelMetadataProvider,
        IList<IModelValidatorProvider> validatorProviders)
    {
        ArgumentNullException.ThrowIfNull(modelMetadataProvider);
        ArgumentNullException.ThrowIfNull(validatorProviders);

        _modelMetadataProvider = modelMetadataProvider;
        _validatorCache = new ValidatorCache();

        _validatorProvider = new CompositeModelValidatorProvider(validatorProviders);
    }

    /// <inheritdoc />
    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object? model,
        CancellationToken cancellationToken = default)
    {
        var visitor = GetValidationVisitor(
            actionContext,
            _validatorProvider,
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

    /// <summary>
    /// Validates the provided object model.
    /// If <paramref name="model"/> is <see langword="null"/> and the <paramref name="metadata"/>'s
    /// <see cref="ModelMetadata.IsRequired"/> is <see langword="true"/>, will add one or more
    /// model state errors that <see cref="ValidateAsync(ActionContext, ValidationStateDictionary, string, object, CancellationToken)"/>
    /// would not.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
    /// <param name="prefix">The model prefix key.</param>
    /// <param name="model">The model object.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object model,
        ModelMetadata metadata,
        CancellationToken cancellationToken = default)
        => ValidateAsync(actionContext, validationState, prefix, model, metadata, container: null, cancellationToken);

    /// <summary>
    /// Validates the provided object model.
    /// If <paramref name="model"/> is <see langword="null"/> and the <paramref name="metadata"/>'s
    /// <see cref="ModelMetadata.IsRequired"/> is <see langword="true"/>, will add one or more
    /// model state errors that <see cref="ValidateAsync(ActionContext, ValidationStateDictionary, string, object, CancellationToken)"/>
    /// would not.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
    /// <param name="prefix">The model prefix key.</param>
    /// <param name="model">The model object.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/>.</param>
    /// <param name="container">The model container</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    public virtual Task ValidateAsync(
        ActionContext actionContext,
        ValidationStateDictionary? validationState,
        string prefix,
        object? model,
        ModelMetadata metadata,
        object? container,
        CancellationToken cancellationToken = default
    )
    {
        var visitor = GetValidationVisitor(
            actionContext,
            _validatorProvider,
            _validatorCache,
            _modelMetadataProvider,
            validationState);

        return visitor.ValidateAsync(metadata, prefix, model, alwaysValidateAtTopLevel: metadata.IsRequired, container, cancellationToken);
    }

    /// <summary>
    /// Gets a <see cref="ValidationVisitor"/> that traverses the object model graph and performs validation.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/>.</param>
    /// <param name="validatorCache">The <see cref="ValidatorCache"/>.</param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
    /// <returns>A <see cref="ValidationVisitor"/> which traverses the object model graph.</returns>
    public abstract AsyncValidationVisitor GetValidationVisitor(
        ActionContext actionContext,
        IModelValidatorProvider validatorProvider,
        ValidatorCache validatorCache,
        IModelMetadataProvider metadataProvider,
        ValidationStateDictionary? validationState);
}
