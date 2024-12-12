using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CoreDX.Extensions.AspNetCore.Http.Validation;

/// <summary>
/// Parameter name and their validation results.
/// </summary>
public sealed class EndpointArgumentsValidationResults : IReadOnlyDictionary<string, ArgumentPropertiesValidationResults>
{
    private Dictionary<string, ArgumentPropertiesValidationResults> _argumentResults;

    internal EndpointArgumentsValidationResults(Dictionary<string, ArgumentPropertiesValidationResults> results)
    {
        _argumentResults = results;
    }

    #region IReadOnlyDictionary<TKey, TValue> members

    /// <inheritdoc/>
    public ArgumentPropertiesValidationResults this[string key] => _argumentResults[key];

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _argumentResults.Keys;

    /// <inheritdoc/>
    public IEnumerable<ArgumentPropertiesValidationResults> Values => _argumentResults.Values;

    /// <inheritdoc/>
    public int Count => ((IReadOnlyCollection<KeyValuePair<string, ArgumentPropertiesValidationResults>>)_argumentResults).Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _argumentResults.ContainsKey(key);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, ArgumentPropertiesValidationResults>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, ArgumentPropertiesValidationResults>>)_argumentResults).GetEnumerator();
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ArgumentPropertiesValidationResults value)
    {
        return _argumentResults.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}

/// <summary>
/// Argument object properties's key and their validation results.
/// </summary>
public sealed class ArgumentPropertiesValidationResults : IReadOnlyDictionary<string, ImmutableList<ValidationResult>>
{
    private Dictionary<string, ImmutableList<ValidationResult>> _propertyResults;

    internal ArgumentPropertiesValidationResults(Dictionary<string, ImmutableList<ValidationResult>> results)
    {
        _propertyResults = results;
    }

    #region IReadOnlyDictionary<TKey, TValue> members

    /// <inheritdoc/>
    public ImmutableList<ValidationResult> this[string key] => _propertyResults[key];

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _propertyResults.Keys;

    /// <inheritdoc/>
    public IEnumerable<ImmutableList<ValidationResult>> Values => _propertyResults.Values;

    /// <inheritdoc/>
    public int Count => _propertyResults.Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _propertyResults.ContainsKey(key);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, ImmutableList<ValidationResult>>> GetEnumerator()
    {
        return _propertyResults.GetEnumerator();
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ImmutableList<ValidationResult> value)
    {
        return _propertyResults.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}