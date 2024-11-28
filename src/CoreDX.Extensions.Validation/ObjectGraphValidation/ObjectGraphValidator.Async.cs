// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

public static partial class ObjectGraphValidator
{
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
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask<bool> TryValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        CancellationToken cancellationToken = default)
        => TryValidatePropertyAsync(value, validationContext, validationResults, predicate: null, throwOnFirstError: false, cancellationToken);

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
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask<bool> TryValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        Func<Type, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return TryValidatePropertyAsync(value, validationContext, validationResults, predicate, throwOnFirstError: false, cancellationToken);
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
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentException">
    /// When the <see cref="ValidationContext.MemberName"/> of <paramref name="validationContext"/> is not a valid property.
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    private static async ValueTask<bool> TryValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        Func<Type, bool>? predicate,
        bool throwOnFirstError,
        CancellationToken cancellationToken = default)
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

        foreach (ValidationError err in await GetValidationErrorsAsync(value, validationContext, attributes, breakOnFirstError, cancellationToken))
        {
            if (throwOnFirstError) err.ThrowValidationException();

            isValid = false;

            if (breakOnFirstError) break;

            TransferErrorToResult(validationResults!, err);
        }

        if (value is not null)
        {
            var propertyObjectsAreValid = await TryValidatePropertyObjectsAsync(
                value,
                validationContext,
                validationResults,
                validateAllProperties: true,
                predicate,
                throwOnFirstError,
                cancellationToken);
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
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        CancellationToken cancellationToken = default)
        => TryValidateObjectAsync(instance, validationContext, validationResults, validateAllProperties: false, predicate: null, cancellationToken);

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
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        bool validateAllProperties,
        CancellationToken cancellationToken = default)
        => TryValidateObjectAsync(instance, validationContext, validationResults, validateAllProperties, predicate: null, cancellationToken);

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
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        Func<Type, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return TryValidateObjectAsync(instance, validationContext, validationResults, validateAllProperties: false, predicate, cancellationToken);
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
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static async ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        CancellationToken cancellationToken = default)
    {
        CheckValidationContext(validationContext);

        validationContext.Items.Add(_validatedObjectsKey, new HashSet<object>());
        validationContext.Items.Add(_validateObjectOwnerKey, null);
        return await TryValidateObjectRecursiveAsync(instance, validationContext, validationResults, validateAllProperties, predicate, throwOnFirstError: false, cancellationToken);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given property <paramref name="value"/> is not valid.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ValidationException">When <paramref name="value"/> is invalid for this property.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public static async ValueTask ValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default)
        => await TryValidatePropertyAsync(value, validationContext, validationResults: null, predicate: null, throwOnFirstError: true, cancellationToken);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given property <paramref name="value"/> is not valid.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ValidationException">When <paramref name="value"/> is invalid for this property.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public static async ValueTask ValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        Func<Type, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        await TryValidatePropertyAsync(value, validationContext, validationResults: null, predicate, throwOnFirstError: true, cancellationToken);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default)
        => ValidateObjectAsync(instance, validationContext, validateAllProperties: false, predicate: null, cancellationToken);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators.  It cannot be <c>null</c>.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        Func<Type, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return ValidateObjectAsync(instance, validationContext, validateAllProperties: false, predicate, cancellationToken);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given <paramref name="instance"/> is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators. It cannot be <c>null</c>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is recursive over properties of the properties).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        bool validateAllProperties,
        CancellationToken cancellationToken = default)
        => ValidateObjectAsync(instance, validationContext, validateAllProperties, predicate: null, cancellationToken);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given object instance is not valid.
    /// </summary>
    /// <remarks>
    /// This method evaluates all <see cref="ValidationAttribute"/>s attached to the object's type.
    /// If <paramref name="validateAllProperties"/> is <c>true</c> it also validates all the object's properties.
    /// </remarks>
    /// <param name="instance">The object instance to test.  It cannot be null.</param>
    /// <param name="validationContext">Describes the object being validated and provides services and context for the validators. It cannot be <c>null</c>.</param>
    /// <param name="validateAllProperties">If <c>true</c>, also validates all the <paramref name="instance"/>'s properties.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
    /// <exception cref="ValidationException">When <paramref name="instance"/> is found to be invalid.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    public static async ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        CancellationToken cancellationToken = default)
    {
        validationContext.Items.Add(_validatedObjectsKey, new HashSet<object>());
        validationContext.Items.Add(_validateObjectOwnerKey, null);
        await TryValidateObjectRecursiveAsync(instance, validationContext, validationResults: null, validateAllProperties, predicate, throwOnFirstError: true, cancellationToken);
    }

    /// <summary>
    /// Internal iterator to enumerate all validation errors for the given object instance.
    /// </summary>
    /// <param name="instance">Object instance to test.</param>
    /// <param name="validationContext">Describes the object type.</param>
    /// <param name="validateAllProperties">if <c>true</c> also validates all properties.</param>
    /// <param name="breakOnFirstError">Whether to break on the first error or validate everything.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A collection of validation errors that result from validating the <paramref name="instance"/> with
    /// the given <paramref name="validationContext"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/> on <paramref name="validationContext"/>.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static async ValueTask<List<ValidationError>> GetObjectValidationErrorsAsync(
        object instance,
        ValidationContext validationContext,
        bool validateAllProperties,
        bool breakOnFirstError,
        CancellationToken cancellationToken)
    {
        if (validationContext == null)
        {
            throw new ArgumentNullException(nameof(validationContext));
        }

        Debug.Assert(instance != null);

        // Step 1: Validate the object properties' validation attributes
        List<ValidationError> errors = await GetObjectPropertyValidationErrorsAsync(instance!, validationContext, validateAllProperties, breakOnFirstError, cancellationToken);

        // We only proceed to Step 2 if there are no errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Step 2: Validate the object's validation attributes
        IEnumerable<ValidationAttribute> attributes = _store.GetTypeValidationAttributes(validationContext);
        errors.AddRange(await GetValidationErrorsAsync(instance, validationContext, attributes, breakOnFirstError, cancellationToken));

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

        // We only proceed to Step 4 if there are no errors
        if (errors.Count > 0)
        {
            return errors;
        }

        // Step 4: Test for IAsyncValidatableObject implementation
        if (instance is IAsyncValidatableObject asyncValidatable)
        {
            var results = asyncValidatable.ValidateAsync(validationContext, cancellationToken);

            if (results != null)
            {
                await foreach (ValidationResult result in results)
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
    /// <param name="validateAllProperties">If <c>true</c>, evaluates all the properties, otherwise just checks that
    /// ones marked with <see cref="RequiredAttribute"/> are not null.</param>
    /// <param name="breakOnFirstError">Whether to break on the first error or validate everything.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="ValidationError"/> instances.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(_validationInstanceTypeNotStaticallyDiscovered)]
#endif
    private static async ValueTask<List<ValidationError>> GetObjectPropertyValidationErrorsAsync(
        object instance,
        ValidationContext validationContext,
        bool validateAllProperties,
        bool breakOnFirstError,
        CancellationToken cancellationToken)
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
                errors.AddRange(await GetValidationErrorsAsync(property.Value, property.Key, attributes, breakOnFirstError, cancellationToken));
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
    /// Internal iterator to enumerate all validation errors for an value.
    /// </summary>
    /// <remarks>
    /// If a <see cref="RequiredAttribute"/> is found, it will be evaluated first, and if that fails,
    /// validation will abort, regardless of the <paramref name="breakOnFirstError"/> parameter value.
    /// </remarks>
    /// <param name="value">The value to pass to the validation attributes.</param>
    /// <param name="validationContext">Describes the type/member being evaluated.</param>
    /// <param name="attributes">The validation attributes to evaluate.</param>
    /// <param name="breakOnFirstError">Whether or not to break on the first validation failure.  A
    /// <see cref="RequiredAttribute"/> failure will always abort with that sole failure.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The collection of validation errors.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    private static async ValueTask<List<ValidationError>> GetValidationErrorsAsync(
        object? value,
        ValidationContext validationContext,
        IEnumerable<ValidationAttribute> attributes,
        bool breakOnFirstError,
        CancellationToken cancellationToken)
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
            var (success, error) = await TryValidateAsync(value, validationContext, required, cancellationToken);
            if (!success)
            {
                errors.Add(error!);
                return errors;
            }
        }

        // Iterate through the rest of the validators, skipping the required validator
        foreach (ValidationAttribute attr in attributes)
        {
            if (attr != required)
            {
                var (success, error) = await TryValidateAsync(value, validationContext, attr, cancellationToken);
                if (!success)
                {
                    errors.Add(error!);

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
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
    private static async ValueTask<bool> TryValidateObjectRecursiveAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError,
        CancellationToken cancellationToken = default)
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
            throw new ArgumentException("Validator_InstanceMustMatchValidationContextInstance", nameof(instance));
        }

        if (!(validationContext.Items.TryGetValue(_validatedObjectsKey, out var item)
            && item is HashSet<object> visited
            && visited.Add(instance)))
        {
            return true;
        }

        bool isValid = true;
        bool breakOnFirstError = (validationResults == null);

        foreach (ValidationError err in await GetObjectValidationErrorsAsync(instance, validationContext, validateAllProperties, breakOnFirstError, cancellationToken))
        {
            if (throwOnFirstError) err.ThrowValidationException();

            isValid = false;

            if (breakOnFirstError) break;

            TransferErrorToResult(validationResults!, err);
        }

        if (!isValid && breakOnFirstError) return isValid;

        var propertyObjectsAreValid = await TryValidatePropertyObjectsAsync(instance, validationContext, validationResults, validateAllProperties, predicate, throwOnFirstError, cancellationToken);

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
    /// If the type of object implemented multiple <see cref="IAsyncEnumerable{T}"/> interfaces,
    /// we only validate implicit <see cref="IAsyncEnumerable{T}"/> of <see cref="object"/> implementation.
    /// </para>
    /// <para>
    /// If the type of object implemented multiple <see cref="IEnumerable{T}"/>(or <see cref="IEnumerable"/>) interfaces,
    /// we only validate implicit <see cref="IEnumerable"/> implementation when it is not an implicit <see cref="IAsyncEnumerable{T}"/> of <see cref="object"/> implementation.
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
    /// <param name="validateAllProperties">If <c>true</c>, also evaluates all properties of the object (this process is not
    /// recursive over properties of the properties).</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <param name="throwOnFirstError">Whether to throw an exception when a validation error is detected.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the object is valid, <c>false</c> if any validation errors are encountered.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="instance"/> doesn't match the
    /// <see cref="ValidationContext.ObjectInstance"/>on <paramref name="validationContext"/>.</exception>
    private static async ValueTask<bool> TryValidatePropertyObjectsAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError,
        CancellationToken cancellationToken)
    {
        bool isValid = true;
        bool breakOnFirstError = (validationResults == null);

        List<KeyValuePair<ValidationContext, object>> propertyObjects = GetPropertyObjectValues(instance, validationContext, predicate);
        foreach (var propertyObject in propertyObjects)
        {
            var innerIsValid = await TryValidateObjectRecursiveAsync(
                propertyObject.Value,
                propertyObject.Key,
                validationResults,
                validateAllProperties,
                predicate,
                throwOnFirstError,
                cancellationToken);
            if (isValid && !innerIsValid) isValid = false;
            if (!isValid && breakOnFirstError) break;
        }

        if (!isValid && breakOnFirstError) return isValid;

        if (instance is IAsyncEnumerable<object?> asyncEnumerable)
        {
            var index = -1;
            await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
            {
                index++;
                var innerIsValid = await TryValidateAsyncEnumerableElementAsync(
                    instance,
                    validationContext,
                    validationResults,
                    validateAllProperties,
                    predicate,
                    throwOnFirstError,
                    index,
                    item,
                    cancellationToken);
                if (isValid && !innerIsValid) isValid = false;
                if (!isValid && breakOnFirstError) break;
            }
        }
        else if (instance is IEnumerable enumerable)
        {
            var index = -1;
            foreach (var item in enumerable)
            {
                index++;
                var innerIsValid = await TryValidateAsyncEnumerableElementAsync(
                    instance,
                    validationContext,
                    validationResults,
                    validateAllProperties,
                    predicate,
                    throwOnFirstError,
                    index,
                    item,
                    cancellationToken);
                if (isValid && !innerIsValid) isValid = false;
                if (!isValid && breakOnFirstError) break;
            }
        }

        return isValid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<bool> TryValidateAsyncEnumerableElementAsync(
        object instance,
        ValidationContext validationContext,
        ValidationResultStore? validationResults,
        bool validateAllProperties,
        Func<Type, bool>? predicate,
        bool throwOnFirstError,
        int index,
        object? element,
        CancellationToken cancellationToken)
    {
        if (element is null || !IsValidatableType(element.GetType(), predicate)) return true;

        return await TryValidateObjectRecursiveAsync(
            element,
            CreateValidationContext(
                element,
                validationContext,
                new FieldIdentifier(instance, index, (FieldIdentifier?)validationContext.Items[_validateObjectOwnerKey])),
            validationResults,
            validateAllProperties,
            predicate,
            throwOnFirstError,
            cancellationToken);
    }

    /// <summary>
    /// Tests whether a value is valid against a single <see cref="ValidationAttribute"/> using the <see cref="ValidationContext"/>.
    /// </summary>
    /// <param name="value">The value to be tested for validity.</param>
    /// <param name="validationContext">Describes the property member to validate.</param>
    /// <param name="attribute">The validation attribute to test.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="validationContext"/> is null.</exception>
    private static async ValueTask<(bool success, ValidationError? error)> TryValidateAsync(
        object? value,
        ValidationContext validationContext,
        ValidationAttribute attribute,
        CancellationToken cancellationToken)
    {
        Debug.Assert(validationContext != null);

        ValidationResult? validationResult;
        if (attribute is AsyncValidationAttribute asyncValidation)
        {
            validationResult = await asyncValidation.GetValidationResultAsync(value, validationContext!, cancellationToken);
        }
        else
        {
            validationResult = attribute.GetValidationResult(value, validationContext);
        }
        if (validationResult != ValidationResult.Success)
        {
            return (false, new ValidationError(attribute, value, validationResult!, validationContext!));
        }

        return (true, null);
    }
}
