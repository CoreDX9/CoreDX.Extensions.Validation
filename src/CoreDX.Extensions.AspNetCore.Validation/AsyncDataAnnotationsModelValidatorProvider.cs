using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
#if NET6_0
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;
#if NET6_0
using System.Runtime.Versioning;
#endif

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

            if (validatorItem.ValidatorMetadata is not AsyncValidationAttribute asyncAttribute)
            {
                continue;
            }

            var validator = new AsyncDataAnnotationsModelValidator(
                _validationAttributeAdapterProvider,
                asyncAttribute,
                stringLocalizer);

            validatorItem.Validator = validator;
            validatorItem.IsReusable = true;

            // NEVER TRUE
            //// Inserts validators based on whether or not they are 'required'. We want to run
            //// 'required' validators first so that we get the best possible error message.
            //if (asyncAttribute is RequiredAttribute)
            //{
            //    context.Results.Remove(validatorItem);
            //    context.Results.Insert(0, validatorItem);
            //}
        }

        // Produce a validator if the type supports IAsyncValidatableObject
        if (typeof(IAsyncValidatableObject).IsAssignableFrom(context.ModelMetadata.ModelType))
        {
            context.Results.Add(new ValidatorItem
            {
                Validator = new AsyncValidatableObjectAdapter(),
                IsReusable = true
            });
        }
    }

    /// <inheritdoc/>
    public bool HasValidators(Type modelType, IList<object> validatorMetadata)
    {
        if (typeof(IAsyncValidatableObject).IsAssignableFrom(modelType))
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

internal class ValidatableObjectAdapter : IModelValidator
{
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var model = context.Model;
        if (model == null)
        {
            return Enumerable.Empty<ModelValidationResult>();
        }

        if (!(model is IValidatableObject validatable))
        {
            //var message = Resources.FormatValidatableObjectAdapter_IncompatibleType(
            //    typeof(IValidatableObject).Name,
            //    model.GetType());

            throw new InvalidOperationException(/*message*/);
        }

        // The constructed ValidationContext is intentionally slightly different from what
        // DataAnnotationsModelValidator creates. The instance parameter would be context.Container
        // (if non-null) in that class. But, DataAnnotationsModelValidator _also_ passes context.Model
        // separately to any ValidationAttribute.
        var validationContext = new ValidationContext(
            instance: validatable,
            serviceProvider: context.ActionContext?.HttpContext?.RequestServices,
            items: null)
        {
            DisplayName = context.ModelMetadata.GetDisplayName(),
            MemberName = context.ModelMetadata.Name,
        };

        return ConvertResults(validatable.Validate(validationContext));
    }

    private static IEnumerable<ModelValidationResult> ConvertResults(IEnumerable<ValidationResult> results)
    {
        foreach (var result in results)
        {
            if (result != ValidationResult.Success)
            {
                if (result.MemberNames == null || !result.MemberNames.Any())
                {
                    yield return new ModelValidationResult(memberName: null, message: result.ErrorMessage);
                }
                else
                {
                    foreach (var memberName in result.MemberNames)
                    {
                        yield return new ModelValidationResult(memberName, result.ErrorMessage);
                    }
                }
            }
        }
    }
}

internal sealed class AsyncValidatableObjectAdapter : ValidatableObjectAdapter, IAsyncModelValidator
{
    public Task<IEnumerable<ModelValidationResult>> ValidateAsync(ModelValidationContext context, CancellationToken cancellationToken = default)
    {
        var model = context.Model;
        if (model == null)
        {
            return Task.FromResult(Enumerable.Empty<ModelValidationResult>());
        }

        if (!(model is IAsyncValidatableObject asyncValidatable))
        {
            //var message = Resources.FormatValidatableObjectAdapter_IncompatibleType(
            //    typeof(IAsyncValidatableObject).Name,
            //    model.GetType());

            throw new InvalidOperationException(/*message*/);
        }

        // The constructed ValidationContext is intentionally slightly different from what
        // DataAnnotationsModelValidator creates. The instance parameter would be context.Container
        // (if non-null) in that class. But, DataAnnotationsModelValidator _also_ passes context.Model
        // separately to any ValidationAttribute.
        var validationContext = new ValidationContext(
            instance: model,
            serviceProvider: context.ActionContext?.HttpContext?.RequestServices,
            items: null)
        {
            DisplayName = context.ModelMetadata.GetDisplayName(),
            MemberName = context.ModelMetadata.Name,
        };

        return Task.FromResult(ConvertResultsAsync(asyncValidatable.ValidateAsync(validationContext, cancellationToken), cancellationToken).ToBlockingEnumerable(cancellationToken));
    }

    private static async IAsyncEnumerable<ModelValidationResult> ConvertResultsAsync(IAsyncEnumerable<ValidationResult> results, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in results.WithCancellation(cancellationToken))
        {
            if (result != ValidationResult.Success)
            {
                if (result.MemberNames == null || !result.MemberNames.Any())
                {
                    yield return new ModelValidationResult(memberName: null, message: result.ErrorMessage);
                }
                else
                {
                    foreach (var memberName in result.MemberNames)
                    {
                        yield return new ModelValidationResult(memberName, result.ErrorMessage);
                    }
                }
            }
        }
    }
}

#if NET6_0
/// <summary>Provides a set of static methods for configuring <see cref="Task"/>-related behaviors on asynchronous enumerables and disposables.</summary>
internal static class TaskAsyncEnumerableExtensions
{
    /// <summary>
    /// Converts an <see cref="IAsyncEnumerable{T}"/> instance into an <see cref="IEnumerable{T}"/> that enumerates elements in a blocking manner.
    /// </summary>
    /// <typeparam name="T">The type of the objects being iterated.</typeparam>
    /// <param name="source">The source enumerable being iterated.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> instance that enumerates the source <see cref="IAsyncEnumerable{T}"/> in a blocking manner.</returns>
    /// <remarks>
    /// This method is implemented by using deferred execution. The underlying <see cref="IAsyncEnumerable{T}"/> will not be enumerated
    /// unless the returned <see cref="IEnumerable{T}"/> is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
    /// Async enumeration does not happen in the background; each MoveNext call will invoke the underlying <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> exactly once.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    public static IEnumerable<T> ToBlockingEnumerable<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(cancellationToken);
        // A ManualResetEventSlim variant that lets us reuse the same
        // awaiter callback allocation across the entire enumeration.
        ManualResetEventWithAwaiterSupport? mres = null;

        try
        {
            while (true)
            {
#pragma warning disable CA2012 // Use ValueTasks correctly
                ValueTask<bool> moveNextTask = enumerator.MoveNextAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly

                if (!moveNextTask.IsCompleted)
                {
                    (mres ??= new()).Wait(moveNextTask.ConfigureAwait(false).GetAwaiter());
                    Debug.Assert(moveNextTask.IsCompleted);
                }

                if (!moveNextTask.Result)
                {
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            ValueTask disposeTask = enumerator.DisposeAsync();

            if (!disposeTask.IsCompleted)
            {
                (mres ?? new()).Wait(disposeTask.ConfigureAwait(false).GetAwaiter());
                Debug.Assert(disposeTask.IsCompleted);
            }

            disposeTask.GetAwaiter().GetResult();
        }
    }

    private sealed class ManualResetEventWithAwaiterSupport : ManualResetEventSlim
    {
        private readonly Action _onCompleted;

        public ManualResetEventWithAwaiterSupport()
        {
            _onCompleted = Set;
        }

        [UnsupportedOSPlatform("browser")]
        public void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            awaiter.UnsafeOnCompleted(_onCompleted);
            Wait();
            Reset();
        }
    }
}
#endif
