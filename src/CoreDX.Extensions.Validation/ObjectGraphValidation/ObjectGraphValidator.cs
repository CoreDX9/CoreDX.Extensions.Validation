// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
#if NET6_0_OR_GREATER
using System.Collections.Immutable;
#endif
using System.ComponentModel;
using System.Diagnostics;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
/// Helper class to validate object graph of objects and properties using their associated <see cref="ValidationAttribute"/>
/// custom attributes.
/// </summary>
public static partial class ObjectGraphValidator
{
    internal const string _validationContextInstanceTypeNotStaticallyDiscovered = "The Type of validationContext.ObjectType cannot be statically discovered.";
    internal const string _validationInstanceTypeNotStaticallyDiscovered = "The Type of instance cannot be statically discovered and the Type's properties can be trimmed.";

    private static readonly object _validatedObjectsKey = new object();
    private static readonly object _validateObjectOwnerKey = new object();

    private static readonly ValidationAttributeStore _store = ValidationAttributeStore.Instance;

    private static readonly
#if NET8_0_OR_GREATER
        FrozenSet<Type>
#elif NET6_0_OR_GREATER
        ImmutableHashSet<Type>
#else
        HashSet<Type>
#endif
        _disableBuiltInTypesForValidation =

#if NET8_0_OR_GREATER
        FrozenSet.ToFrozenSet(
#elif NET6_0_OR_GREATER
        ImmutableHashSet.Create(
#else
        new HashSet<Type>(
#endif
    [
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
#if NET7_0_OR_GREATER
        typeof(Int128),
#endif
        typeof(BigInteger),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(Complex),
        typeof(bool),
        typeof(char),
        typeof(string),
        typeof(object),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
#if NET6_0_OR_GREATER
        typeof(DateOnly),
        typeof(TimeOnly),
#endif
        typeof(TimeSpan),
        typeof(DBNull),
        typeof(Uri),
        typeof(Version),
        typeof(BitArray),
    ]);
    
    private static readonly
#if NET8_0_OR_GREATER
        FrozenSet<Type>
#elif NET6_0_OR_GREATER
        ImmutableHashSet<Type>
#else
        HashSet<Type>
#endif
        _disableBuiltInSingleTypeArgumentGenericSetTypesForValidation =

#if NET8_0_OR_GREATER
        FrozenSet.ToFrozenSet(
#elif NET6_0_OR_GREATER
        ImmutableHashSet.Create(
#else
        new HashSet<Type>(
#endif
    [
        typeof(List<>),
        typeof(HashSet<>),
        typeof(Queue<>),
        typeof(Stack<>),
        typeof(SortedSet<>),
        typeof(LinkedList<>),
#if NET6_0_OR_GREATER
        typeof(ImmutableList<>),
        typeof(ImmutableHashSet<>),
        typeof(ImmutableQueue<>),
        typeof(ImmutableStack<>),
#endif
#if NET8_0_OR_GREATER
        typeof(FrozenSet<>),
#endif
        typeof(BlockingCollection<>),
        typeof(ConcurrentBag<>),
        typeof(ConcurrentQueue<>),
        typeof(ConcurrentStack<>),
    ]);

    /// <summary>
    /// Tests whether the given property value is valid.
    /// </summary>
    /// <remarks>
    /// This method will test each <see cref="ValidationAttribute"/> associated with the property
    /// identified by <paramref name="validationContext"/>.  If <paramref name="validationResults"/> is non-null,
    /// this method will add a <see cref="ValidationResult"/> to it for each validation failure.
    /// <para>
    /// If there is a <see cref="RequiredAttribute"/> found on the property, it will be evaluated before all other
    /// validation attributes.  If the required validator fails then validation will abort, adding that single
    /// failure into the <paramref name="validationResults"/> when applicable, returning a value of <c>false</c>.
    /// </para>
    /// <para>
    /// If <paramref name="validationResults"/> is null and there isn't a <see cref="RequiredAttribute"/> failure,
    /// then all validators will be evaluated.
    /// </para>
    /// </remarks>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the property member to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
# endif
    public static bool TryValidateProperty(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior)
        => TryValidateProperty(value, validationContext, validationResults, asyncValidationBehavior, predicate: null, throwOnFirstError: false);

    /// <summary>
    /// Tests whether the given property value is valid.
    /// </summary>
    /// <remarks>
    /// This method will test each <see cref="ValidationAttribute"/> associated with the property
    /// identified by <paramref name="validationContext"/>.  If <paramref name="validationResults"/> is non-null,
    /// this method will add a <see cref="ValidationResult"/> to it for each validation failure.
    /// <para>
    /// If there is a <see cref="RequiredAttribute"/> found on the property, it will be evaluated before all other
    /// validation attributes.  If the required validator fails then validation will abort, adding that single
    /// failure into the <paramref name="validationResults"/> when applicable, returning a value of <c>false</c>.
    /// </para>
    /// <para>
    /// If <paramref name="validationResults"/> is null and there isn't a <see cref="RequiredAttribute"/> failure,
    /// then all validators will be evaluated.
    /// </para>
    /// </remarks>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the property member to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
# endif
    public static bool TryValidateProperty(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        Func<Type, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return TryValidateProperty(value, validationContext, validationResults, asyncValidationBehavior, predicate, throwOnFirstError: false);
    }

    /// <summary>
    /// Tests whether the given property value is valid.
    /// </summary>
    /// <remarks>
    /// This method will test each <see cref="ValidationAttribute"/> associated with the property
    /// identified by <paramref name="validationContext"/>.  If <paramref name="validationResults"/> is non-null,
    /// this method will add a <see cref="ValidationResult"/> to it for each validation failure.
    /// <para>
    /// If there is a <see cref="RequiredAttribute"/> found on the property, it will be evaluated before all other
    /// validation attributes.  If the required validator fails then validation will abort, adding that single
    /// failure into the <paramref name="validationResults"/> when applicable, returning a value of <c>false</c>.
    /// </para>
    /// <para>
    /// If <paramref name="validationResults"/> is null and there isn't a <see cref="RequiredAttribute"/> failure,
    /// then all validators will be evaluated.
    /// </para>
    /// </remarks>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the property member to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
# endif
    private static bool TryValidateProperty(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        Func<Type, bool>? predicate,
        bool throwOnFirstError)
    {
        CheckValidationContext(validationContext);

        validationContext.Items.Add(_validatedObjectsKey, new HashSet<object>());
        validationContext.Items.Add(_validateObjectOwnerKey, new FieldIdentifier(validationContext.ObjectInstance, validationContext.MemberName!, null));

        // Throw if value cannot be assigned to this property.  That is not a validation exception.
        Type propertyType = _store.GetPropertyType(validationContext);
        string propertyName = validationContext.MemberName!;
        EnsureValidPropertyType(propertyName, propertyType, value);

        bool isValid = true;
        bool breakOnFirstError = (validationResults == null);

        IEnumerable<ValidationAttribute> attributes = _store.GetPropertyValidationAttributes(validationContext);

        foreach (ValidationError err in GetValidationErrors(value, validationContext, attributes, asyncValidationBehavior, breakOnFirstError))
        {
            if (throwOnFirstError) err.ThrowValidationException();

            isValid = false;

            if (breakOnFirstError) break;

            TransferErrorToResult(validationResults!, err);
        }

        if (value is not null)
        {
            var propertyObjectsAreValid = TryValidatePropertyObjects(
                value,
                validationContext,
                validationResults,
                asyncValidationBehavior,
                validateAllProperties: true,
                predicate,
                throwOnFirstError);
            if (isValid && !propertyObjectsAreValid) isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Tests whether the given object instance is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  It does not validate the
    /// property values of the object.
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be <c>null</c>.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static bool TryValidateObject(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior)
        => TryValidateObject(instance, validationContext, validationResults, asyncValidationBehavior, validateAllProperties: false, predicate: null);

    /// <summary>
    /// Tests whether the given object instance is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  It does not validate the
    /// property values of the object.
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be <c>null</c>.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static bool TryValidateObject(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties)
        => TryValidateObject(instance, validationContext, validationResults, asyncValidationBehavior, validateAllProperties, predicate: null);

    /// <summary>
    /// Tests whether the given object instance is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  It does not validate the
    /// property values of the object.
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be <c>null</c>.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static bool TryValidateObject(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        Func<Type, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return TryValidateObject(instance, validationContext, validationResults, asyncValidationBehavior, validateAllProperties: false, predicate);
    }

    /// <summary>
    /// Tests whether the given object instance is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  If <paramref name="validateAllProperties"/>
    /// is <c>true</c>, this method will also evaluate the <see cref="ValidationAttribute"/>s for all the immediate properties
    /// of this object.  This process is recursive.
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// <para>
    /// For any given property, if it has a <see cref="RequiredAttribute"/> that fails validation, no other validators
    /// will be evaluated for that property.
    /// </para>
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static bool TryValidateObject(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        Func<Type, bool>? predicate)
    {
        CheckValidationContext(validationContext);

        validationContext.Items.Add(_validatedObjectsKey, new HashSet<object>());
        validationContext.Items.Add(_validateObjectOwnerKey, null);
        return TryValidateObjectRecursive(instance, validationContext, validationResults, asyncValidationBehavior, validateAllProperties, predicate, throwOnFirstError: false);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given property <paramref name="value"/> is not valid.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ValidationException">When <paramref name="value"/> is invalid for this property.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
# endif
    public static void ValidateProperty(
        object? value,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior)
        => TryValidateProperty(value, validationContext, validationResults: null, asyncValidationBehavior, predicate: null, throwOnFirstError: true);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given property <paramref name="value"/> is not valid.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ValidationException">When <paramref name="value"/> is invalid for this property.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
# endif
    public static void ValidateProperty(
        object? value,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        Func<Type, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        TryValidateProperty(value, validationContext, validationResults: null, asyncValidationBehavior, predicate, throwOnFirstError: true);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static void ValidateObject(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior)
        => ValidateObject(instance, validationContext, asyncValidationBehavior, validateAllProperties: false, predicate: null);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static void ValidateObject(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        Func<Type, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        ValidateObject(instance, validationContext, asyncValidationBehavior, validateAllProperties: false, predicate);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static void ValidateObject(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties)
        => ValidateObject(instance, validationContext, asyncValidationBehavior, validateAllProperties, predicate: null);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given object instance is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// If <paramref name="validateAllProperties"/> is <c>true</c> it also validates all the object's properties.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also validates all the <paramref name="instance"/>'s properties.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static void ValidateObject(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        Func<Type, bool>? predicate)
    {
        validationContext.Items.Add(_validatedObjectsKey, new HashSet<object>());
        validationContext.Items.Add(_validateObjectOwnerKey, null);
        TryValidateObjectRecursive(
            instance,
            validationContext,
            validationResults: null,
            asyncValidationBehavior,
            validateAllProperties,
            predicate,
            throwOnFirstError: true);
    }

    /// <summary>
    /// Creates a new <see cref="ValidationContext"/> to use to validate the type or a member of
    /// the given object instance.
    /// </summary>
    /// <param name="instance">The object instance to use for the context.</param>
    /// <param name="validationContext">An parent validation context that supplies an <see cref="IServiceProvider"/>
    /// and <see cref="ValidationContext.Items"/>.</param>
    /// <param name="instanceOwner">The object that owns the <paramref name="instance"/>.</param>
    /// <returns>A new <see cref="ValidationContext"/> for the <paramref name="instance"/> provided.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static ValidationContext CreateValidationContext(object instance, ValidationContext validationContext, FieldIdentifier? instanceOwner)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        // Create a new context using the existing ValidationContext that acts as an IServiceProvider and contains our existing items.
        ValidationContext context = new ValidationContext(instance, validationContext, validationContext.Items);
        context.Items[_validateObjectOwnerKey] = instanceOwner;
        return context;
    }

    /// <summary>
    /// Determine whether the given value can legally be assigned into the specified type.
    /// </summary>
    /// <param name="destinationType">The destination <see cref="Type"/> for the value.</param>
    /// <param name="value">The value to test to see if it can be assigned as the Type indicated by <paramref name="destinationType"/>.</param>
    /// <returns><c>true</c> if the assignment is legal.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="destinationType"/> is null.</exception>
    private static bool CanBeAssigned(Type destinationType, object? value)
    {
        if (destinationType == null)
        {
            throw new ArgumentNullException(nameof(destinationType));
        }

        if (value == null)
        {
            // Null can be assigned only to reference types or Nullable or Nullable<>
            return !destinationType.IsValueType ||
                    (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        // Not null -- be sure it can be cast to the right type
        return destinationType.IsAssignableFrom(value.GetType());
    }

    /// <summary>
    /// Determines whether the given value can legally be assigned to the given property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <param name="value">The value.  Null is permitted only if the property will accept it.</param>
    /// <exception cref="ArgumentException"> is thrown if <paramref name="value"/> is the wrong type for this property.</exception>
    private static void EnsureValidPropertyType(string propertyName, Type propertyType, object? value)
    {
        if (!CanBeAssigned(propertyType, value))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "The value for property '{0}' must be of type '{1}'.", propertyName, propertyType), nameof(value));
        }
    }

    /// <summary>
    /// Internal iterator to enumerate all validation errors for the given object instance.
    /// </summary>
    /// <param name="instance">Object instance to test.</param>
    /// <param name="validationContext">Describes the object type.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">if <c>true</c> also validates all properties.</param>
    /// <param name="breakOnFirstError">Whether to break on the first error or validate everything.</param>
    /// <returns>A collection of validation errors that result from validating the <paramref name="instance"/> with
    /// the given <paramref name="validationContext"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static List<ValidationError> GetObjectValidationErrors(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        bool breakOnFirstError)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        Debug.Assert(instance != null);

        // Step 1: Validate the object properties' validation attributes
        List<ValidationError> errors = GetObjectPropertyValidationErrors(instance!, validationContext, asyncValidationBehavior, validateAllProperties, breakOnFirstError);

        // We only proceed to Step 2 if there are no errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Step 2: Validate the object's validation attributes
        IEnumerable<ValidationAttribute> attributes = _store.GetTypeValidationAttributes(validationContext);
        errors.AddRange(GetValidationErrors(instance, validationContext, attributes, asyncValidationBehavior, breakOnFirstError));

        // We only proceed to Step 3 if there are no errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Step 3: Test for IValidatableObject implementation
        if (instance is IValidatableObject validatable)
        {
            var results = validatable.Validate(validationContext);

            if (results != null)
            {
                foreach (ValidationResult result in results)
                {
                    if (result != ValidationResult.Success)
                    {
                        errors.Add(new ValidationError(null, instance, result, validationContext));
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Internal iterator to enumerate all the validation errors for all properties of the given object instance.
    /// </summary>
    /// <param name="instance">Object instance to test.</param>
    /// <param name="validationContext">Describes the object type.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, evaluates all the properties, otherwise just checks that
    /// ones marked with <see cref="RequiredAttribute"/> are not null.</param>
    /// <param name="breakOnFirstError">Whether to break on the first error or validate everything.</param>
    /// <returns>A list of <see cref="ValidationError"/> instances.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static List<ValidationError> GetObjectPropertyValidationErrors(
        object instance,
        ValidationContext validationContext,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        bool breakOnFirstError)
    {
        List<KeyValuePair<ValidationContext, object?>> properties = GetPropertyValues(instance, validationContext);
        List<ValidationError> errors = new List<ValidationError>();

        foreach (KeyValuePair<ValidationContext, object?> property in properties)
        {
            // get list of all validation attributes for this property
            IEnumerable<ValidationAttribute> attributes = _store.GetPropertyValidationAttributes(property.Key);

            if (validateAllProperties)
            {
                // validate all validation attributes on this property
                errors.AddRange(GetValidationErrors(property.Value, property.Key, attributes, asyncValidationBehavior, breakOnFirstError));
            }
            else
            {
                // only validate the Required attributes
                RequiredAttribute? reqAttr = attributes.FirstOrDefault(a => a is RequiredAttribute) as RequiredAttribute;
                if (reqAttr != null)
                {
                    // Note: we let the [Required] attribute do its own null testing,
                    // since the user may have subclassed it and have a deeper meaning to what 'required' means
                    ValidationResult? validationResult = reqAttr.GetValidationResult(property.Value, property.Key);
                    if (validationResult != ValidationResult.Success)
                    {
                        errors.Add(new ValidationError(reqAttr, property.Value, validationResult!, property.Key));
                    }
                }
            }

            if (breakOnFirstError && errors.Count > 0)
            {
                break;
            }
        }

        return errors;
    }

    /// <summary>
    /// Retrieves the property values for the given instance.
    /// </summary>
    /// <param name="instance">Instance from which to fetch the properties.</param>
    /// <param name="validationContext">Describes the entity being validated.</param>
    /// <returns>A set of key value pairs, where the key is a validation context for the property and the value is its current
    /// value.</returns>
    /// <remarks>Ignores indexed properties.</remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static List<KeyValuePair<ValidationContext, object?>> GetPropertyValues(object instance, ValidationContext validationContext)
    {
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(instance);
        List<KeyValuePair<ValidationContext, object?>> items = new List<KeyValuePair<ValidationContext, object?>>(properties.Count);
        foreach (PropertyDescriptor property in properties)
        {
            ValidationContext context = CreateValidationContext(
                instance,
                validationContext,
                instanceOwner: new FieldIdentifier(
                    instance,
                    property.Name,
                    (FieldIdentifier?)validationContext.Items[_validateObjectOwnerKey]
                )
            );

            context.MemberName = property.Name;

            if (_store.GetPropertyValidationAttributes(context).Any())
            {
                items.Add(new KeyValuePair<ValidationContext, object?>(context, property.GetValue(instance)));
            }
        }
        return items;
    }

    /// <summary>
    /// Retrieves the property values for the given instance as object self.
    /// </summary>
    /// <param name="instance">Instance from which to fetch the properties.</param>
    /// <param name="validationContext">Describes the entity being validated.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <returns>A set of key value pairs, where the key is a validation context for the property and the value is its current
    /// value.</returns>
    /// <remarks>Ignores indexed properties.</remarks>
    private static List<KeyValuePair<ValidationContext, object>> GetPropertyObjectValues(
        object instance,
        ValidationContext validationContext,
        Func<Type, bool>? predicate)
    {
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(instance);
        List<KeyValuePair<ValidationContext, object>> items = new List<KeyValuePair<ValidationContext, object>>(properties.Count);
        foreach (PropertyDescriptor property in properties)
        {
            if (property.GetValue(instance) is object propertyObject
                && IsValidatableType(property.PropertyType, predicate))
            {
                ValidationContext context = CreateValidationContext(
                    propertyObject,
                    validationContext,
                    instanceOwner: new FieldIdentifier(
                        instance,
                        property.Name,
                        (FieldIdentifier?)validationContext.Items[_validateObjectOwnerKey]
                    )
                );

                items.Add(new KeyValuePair<ValidationContext, object>(context, propertyObject));
            }
        }
        
        return items;
    }

    /// <summary>
    /// Test whether an object of the <paramref name="type"/> should be validated as an object.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to test.</param>
    /// <param name="predicate">
    /// <para>A predicate to filter whether the <paramref name="type"/> should be validated.</para>
    /// <para>This method won't use some special built-in types to call the predicate.</para>
    /// </param>
    /// <returns>If object of the <paramref name="type"/> should be validated, return <see langword="true"/>; otherwise, return <see langword="false"/>.</returns>
    /// <remarks>Some special built-in types (eg. <see cref="int"/>, array of <see cref="int"/>) always return <see langword="false"/>.</remarks>
    private static bool IsValidatableType(Type type, Func<Type, bool>? predicate)
    {
        if (type.IsEnum) return false;
        if (typeof(Delegate).IsAssignableFrom(type)) return false;
        if (_disableBuiltInTypesForValidation.Contains(type)) return false;
        if (type.IsArray && !IsValidatableType(type.GetElementType()!, predicate)) return false;

        if (type.IsGenericType
            && ( (type.IsValueType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                || _disableBuiltInSingleTypeArgumentGenericSetTypesForValidation.Contains(type.GetGenericTypeDefinition())
            )
            && !IsValidatableType(type.GenericTypeArguments[0], predicate)) return false;

        if (predicate?.Invoke(type) is false) return false;

        return true;
    }

    /// <summary>
    /// Internal iterator to enumerate all validation errors for an value.
    /// </summary>
    /// <remarks>
    /// If a <see cref="RequiredAttribute"/> is found, it will be evaluated first, and if that fails,
    /// validation will abort, regardless of the <paramref name="breakOnFirstError"/> parameter value.
    /// </remarks>
    /// <param name="value">The value to pass to the validation attributes.</param>
    /// <param name="validationContext">Describes the type/member being evaluated.</param>
    /// <param name="attributes">The validation attributes to evaluate.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="breakOnFirstError">Whether or not to break on the first validation failure.  A
    /// <see cref="RequiredAttribute"/> failure will always abort with that sole failure.</param>
    /// <returns>The collection of validation errors.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    private static List<ValidationError> GetValidationErrors(
        object? value,
        ValidationContext validationContext,
        IEnumerable<ValidationAttribute> attributes,
        AsyncValidationBehavior asyncValidationBehavior,
        bool breakOnFirstError)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        List<ValidationError> errors = new List<ValidationError>();

        // Get the required validator if there is one and test it first, aborting on failure
        RequiredAttribute? required = attributes.FirstOrDefault(a => a is RequiredAttribute) as RequiredAttribute;
        if (required != null)
        {
            var result = TryValidate(value, validationContext, required, asyncValidationBehavior, out var validationError);
            if (!result)
            {
                errors.Add(validationError!);
                return errors;
            }
        }

        // Iterate through the rest of the validators, skipping the required validator
        foreach (ValidationAttribute attr in attributes)
        {
            if (attr != required)
            {
                var result = TryValidate(value, validationContext, attr, asyncValidationBehavior, out var validationError);
                if (!result)
                {
                    errors.Add(validationError!);

                    if (breakOnFirstError)
                    {
                        break;
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Tests recursively whether the given object instance is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  If <paramref name="validateAllProperties"/>
    /// is <c>true</c>, this method will also evaluate the <see cref="ValidationAttribute"/>s for all the immediate properties
    /// of this object.  This process is recursive.
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// <para>
    /// For any given property, if it has a <see cref="RequiredAttribute"/> that fails validation, no other validators
    /// will be evaluated for that property.
    /// </para>
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
    private static bool TryValidateObjectRecursive(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }
        if (instance != validationContext.ObjectInstance)
        {
            throw new ArgumentException("The instance provided must match the ObjectInstance on the ValidationContext supplied.", nameof(instance));
        }

        if (!(validationContext.Items.TryGetValue(_validatedObjectsKey, out var item)
            && item is HashSet<object> visited
            && visited.Add(instance)))
        {
            return true;
        }

        bool isValid = true;
        bool breakOnFirstError = (validationResults == null);

        foreach (ValidationError err in GetObjectValidationErrors(
            instance,
            validationContext,
            asyncValidationBehavior,
            validateAllProperties,
            breakOnFirstError))
        {
            if (throwOnFirstError) err.ThrowValidationException();

            isValid = false;

            if (breakOnFirstError) break;

            TransferErrorToResult(validationResults!, err);
        }

        if (!isValid && breakOnFirstError) return isValid;

        var propertyObjectsAreValid = TryValidatePropertyObjects(
            instance,
            validationContext,
            validationResults,
            asyncValidationBehavior,
            validateAllProperties,
            predicate,
            throwOnFirstError);

        if (isValid && !propertyObjectsAreValid) isValid = false;

        return isValid;
    }

    /// <summary>
    /// Tests whether the properties of given object instance as object is valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object instance's type.  It also
    /// checks to ensure all properties marked with <see cref="RequiredAttribute"/> are set.  If <paramref name="validateAllProperties"/>
    /// is <c>true</c>, this method will also evaluate the <see cref="ValidationAttribute"/>s for all the immediate properties
    /// of this object.  This process is recursive.
    /// <para>
    /// If the type of object implemented multiple <see cref="IEnumerable{T}"/>(or <see cref="IEnumerable"/>) interfaces,
    /// we only validate implicit <see cref="IEnumerable"/> implementation.
    /// </para>
    /// <para>
    /// If <paramref name="validationResults"/> is null, then execution will abort upon the first validation
    /// failure.  If <paramref name="validationResults"/> is non-null, then all validation attributes will be
    /// evaluated.
    /// </para>
    /// <para>
    /// For any given property, if it has a <see cref="RequiredAttribute"/> that fails validation, no other validators
    /// will be evaluated for that property.
    /// </para>
    /// </remarks>
    /// <param name="instance">The properties of object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object to validate and provides services and context for the validators.</param>
    /// <param name="validationResults">Optional collection to receive <see cref="ValidationResult"/>s for the failures.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
    private static bool TryValidatePropertyObjects(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError)
    {
        bool isValid = true;
        bool breakOnFirstError = (validationResults == null);

        List<KeyValuePair<ValidationContext, object>> propertyObjects = GetPropertyObjectValues(instance, validationContext, predicate);
        foreach (var propertyObject in propertyObjects)
        {
            var innerIsValid = TryValidateObjectRecursive(
                propertyObject.Value,
                propertyObject.Key,
                validationResults,
                asyncValidationBehavior,
                validateAllProperties,
                predicate,
                throwOnFirstError);
            if (isValid && !innerIsValid) isValid = false;
            if (!isValid && breakOnFirstError) break;
        }

        if (!isValid && breakOnFirstError) return isValid;

        if (instance is IEnumerable enumerable)
        {
            var index = -1;
            foreach (var item in enumerable)
            {
                index++;
                var innerIsValid = TryValidateEnumerableElement(
                    instance,
                    validationContext,
                    validationResults,
                    asyncValidationBehavior,
                    validateAllProperties,
                    predicate,
                    throwOnFirstError,
                    index,
                    item);
                if (isValid && !innerIsValid) isValid = false;
                if (!isValid && breakOnFirstError) break;
            }
        }

        return isValid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryValidateEnumerableElement(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        AsyncValidationBehavior asyncValidationBehavior,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError,
        int index,
        object? element)
    {
        if (element is null || !IsValidatableType(element.GetType(), predicate)) return true;

        return TryValidateObjectRecursive(
            element,
            CreateValidationContext(
                element,
                validationContext,
                new FieldIdentifier(instance, index, (FieldIdentifier?)validationContext.Items[_validateObjectOwnerKey])),
            validationResults,
            asyncValidationBehavior,
            validateAllProperties,
            predicate,
            throwOnFirstError);
    }

    /// <summary>
    /// Tests whether a value is valid against a single <see cref="ValidationAttribute"/> using the <see cref="ValidationContext"/>.
    /// </summary>
    /// <param name="value">The value to be tested for validity.</param>
    /// <param name="validationContext">Describes the property member to validate.</param>
    /// <param name="attribute">The validation attribute to test.</param>
    /// <param name="asyncValidationBehavior">The <see cref="AsyncValidationBehavior"/></param>
    /// <param name="validationError">
    ///     The validation error that occurs during validation.  Will be <c>null</c> when the return
    ///     value is <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    private static bool TryValidate(
        object? value,
        ValidationContext validationContext,
        ValidationAttribute attribute,
        AsyncValidationBehavior asyncValidationBehavior,
#if NET6_0_OR_GREATER
        [NotNullWhen(false)]
#endif
        out ValidationError? validationError)
    {
#if NET5_0_OR_GREATER
        if (!Enum.IsDefined(asyncValidationBehavior))
#else
        if (!Enum.IsDefined(typeof(AsyncValidationBehavior), asyncValidationBehavior))
#endif
        {
            throw new ArgumentException("Undefined value.", nameof(asyncValidationBehavior));
        }

        Debug.Assert(validationContext != null);

        ValidationResult? validationResult = null;

        if (attribute is AsyncValidationAttribute asyncAttribute)
        {
            validationResult = asyncValidationBehavior switch
            {
                AsyncValidationBehavior.Throw => attribute.GetValidationResult(value, validationContext),
                AsyncValidationBehavior.Ignore => ValidationResult.Success,
                AsyncValidationBehavior.TrySynchronously => TryValidateAsyncValidationSynchronously(value, validationContext!, asyncAttribute),
                _ => null, // NEVER TRUE
            };
        }
        else validationResult = attribute.GetValidationResult(value, validationContext);

        if (validationResult != ValidationResult.Success)
        {
            validationError = new ValidationError(attribute, value, validationResult!, validationContext!);
            return false;
        }

        validationError = null;
        return true;

        static ValidationResult? TryValidateAsyncValidationSynchronously(
            object? value,
            ValidationContext validationContext,
            AsyncValidationAttribute asyncAttribute)
        {
            ValidationResult? result = null;
            ManualResetEventSlimWithAwaiterSupport? mres = null;
            Exception? exception = null;

            try
            {
#pragma warning disable CA2012 // Use ValueTasks correctly
                var resultTask = asyncAttribute.GetValidationResultAsync(value, validationContext);
#pragma warning restore CA2012 // Use ValueTasks correctly

                if (!resultTask.IsCompleted)
                {
                    mres = new ManualResetEventSlimWithAwaiterSupport();
                    mres.Wait(resultTask.ConfigureAwait(false).GetAwaiter());

                    Debug.Assert(resultTask.IsCompleted);
                }

                result = resultTask.Result;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                mres?.Dispose();
            }

            if (exception is not null) throw exception;
            return result;
        }
    }

    private static void TransferErrorToResult(ValidationResultStore validationResultStore, ValidationError err)
    {
        var modelIdentyfier = (FieldIdentifier)err.ValidationContext.Items[_validateObjectOwnerKey]!;

        if (!err.ValidationResult.MemberNames.Any())
        {
            validationResultStore.Add(modelIdentyfier, err.ValidationResult);
            return;
        }

        PropertyDescriptorCollection? properties = null;
        foreach (var memberName in err.ValidationResult.MemberNames)
        {
            var resultModelIdentifier = modelIdentyfier;
            if (memberName != modelIdentyfier.FieldName)
            {
                properties ??= TypeDescriptor.GetProperties(modelIdentyfier.Model);

                if (properties.Find(memberName, false) is not null)
                {
                    resultModelIdentifier = new FieldIdentifier(modelIdentyfier.Model, memberName, modelIdentyfier.ModelOwner);
                }
            }

            validationResultStore.Add(resultModelIdentifier, err.ValidationResult);
        }
    }

    private static void CheckValidationContext(ValidationContext? validationContext)
    {
        if (validationContext is null) throw new ArgumentNullException(nameof(validationContext));

        if (validationContext.Items.ContainsKey(_validatedObjectsKey) || validationContext.Items.ContainsKey(_validateObjectOwnerKey))
        {
            throw new ArgumentException("The validation context has already been used.", nameof(validationContext));
        }
    }

    /// <summary>
    ///     Private helper class to encapsulate a ValidationAttribute with the failed value and the user-visible
    ///     target name against which it was validated.
    /// </summary>
    private sealed class ValidationError
    {
        private readonly ValidationAttribute? _validationAttribute;
        private readonly object? _value;

        internal ValidationError(
            ValidationAttribute? validationAttribute,
            object? value,
            ValidationResult validationResult,
            ValidationContext validationContext)
        {
            _validationAttribute = validationAttribute;
            _value = value;
            ValidationResult = validationResult;
            ValidationContext = validationContext;
        }

        internal ValidationResult ValidationResult { get; }
        internal ValidationContext ValidationContext { get; }

        internal void ThrowValidationException() => throw new ValidationException(ValidationResult, _validationAttribute, _value);
    }
}
