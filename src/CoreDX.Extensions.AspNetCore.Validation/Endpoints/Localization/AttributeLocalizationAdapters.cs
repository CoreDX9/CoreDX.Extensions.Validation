using System.Collections;

namespace CoreDX.Extensions.AspNetCore.Http.Validation.Localization;

/// <summary>
/// A attribute localization adapter collection to provide <see cref="IAttributeAdapter"/>.
/// </summary>
public class AttributeLocalizationAdapters : IList<IAttributeAdapter>
{
    private readonly List<IAttributeAdapter> _provider = [];

    /// <inheritdoc/>
    public IAttributeAdapter this[int index]
    {
        get => _provider[index];
        set => _provider[index] = value;
    }

    #region IList<T> members

    /// <inheritdoc/>
    public int Count => _provider.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => ((ICollection<IAttributeAdapter>)_provider).IsReadOnly;

    /// <inheritdoc/>
    public void Add(IAttributeAdapter item) => _provider.Add(item);

    /// <inheritdoc/>
    public void Clear() => _provider.Clear();

    /// <inheritdoc/>
    public bool Contains(IAttributeAdapter item) => _provider.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(IAttributeAdapter[] array, int arrayIndex) => _provider.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<IAttributeAdapter> GetEnumerator() => _provider.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(IAttributeAdapter item) => _provider.IndexOf(item);

    /// <inheritdoc/>
    public void Insert(int index, IAttributeAdapter item) => _provider.Insert(index, item);

    /// <inheritdoc/>
    public bool Remove(IAttributeAdapter item) => _provider.Remove(item);

    /// <inheritdoc/>
    public void RemoveAt(int index) => _provider.RemoveAt(index);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
