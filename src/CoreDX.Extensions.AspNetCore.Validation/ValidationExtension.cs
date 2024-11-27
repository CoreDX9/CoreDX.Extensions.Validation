using CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Helper class for Add async validation services to MVC.
/// </summary>
public static class AsyncValidationExtension
{
    /// <summary>
    /// Adds async validation services to the specified <see cref="IMvcBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    public static IMvcBuilder AddAsyncDataAnnotations(this IMvcBuilder builder)
    {
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptionsSetup>();
        builder.Services.AddSingleton<ParameterBinder, AsyncParamterBinder>();
        builder.Services.TryAddSingleton<IAsyncObjectModelValidator>(s =>
        {
            var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
            var metadataProvider = s.GetRequiredService<IModelMetadataProvider>();
            return new DefaultAsyncObjecValidator(metadataProvider, options.ModelValidatorProviders, options);
        });
        return builder;
    }

    /// <summary>
    /// Adds async validation services to the specified <see cref="IMvcCoreBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>You should call <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/> before this.</remarks>
    public static IMvcCoreBuilder AddAsyncDataAnnotations(this IMvcCoreBuilder builder)
    {
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptionsSetup>();
        builder.Services.AddSingleton<ParameterBinder, AsyncParamterBinder>();
        builder.Services.TryAddSingleton<IAsyncObjectModelValidator>(s =>
        {
            var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
            var cache = s.GetRequiredService<ValidatorCache>();
            var metadataProvider = s.GetRequiredService<IModelMetadataProvider>();
            return new DefaultAsyncObjecValidator(metadataProvider, options.ModelValidatorProviders, options);
        });
        return builder;
    }

    internal sealed class ConfigureMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly IStringLocalizerFactory? _stringLocalizerFactory;
        private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
        private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _dataAnnotationLocalizationOptions;

        public ConfigureMvcOptionsSetup(
            IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
            IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions)
        {
            ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);
            ArgumentNullException.ThrowIfNull(dataAnnotationLocalizationOptions);

            _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
            _dataAnnotationLocalizationOptions = dataAnnotationLocalizationOptions;
        }

        public ConfigureMvcOptionsSetup(
            IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
            IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions,
            IStringLocalizerFactory stringLocalizerFactory)
            : this(validationAttributeAdapterProvider, dataAnnotationLocalizationOptions)
        {
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        public void Configure(MvcOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.ModelValidatorProviders.Insert(0, new AsyncDataAnnotationsModelValidatorProvider(
                _validationAttributeAdapterProvider,
                _dataAnnotationLocalizationOptions,
                _stringLocalizerFactory));

            options.ModelValidatorProviders.Insert(0, new DefaultAsyncModelValidatorProvider());
        }
    }
}

/// <summary>
/// Helper class for try validate model asynchronously in controllers and pages.
/// </summary>
public static class AsyncValidatiorExtension
{
    /// <summary>
    /// Validates the specified <paramref name="model"/> instance.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="model">The model to validate.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <see cref="ControllerBase.ModelState"/> is valid;<c>false</c> otherwise.</returns>
    public static Task<bool> TryValidateModelAsync(
        this ControllerBase controller,
        object model,
        CancellationToken cancellationToken = default)
    {
        return TryValidateModelAsync(controller, model, null, cancellationToken);
    }

    /// <summary>
    /// Validates the specified <paramref name="model"/> instance.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="model">The model to validate.</param>
    /// <param name="prefix">The key to use when looking up information in <see cref="ControllerBase.ModelState"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <see cref="ControllerBase.ModelState"/> is valid;<c>false</c> otherwise.</returns>
    public static async Task<bool> TryValidateModelAsync(
        this ControllerBase controller,
        object model,
        string? prefix,
        CancellationToken cancellationToken = default)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await TryValidateModelAsync(
            controller.ControllerContext,
            model: model,
            prefix: prefix ?? string.Empty,
            cancellationToken);

        return controller.ModelState.IsValid;
    }

    /// <summary>
    /// Validates the specified <paramref name="model"/> instance.
    /// </summary>
    /// <param name="page">The controller.</param>
    /// <param name="model">The model to validate.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <see cref="PageModel.ModelState"/> is valid;<c>false</c> otherwise.</returns>
    public static Task<bool> TryValidateModelAsync(
        this PageModel page,
        object model,
        CancellationToken cancellationToken = default)
    {
        return TryValidateModelAsync(page, model, null, cancellationToken);
    }

    /// <summary>
    /// Validates the specified <paramref name="model"/> instance.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="model">The model to validate.</param>
    /// <param name="prefix">The key to use when looking up information in <see cref="PageModel.ModelState"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <see cref="PageModel.ModelState"/> is valid;<c>false</c> otherwise.</returns>
    public static async Task<bool> TryValidateModelAsync(
        this PageModel page,
        object model,
        string? prefix,
        CancellationToken cancellationToken = default)
    {
        if (page is null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await TryValidateModelAsync(
            page.PageContext,
            model: model,
            prefix: prefix ?? string.Empty,
            cancellationToken);

        return page.ModelState.IsValid;
    }

    /// <summary>
    /// Validates the specified <paramref name="model"/> instance.
    /// </summary>
    /// <param name="context">The controller.</param>
    /// <param name="model">The model to validate.</param>
    /// <param name="prefix">The key to use when looking up information in <see cref="ActionContext.ModelState"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the <see cref="ActionContext.ModelState"/> is valid;<c>false</c> otherwise.</returns>
    private static async ValueTask TryValidateModelAsync(
        ActionContext context,
        object model,
        string? prefix,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IAsyncObjectModelValidator>();

        await validator.ValidateAsync(
            context,
            validationState: null,
            prefix: prefix ?? string.Empty,
            model: model,
            cancellationToken);
    }
}