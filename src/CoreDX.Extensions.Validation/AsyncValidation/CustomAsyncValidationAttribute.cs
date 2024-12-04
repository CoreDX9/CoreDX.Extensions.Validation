// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Reflection;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
///     Validation attribute that executes a user-supplied method at runtime, using one of these signatures:
///     <para>
///         public static <see cref="ValidationResult" /> Method(object value) { ... }
///     </para>
///     <para>
///         public static <see cref="ValidationResult" /> Method(object value, <see cref="ValidationContext" /> context) {
///         ... }
///     </para>
///     <para>
///         The value can be strongly typed as type conversion will be attempted.
///     </para>
/// </summary>
/// <remarks>
///     This validation attribute is used to invoke custom logic to perform validation at runtime.
///     Like any other <see cref="ValidationAttribute" />, its <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" />
///     method is invoked to perform validation.  This implementation simply redirects that call to the method
///     identified by <see cref="Method" /> on a type identified by <see cref="ValidatorType" />
///     <para>
///         The supplied <see cref="ValidatorType" /> cannot be null, and it must be a public type.
///     </para>
///     <para>
///         The named <see cref="Method" /> must be public, static, return <see cref="ValidationResult" /> and take at
///         least one input parameter for the value to be validated.  This value parameter may be strongly typed.
///         Type conversion will be attempted if clients pass in a value of a different type.
///     </para>
///     <para>
///         The <see cref="Method" /> may also declare an additional parameter of type <see cref="ValidationContext" />.
///         The <see cref="ValidationContext" /> parameter provides additional context the method may use to determine
///         the context in which it is being used.
///     </para>
///     <para>
///         If the method returns <see cref="ValidationResult" />.<see cref="ValidationResult.Success" />, that indicates
///         the given value is acceptable and validation passed.
///         Returning an instance of <see cref="ValidationResult" /> indicates that the value is not acceptable
///         and validation failed.
///     </para>
///     <para>
///         If the method returns a <see cref="ValidationResult" /> with a <c>null</c>
///         <see cref="ValidationResult.ErrorMessage" />
///         then the normal <see cref="ValidationAttribute.FormatErrorMessage" /> method will be called to compose the
///         error message.
///     </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method |
    AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class CustomAsyncValidationAttribute : AsyncValidationAttribute
{
    private const string _parameterInvalidMessageTemplate = "The CustomValidationAttribute method '{0}' in type '{1}' must match the expected signature: public static Task<ValidationResult>(or ValueTask<ValidationResult>) {0}(object value, ValidationContext context, CancellationToken cancellationToken).  The value can be strongly typed.  The ValidationContext and CancellationToken parameter are optional.";

    #region Member Fields

    private readonly Lazy<string?> _malformedErrorMessage;
    private bool _isSingleArgumentMethod;
    private bool _methodHasCancellationTokenArgument;
    private string? _lastMessage;
    private MethodInfo? _methodInfo;
    private Type? _firstParameterType;
    private Tuple<string, Type>? _typeId;

    #endregion

    #region All Constructors

    /// <summary>
    ///     Instantiates a custom validation attribute that will invoke a method in the
    ///     specified type.
    /// </summary>
    /// <remarks>
    ///     An invalid <paramref name="validatorType" /> or <paramref name="method" /> will be cause
    ///     <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" />> to return a <see cref="ValidationResult" />
    ///     and <see cref="ValidationAttribute.FormatErrorMessage" /> to return a summary error message.
    /// </remarks>
    /// <param name="validatorType">
    ///     The type that will contain the method to invoke.  It cannot be null.  See
    ///     <see cref="Method" />.
    /// </param>
    /// <param name="method">The name of the method to invoke in <paramref name="validatorType" />.</param>
    public CustomAsyncValidationAttribute(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        Type validatorType, string method)
        : base(() => "{0} is not valid.")
    {
        ValidatorType = validatorType;
        Method = method;
        _malformedErrorMessage = new Lazy<string?>(CheckAttributeWellFormed);
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the type that contains the validation method identified by <see cref="Method" />.
    /// </summary>
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
    public Type ValidatorType { get; }

    /// <summary>
    /// Gets a unique identifier for this attribute.
    /// </summary>
    public override object TypeId => _typeId ??= new Tuple<string, Type>(Method, ValidatorType);

    /// <summary>
    ///     Gets the name of the method in <see cref="ValidatorType" /> to invoke to perform validation.
    /// </summary>
    public string Method { get; }
    
    /// <inheritdoc/>
    public override bool RequiresValidationContext
    {
        get
        {
            // If attribute is not valid, throw an exception right away to inform the developer
            ThrowIfAttributeNotWellFormed();
            // We should return true when 2-parameter form of the validation method is used
            return !_isSingleArgumentMethod;
        }
    }
    #endregion

    /// <summary>
    ///     Override of validation method.  See <see cref="ValidationAttribute.IsValid(object, ValidationContext)" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">
    ///     A <see cref="ValidationContext" /> instance that provides
    ///     context about the validation operation, such as the object and member being validated.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Whatever the <see cref="Method" /> in <see cref="ValidatorType" /> returns.</returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    protected override async ValueTask<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken = default)
    {
        // If attribute is not valid, throw an exception right away to inform the developer
        ThrowIfAttributeNotWellFormed();

        var methodInfo = _methodInfo;

        // If the value is not of the correct type and cannot be converted, fail
        // to indicate it is not acceptable.  The convention is that IsValid is merely a probe,
        // and clients are not expecting exceptions.
        object? convertedValue;
        if (!TryConvertValue(value, out convertedValue))
        {
            return new ValidationResult(string.Format(CultureInfo.CurrentCulture, "Could not convert the value of type '{0}' to '{1}' as expected by method {2}.{3}.",
                                        (value != null ? value.GetType().ToString() : "null"),
                                        _firstParameterType,
                                        ValidatorType,
                                        Method));
        }

        // Invoke the method.  Catch TargetInvocationException merely to unwrap it.
        // Callers don't know Reflection is being used and will not typically see
        // the real exception
        try
        {
            // 1-parameter form is Task(or ValueTask) of ValidationResult Method(object value) or ValidationResult Method(object value, CancellationToken cancellationToken)
            // 2-parameter form is Task(or ValueTask) of ValidationResult Method(object value, ValidationContext context) or Method(object value, ValidationContext context, CancellationToken cancellationToken),
            var methodParams = (_isSingleArgumentMethod, _methodHasCancellationTokenArgument) switch
            {
                (true, true) => new object?[] { convertedValue, cancellationToken },
                (true, false) => new[] { convertedValue },
                (false, true) => new[] { convertedValue, validationContext, cancellationToken },
                (false, false) => new[] { convertedValue, validationContext },
            };

            ValidationResult? result = null;

            var asyncResult = methodInfo!.Invoke(null, methodParams);
            if(asyncResult is Task<ValidationResult?> task)
            {
                result = await task;
            }
            else if (asyncResult is ValueTask<ValidationResult?> valueTask)
            {
                result = await valueTask;
            }

            // We capture the message they provide us only in the event of failure,
            // otherwise we use the normal message supplied via the ctor
            _lastMessage = null;

            if (result != null)
            {
                _lastMessage = result.ErrorMessage;
            }

            return result;
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException!;
        }
    }

    /// <summary>
    ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
    /// </summary>
    /// <param name="name">The name to include in the formatted string</param>
    /// <returns>A localized string to describe the problem.</returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
    public override string FormatErrorMessage(string name)
    {
        // If attribute is not valid, throw an exception right away to inform the developer
        ThrowIfAttributeNotWellFormed();

        if (!string.IsNullOrEmpty(_lastMessage))
        {
            return string.Format(CultureInfo.CurrentCulture, _lastMessage, name);
        }

        // If success or they supplied no custom message, use normal base class behavior
        return base.FormatErrorMessage(name);
    }

    /// <summary>
    ///     Checks whether the current attribute instance itself is valid for use.
    /// </summary>
    /// <returns>The error message why it is not well-formed, null if it is well-formed.</returns>
    private string? CheckAttributeWellFormed() => ValidateValidatorTypeParameter() ?? ValidateMethodParameter();

    /// <summary>
    ///     Internal helper to determine whether <see cref="ValidatorType" /> is legal for use.
    /// </summary>
    /// <returns><c>null</c> or the appropriate error message.</returns>
    private string? ValidateValidatorTypeParameter()
    {
        if (ValidatorType == null)
        {
            return "The CustomValidationAttribute.ValidatorType was not specified.";
        }

        if (!ValidatorType.IsVisible)
        {
            return string.Format(CultureInfo.CurrentCulture, "The custom validation type '{0}' must be public.", ValidatorType.Name);
        }

        return null;
    }

    /// <summary>
    ///     Internal helper to determine whether <see cref="Method" /> is legal for use.
    /// </summary>
    /// <returns><c>null</c> or the appropriate error message.</returns>
    private string? ValidateMethodParameter()
    {
        if (string.IsNullOrEmpty(Method))
        {
            return "The CustomValidationAttribute.Method was not specified.";
        }

        // Named method must be public and static
        var methodInfo = ValidatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(m => string.Equals(m.Name, Method, StringComparison.Ordinal));
        if (methodInfo == null)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "The CustomValidationAttribute method '{0}' does not exist in type '{1}' or is not public and static.",
                Method,
                ValidatorType.Name);
        }

        // Method must return a Task<ValidationResult>(or ValueTask<ValidationResult>) or derived class
        if (!typeof(Task<ValidationResult>).IsAssignableFrom(methodInfo.ReturnType)
            && !typeof(ValueTask<ValidationResult>).IsAssignableFrom(methodInfo.ReturnType))
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "The CustomValidationAttribute method '{0}' in type '{1}' must return Task(or ValueTask) of System.ComponentModel.DataAnnotations.ValidationResult.  Use System.ComponentModel.DataAnnotations.ValidationResult.Success to represent success.",
                Method,
                ValidatorType.Name);
        }

        ParameterInfo[] parameterInfos = methodInfo.GetParameters();

        // Must declare at least one input parameter for the value and it cannot be ByRef
        if (parameterInfos.Length == 0 || parameterInfos[0].ParameterType.IsByRef)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                _parameterInvalidMessageTemplate,
                Method,
                ValidatorType.Name);
        }

        _methodHasCancellationTokenArgument = parameterInfos[parameterInfos.Length - 1].ParameterType == typeof(CancellationToken);

        // We accept 2 forms:
        // 1-parameter form is Task(or ValueTask) ValidationResult Method(object value) or ValidationResult Method(object value, CancellationToken cancellationToken)
        // 2-parameter form is Task(or ValueTask) ValidationResult Method(object value, ValidationContext context) or ValidationResult Method(object value, ValidationContext context, CancellationToken cancellationToken),
        _isSingleArgumentMethod = (parameterInfos.Length == 1 && !_methodHasCancellationTokenArgument)
            || (parameterInfos.Length == 2 && _methodHasCancellationTokenArgument);

        if (parameterInfos.Length - (_methodHasCancellationTokenArgument ? 1 : 0) - (_isSingleArgumentMethod ? 1 : 2) != 0
            || (!_isSingleArgumentMethod && parameterInfos[1].ParameterType != typeof(ValidationContext)))
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                _parameterInvalidMessageTemplate,
                Method,
                ValidatorType.Name);
        }

        _methodInfo = methodInfo;
        _firstParameterType = parameterInfos[0].ParameterType;
        return null;
    }

    /// <summary>
    ///     Throws InvalidOperationException if the attribute is not valid.
    /// </summary>
    private void ThrowIfAttributeNotWellFormed()
    {
        string? errorMessage = _malformedErrorMessage.Value;
        if (errorMessage != null)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    ///     Attempts to convert the given value to the type needed to invoke the method for the current
    ///     CustomValidationAttribute.
    /// </summary>
    /// <param name="value">The value to check/convert.</param>
    /// <param name="convertedValue">If successful, the converted (or copied) value.</param>
    /// <returns><c>true</c> if type value was already correct or was successfully converted.</returns>
    private bool TryConvertValue(object? value, out object? convertedValue)
    {
        convertedValue = null;
        var expectedValueType = _firstParameterType!;

        // Null is permitted for reference types or for Nullable<>'s only
        if (value == null)
        {
            if (expectedValueType.IsValueType
                && (!expectedValueType.IsGenericType
                    || expectedValueType.GetGenericTypeDefinition() != typeof(Nullable<>)))
            {
                return false;
            }

            return true; // convertedValue already null, which is correct for this case
        }

        // If the type is already legally assignable, we're good
        if (expectedValueType.IsInstanceOfType(value))
        {
            convertedValue = value;
            return true;
        }

        // Value is not the right type -- attempt a convert.
        // Any expected exception returns a false
        try
        {
            convertedValue = Convert.ChangeType(value, expectedValueType, CultureInfo.CurrentCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
