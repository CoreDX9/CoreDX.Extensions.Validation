// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Uniquely identifies a single field. This may correspond to a property on a model object, or can be any other named value.
/// </summary>
/// <remarks>
/// If <see cref="Model"/> is value type, It's a new instance copied from original object.
/// Directly modifying <see cref="Model"/> may not eliminate validation errors.
/// </remarks>
public sealed class FieldIdentifier : IEquatable<FieldIdentifier>
{
    /// <summary>
    /// Gets the fake object to use as top level object like parameter or local variable.
    /// </summary>
    private static readonly object TopLevelObjectFaker = new();

    /// <summary>
    /// Gets the fake object identitier to use as top level object like parameter or local variable.
    /// </summary>
    /// <param name="fieldName">The name of the editable field.</param>
    /// <returns>The field identifier.</returns>
    public static FieldIdentifier GetFakeTopLevelObjectIdentifier(string fieldName)
    {
        return new(TopLevelObjectFaker, fieldName, null);
    }

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

        CheckTopLevelObjectFaker(model, modelOwner);

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
        //if (model.GetType().IsValueType)
        //{
        //    throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
        //}

        Model = model ?? throw new ArgumentNullException(nameof(model));

        CheckTopLevelObjectFaker(model, modelOwner);

        if (enumerableElementIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enumerableElementIndex), "The index must be great than or equals 0.");
        }

        EnumerableElementIndex = enumerableElementIndex;
        
        ModelOwner = modelOwner;
    }

    private static void CheckTopLevelObjectFaker(object model, FieldIdentifier? modelOwner)
    {
        if (model == TopLevelObjectFaker && modelOwner is not null)
        {
            throw new ArgumentException($"{nameof(modelOwner)} must be null when {nameof(model)} is {nameof(TopLevelObjectFaker)}", nameof(modelOwner));
        }
    }

    /// <summary>
    /// Gets the object that owns the editable field.
    /// </summary>
    public object Model { get; }

    /// <summary>
    /// Gets if the <see cref="Model"/> is value type and not original instance.
    /// </summary>
    public bool ModelIsCopiedInstanceOfValueType => Model.GetType().IsValueType;

    /// <summary>
    /// Gets if the <see cref="Model"/> is top level fake object.
    /// </summary>
    public bool ModelIsTopLevelFakeObject => Model == TopLevelObjectFaker;

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
        return (ReferenceEquals(otherIdentifier?.Model, Model) || Equals(otherIdentifier?.Model, Model))
            && string.Equals(otherIdentifier?.FieldName, FieldName, StringComparison.Ordinal)
            && Nullable.Equals(otherIdentifier?.EnumerableElementIndex, EnumerableElementIndex)
            && ReferenceEquals(otherIdentifier?.ModelOwner, ModelOwner);
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
    public override string? ToString()
    {
        if (ModelIsTopLevelFakeObject) return FieldName;

        var sb = new StringBuilder();
        var fieldIdentifier = this;
        var chainHasTopLevelFaker = false;
        do
        {
            sb.Insert(0, fieldIdentifier.FieldName is not null ? $".{fieldIdentifier.FieldName}" : $"[{fieldIdentifier.EnumerableElementIndex}]");

            if (chainHasTopLevelFaker is false && fieldIdentifier.ModelIsTopLevelFakeObject) chainHasTopLevelFaker = true;

            fieldIdentifier = fieldIdentifier.ModelOwner;

        } while (fieldIdentifier != null && !fieldIdentifier.ModelIsTopLevelFakeObject);

        if (fieldIdentifier is null && !chainHasTopLevelFaker) sb.Insert(0, "$");
        else if (fieldIdentifier!.ModelIsTopLevelFakeObject) sb.Insert(0, fieldIdentifier.FieldName);

        return sb.ToString();
    }
}
