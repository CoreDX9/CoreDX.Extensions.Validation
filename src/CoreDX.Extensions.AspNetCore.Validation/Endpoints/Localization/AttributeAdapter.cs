using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.AspNetCore.Http.Validation.Localization;

/// <summary>
/// A class which wraps up <see cref="ValidationAttribute"/>.
/// </summary>
public sealed class AttributeAdapter : AttributeAdapterBase<ValidationAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(ValidationAttribute attribute)
    {
        return [];
    }
}

/// <summary>
/// A class which wraps up <see cref="CompareAttribute"/>.
/// </summary>
public sealed class CompareAttributeAdapter : AttributeAdapterBase<CompareAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(CompareAttribute attribute)
    {
        return [attribute.OtherPropertyDisplayName ?? attribute.OtherProperty];
    }
}

/// <summary>
/// A class which wraps up <see cref="DataTypeAttribute"/>.
/// </summary>
public sealed class DataTypeAttributeAdapter : AttributeAdapterBase<DataTypeAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(DataTypeAttribute attribute)
    {
        return [attribute.GetDataTypeName()];
    }
}

/// <summary>
/// A class which wraps up <see cref="FileExtensionsAttribute"/>.
/// </summary>
public sealed class FileExtensionsAttributeAdapter : AttributeAdapterBase<FileExtensionsAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(FileExtensionsAttribute attribute)
    {
        // Build the extension list based on how the JQuery Validation's 'extension' method expects it
        // https://jqueryvalidation.org/extension-method/

        // These lines follow the same approach as the FileExtensionsAttribute.
        var normalizedExtensions = attribute.Extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant();
        var parsedExtensions = normalizedExtensions.Split(',').Select(e => "." + e);
        var formattedExtensions = string.Join(", ", parsedExtensions);
        //var extensions = string.Join(",", parsedExtensions);
        return [formattedExtensions];
    }
}

/// <summary>
/// A class which wraps up <see cref="MaxLengthAttribute"/>.
/// </summary>
public sealed class MaxLengthAttributeAdapter : AttributeAdapterBase<MaxLengthAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(MaxLengthAttribute attribute)
    {
        return [attribute.Length];
    }
}

/// <summary>
/// A class which wraps up <see cref="MinLengthAttribute"/>.
/// </summary>
public sealed class MinLengthAttributeAdapter : AttributeAdapterBase<MinLengthAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(MinLengthAttribute attribute)
    {
        return [attribute.Length];
    }
}

/// <summary>
/// A class which wraps up <see cref="RangeAttribute"/>.
/// </summary>
public sealed class RangeAttributeAdapter : AttributeAdapterBase<RangeAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(RangeAttribute attribute)
    {
        return [attribute.Minimum, attribute.Maximum];
    }
}

/// <summary>
/// A class which wraps up <see cref="RegularExpressionAttribute"/>.
/// </summary>
public sealed class RegularExpressionAttributeAdapter : AttributeAdapterBase<RegularExpressionAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(RegularExpressionAttribute attribute)
    {
        return [attribute.Pattern];
    }
}

/// <summary>
/// A class which wraps up <see cref="RequiredAttribute"/>.
/// </summary>
public sealed class RequiredAttributeAdapter : AttributeAdapterBase<RequiredAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(RequiredAttribute attribute)
    {
        return null;
    }
}

/// <summary>
/// A class which wraps up <see cref="StringLengthAttribute"/>.
/// </summary>
public sealed class StringLengthAttributeAdapter : AttributeAdapterBase<StringLengthAttribute>
{
    /// <inheritdoc/>
    protected override object[]? GetLocalizationArgumentsInternal(StringLengthAttribute attribute)
    {
        return [attribute.MaximumLength, attribute.MinimumLength];
    }
}