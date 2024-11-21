// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Uniquely identifies a single field that can be edited. This may correspond to a property on a
/// model object, or can be any other named value.
/// </summary>
public sealed class FieldIdentifier : IEquatable<FieldIdentifier>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldIdentifier"/> class.
    /// </summary>
    /// <param name="model">The object that owns the field.</param>
    /// <param name="fieldName">The name of the editable field.</param>
    /// <param name="modelOwner">The object that owns the model.</param>
    public FieldIdentifier(object model, string fieldName, FieldIdentifier? modelOwner)
    {
        
        //if (model.GetType().IsValueType)
        //{
        //    throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
        //}

        Model = model ?? throw new ArgumentNullException(nameof(model));

        // Note that we do allow an empty string. This is used by some validation systems
        // as a place to store object-level (not per-property) messages.
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

        ModelOwner = modelOwner;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldIdentifier"/> class.
    /// </summary>
    /// <param name="model">The object that owns the field.</param>
    /// <param name="enumerableElementIndex">The index of the element of enumerable field.</param>
    /// <param name="modelOwner">The object that owns the model.</param>
    public FieldIdentifier(object model, int enumerableElementIndex, FieldIdentifier? modelOwner)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        //if (model.GetType().IsValueType)
        //{
        //    throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
        //}

        if (enumerableElementIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enumerableElementIndex), "The index must be great than or equals 0.");
        }

        Model = model;

        EnumerableElementIndex = enumerableElementIndex;

        ModelOwner = modelOwner;
    }

    /// <summary>
    /// Gets the object that owns the editable field.
    /// </summary>
    public object Model { get; }

    /// <summary>
    /// Gets the name of the editable field.
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// Gets the element index of the enumerable field.
    /// </summary>
    public int? EnumerableElementIndex { get; }

    /// <summary>
    /// Gets the owner of the model.
    /// </summary>
    public FieldIdentifier? ModelOwner { get; }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // We want to compare Model instances by reference. RuntimeHelpers.GetHashCode returns identical hashes for equal object references (ignoring any `Equals`/`GetHashCode` overrides) which is what we want.
        var modelHash = RuntimeHelpers.GetHashCode(Model);
        var fieldHash = FieldName is null ? 0 : StringComparer.Ordinal.GetHashCode(FieldName);
        var indexHash = EnumerableElementIndex ?? 0;
        var ownerHash = RuntimeHelpers.GetHashCode(ModelOwner);
        return (modelHash, fieldHash, indexHash, ownerHash).GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is FieldIdentifier otherIdentifier
        && Equals(otherIdentifier);

    /// <inheritdoc />
    public bool Equals(FieldIdentifier? otherIdentifier)
    {
        return ReferenceEquals(otherIdentifier?.Model, Model)
            && string.Equals(otherIdentifier.FieldName, FieldName, StringComparison.Ordinal)
            && Nullable.Equals(otherIdentifier.EnumerableElementIndex, EnumerableElementIndex)
            && ReferenceEquals(otherIdentifier.ModelOwner, ModelOwner);
    }

    /// <inheritdoc/>
    public static bool operator ==(FieldIdentifier? left, FieldIdentifier? right)
    {
        if (left is not null) return left.Equals(right);
        if (right is not null) return right.Equals(left);
        return Equals(left, right);
    }

    /// <inheritdoc/>
    public static bool operator !=(FieldIdentifier? left, FieldIdentifier? right) => !(left == right);

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var fieldIdentifier = this;
        do
        {
            sb.Insert(0, fieldIdentifier.FieldName is not null ? $".{fieldIdentifier.FieldName}" : $"[{fieldIdentifier.EnumerableElementIndex}]");
            fieldIdentifier = fieldIdentifier.ModelOwner;
        } while (fieldIdentifier != null);

        sb.Insert(0, "$");

        return sb.ToString();
    }
}
