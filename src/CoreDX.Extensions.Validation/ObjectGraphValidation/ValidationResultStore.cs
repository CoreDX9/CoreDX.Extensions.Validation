// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Holds validation results.
/// </summary>
public sealed class ValidationResultStore : IEnumerable<KeyValuePair<FieldIdentifier, List<ValidationResult>>>
{
    private readonly Dictionary<FieldIdentifier, List<ValidationResult>> _results = new Dictionary<FieldIdentifier, List<ValidationResult>>();

    /// <summary>
    /// Adds a validation result for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">The identifier for the field.</param>
    /// <param name="result">The validation result.</param>
    public void Add(FieldIdentifier fieldIdentifier, ValidationResult result)
        => GetOrCreateResultsListForField(fieldIdentifier).Add(result);

    /// <summary>
    /// Adds the results from the specified collection for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">The identifier for the field.</param>
    /// <param name="results">The validation results to be added.</param>
    public void Add(FieldIdentifier fieldIdentifier, IEnumerable<ValidationResult> results)
        => GetOrCreateResultsListForField(fieldIdentifier).AddRange(results);

    /// <summary>
    /// Gets the validation results within this <see cref="ValidationResultStore"/> for the specified field.
    ///
    /// To get the validation results across all validation result stores.
    /// </summary>
    /// <param name="fieldIdentifier">The identifier for the field.</param>
    /// <returns>The validation results for the specified field within this <see cref="ValidationResultStore"/>.</returns>
    public IEnumerable<ValidationResult> this[FieldIdentifier fieldIdentifier]
        => _results.TryGetValue(fieldIdentifier, out var results) ? results : Array.Empty<ValidationResult>();

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<FieldIdentifier, List<ValidationResult>>> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_results).GetEnumerator();
    }

    private List<ValidationResult> GetOrCreateResultsListForField(FieldIdentifier fieldIdentifier)
    {
        if (!_results.TryGetValue(fieldIdentifier, out var resultsForField))
        {
            resultsForField = new List<ValidationResult>();
            _results.Add(fieldIdentifier, resultsForField);
        }

        return resultsForField;
    }
}
