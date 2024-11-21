using CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
    public static IMvcBuilder AddAsyncValidation(this IMvcBuilder builder)
    {
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptionsSetup>();
        builder.Services.AddSingleton<ParameterBinder, AsyncParamterBinder>();
        builder.Services.TryAddSingleton<ValidatorCache>();
        builder.Services.TryAddSingleton<IAsyncObjectModelValidator>(s =>
        {
            var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
            var cache = s.GetRequiredService<ValidatorCache>();
            var metadataProvider = s.GetRequiredService<IModelMetadataProvider>();
            return new AsyncObjectModelValidator(metadataProvider, options.ModelValidatorProviders, cache, options);
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

            options.ModelValidatorProviders.Insert(0, new DefaultAsyncModelValidatorProvider());
            options.ModelValidatorProviders.Insert(0, new AsyncDataAnnotationsModelValidatorProvider(
                _validationAttributeAdapterProvider,
                _dataAnnotationLocalizationOptions,
                _stringLocalizerFactory));
        }
    }
}