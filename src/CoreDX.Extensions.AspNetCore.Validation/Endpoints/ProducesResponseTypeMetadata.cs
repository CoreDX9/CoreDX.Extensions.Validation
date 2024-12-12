namespace Microsoft.AspNetCore.Http;

#if NET7_0
/// <summary>
/// Specifies the type of the value and status code returned by the action.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
internal sealed class ProducesResponseTypeMetadata : Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    /// <param name="contentTypes">Content types supported by the response.</param>
    public ProducesResponseTypeMetadata(int statusCode, Type? type = null, string[]? contentTypes = null)
    {
        StatusCode = statusCode;
        Type = type;

        if (contentTypes is null || contentTypes.Length == 0)
        {
            ContentTypes = Enumerable.Empty<string>();
        }
        else
        {
            for (var i = 0; i < contentTypes.Length; i++)
            {
                Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentTypes[i]);
                ValidateContentType(contentTypes[i]);
            }

            ContentTypes = contentTypes;
        }

        static void ValidateContentType(string type)
        {
            if (type.Contains('*', StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Could not parse '{type}'. Content types with wildcards are not supported.");
            }
        }
    }

    // Only for internal use where validation is unnecessary.
    private ProducesResponseTypeMetadata(int statusCode, Type? type, IEnumerable<string> contentTypes)
    {
        Type = type;
        StatusCode = statusCode;
        ContentTypes = contentTypes;
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type? Type { get; private set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; private set; }

    /// <summary>
    /// Gets or sets the description of the response.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the content types associated with the response.
    /// </summary>
    public IEnumerable<string> ContentTypes { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(StatusCode), StatusCode, nameof(ContentTypes), ContentTypes, nameof(Type), Type, includeNullValues: false, prefix: "Produces");
    }

    internal static ProducesResponseTypeMetadata CreateUnvalidated(Type? type, int statusCode, IEnumerable<string> contentTypes) => new(statusCode, type, contentTypes);
}

internal static class DebuggerHelpers
{
    public static string GetDebugText(string key1, object? value1, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1) }, includeNullValues, prefix);
    }

    public static string GetDebugText(string key1, object? value1, string key2, object? value2, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1), Create(key2, value2) }, includeNullValues, prefix);
    }

    public static string GetDebugText(string key1, object? value1, string key2, object? value2, string key3, object? value3, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1), Create(key2, value2), Create(key3, value3) }, includeNullValues, prefix);
    }

    public static string GetDebugText(ReadOnlySpan<KeyValuePair<string, object?>> values, bool includeNullValues = true, string? prefix = null)
    {
        if (values.Length == 0)
        {
            return prefix ?? string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        if (prefix != null)
        {
            sb.Append(prefix);
        }

        var first = true;
        for (var i = 0; i < values.Length; i++)
        {
            var kvp = values[i];

            if (HasValue(kvp.Value) || includeNullValues)
            {
                if (first)
                {
                    if (prefix != null)
                    {
                        sb.Append(' ');
                    }

                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(kvp.Key);
                sb.Append(": ");
                if (kvp.Value is null)
                {
                    sb.Append("(null)");
                }
                else if (kvp.Value is string s)
                {
                    sb.Append(s);
                }
                else if (kvp.Value is System.Collections.IEnumerable enumerable)
                {
                    var firstItem = true;
                    foreach (var item in enumerable)
                    {
                        if (firstItem)
                        {
                            firstItem = false;
                        }
                        else
                        {
                            sb.Append(',');
                        }
                        sb.Append(item);
                    }
                }
                else
                {
                    sb.Append(kvp.Value);
                }
            }
        }

        return sb.ToString();
    }

    private static bool HasValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        // Empty collections don't have a value.
        if (value is not string && value is System.Collections.IEnumerable enumerable && !enumerable.GetEnumerator().MoveNext())
        {
            return false;
        }

        return true;
    }

    private static KeyValuePair<string, object?> Create(string key, object? value) => new KeyValuePair<string, object?>(key, value);
}
#endif
