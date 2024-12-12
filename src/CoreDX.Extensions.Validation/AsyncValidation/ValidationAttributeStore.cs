// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Reflection;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

/// <summary>
///     Cache of <see cref="ValidationAttribute" />s
/// </summary>
/// <remarks>
///     This class serves as a cache of validation attributes and [Display] attributes.
///     It exists both to help performance as well as to abstract away the differences between
///     Reflection and TypeDescriptor.
/// </remarks>
public sealed class ValidationAttributeStore
{
    private readonly ConcurrentDictionary<Type, TypeStoreItem> _typeStoreItems = new();

    /// <summary>
    ///     Gets the singleton <see cref="ValidationAttributeStore" />
    /// </summary>
    public static ValidationAttributeStore Instance { get; } = new ValidationAttributeStore();

    /// <summary>
    ///     Retrieves the type level validation attributes for the given type.
    /// </summary>
    /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
    /// <returns>The collection of validation attributes.  It could be empty.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public IEnumerable<ValidationAttribute> GetTypeValidationAttributes(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var item = GetTypeStoreItem(validationContext.ObjectType);
        return item.ValidationAttributes;
    }

    /// <summary>
    ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given type.  It may be null.
    /// </summary>
    /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
    /// <returns>The display attribute instance, if present.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public DisplayAttribute? GetTypeDisplayAttribute(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var item = GetTypeStoreItem(validationContext.ObjectType);
        return item.DisplayAttribute;
    }

    /// <summary>
    ///     Retrieves the <see cref="DisplayNameAttribute" /> associated with the given type.  It may be null.
    /// </summary>
    /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
    /// <returns>The display attribute instance, if present.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public DisplayNameAttribute? GetTypeDisplayNameAttribute(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var item = GetTypeStoreItem(validationContext.ObjectType);
        return item.DisplayNameAttribute;
    }

    /// <summary>
    ///     Retrieves the set of validation attributes for the property
    /// </summary>
    /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
    /// <returns>The collection of validation attributes.  It could be empty.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var typeItem = GetTypeStoreItem(validationContext.ObjectType);
        var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
        return item.ValidationAttributes;
    }

    /// <summary>
    ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given property
    /// </summary>
    /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
    /// <returns>The display attribute instance, if present.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public DisplayAttribute? GetPropertyDisplayAttribute(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var typeItem = GetTypeStoreItem(validationContext.ObjectType);
        var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
        return item.DisplayAttribute;
    }

    /// <summary>
    ///     Retrieves the <see cref="DisplayNameAttribute" /> associated with the given property
    /// </summary>
    /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
    /// <returns>The display attribute instance, if present.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public DisplayNameAttribute? GetPropertyDisplayNameAttribute(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var typeItem = GetTypeStoreItem(validationContext.ObjectType);
        var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
        return item.DisplayNameAttribute;
    }

    /// <summary>
    ///     Retrieves the Type of the given property.
    /// </summary>
    /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
    /// <returns>The type of the specified property</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public Type GetPropertyType(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var typeItem = GetTypeStoreItem(validationContext.ObjectType);
        var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
        return item.PropertyType;
    }

    /// <summary>
    ///     Determines whether or not a given <see cref="ValidationContext" />'s
    ///     <see cref="ValidationContext.MemberName" /> references a property on
    ///     the <see cref="ValidationContext.ObjectType" />.
    /// </summary>
    /// <param name="validationContext">The <see cref="ValidationContext" /> to check.</param>
    /// <returns><c>true</c> when the <paramref name="validationContext" /> represents a property, <c>false</c> otherwise.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(AsyncValidator._validationContextInstanceTypeNotStaticallyDiscovered)]
#endif
    public bool IsPropertyContext(ValidationContext validationContext)
    {
        EnsureValidationContext(validationContext);
        var typeItem = GetTypeStoreItem(validationContext.ObjectType);
        return typeItem.TryGetPropertyStoreItem(validationContext.MemberName!, out _);
    }

    /// <summary>
    ///     Retrieves or creates the store item for the given type
    /// </summary>
    /// <param name="type">The type whose store item is needed.  It cannot be null</param>
    /// <returns>The type store item.  It will not be null.</returns>
    private TypeStoreItem GetTypeStoreItem(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(TypeStoreItem.DynamicallyAccessedTypes)]
#endif
    Type type)
    {
        Debug.Assert(type != null);

        return _typeStoreItems.GetOrAdd(type!, AddTypeStoreItem);

#if NET6_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "The parameter in the parent method has already been marked DynamicallyAccessedMemberTypes.All.")]
#endif
        static TypeStoreItem AddTypeStoreItem(Type type)
        {
            AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
            return new TypeStoreItem(type, attributes);
        }
    }

    /// <summary>
    ///     Throws an ArgumentException of the validation context is null
    /// </summary>
    /// <param name="validationContext">The context to check</param>
    private static void EnsureValidationContext(ValidationContext validationContext)
    {
        if (validationContext is null) throw new ArgumentNullException(nameof(validationContext));
    }

    internal static bool IsPublic(PropertyInfo p) =>
        (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic);

    /// <summary>
    ///     Private abstract class for all store items
    /// </summary>
    private abstract class StoreItem
    {
        internal StoreItem(AttributeCollection attributes)
        {
            ValidationAttributes = attributes.OfType<ValidationAttribute>();
            DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault();
            DisplayNameAttribute = attributes.OfType<DisplayNameAttribute>().SingleOrDefault();
        }

        internal IEnumerable<ValidationAttribute> ValidationAttributes { get; }

        internal DisplayAttribute? DisplayAttribute { get; }

        internal DisplayNameAttribute? DisplayNameAttribute { get; }
    }

    /// <summary>
    ///     Private class to store data associated with a type
    /// </summary>
    private sealed class TypeStoreItem : StoreItem
    {
#if NET6_0_OR_GREATER
        internal const DynamicallyAccessedMemberTypes DynamicallyAccessedTypes = DynamicallyAccessedMemberTypes.All;
        internal const string _typesPropertiesCannotBeStaticallyDiscovered = "The Types of _type's properties cannot be statically discovered.";
#endif

#if NET9_0_OR_GREATER
        private readonly Lock _syncRoot = new Lock();
#else
        private readonly object _syncRoot = new object();
#endif

#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedTypes)]
#endif
        private readonly Type _type;
        private Dictionary<string, PropertyStoreItem>? _propertyStoreItems;

        internal TypeStoreItem(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedTypes)]
#endif
            Type type, AttributeCollection attributes)
            : base(attributes)
        {
            _type = type;
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode(_typesPropertiesCannotBeStaticallyDiscovered)]
#endif
        internal PropertyStoreItem GetPropertyStoreItem(string propertyName)
        {
            if (!TryGetPropertyStoreItem(propertyName, out PropertyStoreItem? item))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "The type '{0}' does not contain a public property named '{1}'.", _type.Name, propertyName), nameof(propertyName));
            }

            return item!;
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode(_typesPropertiesCannotBeStaticallyDiscovered)]
#endif
        internal bool TryGetPropertyStoreItem(
            string propertyName,
#if NET6_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out PropertyStoreItem? item)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (_propertyStoreItems == null)
            {
                lock (_syncRoot)
                {
                    _propertyStoreItems ??= CreatePropertyStoreItems();
                }
            }

            return _propertyStoreItems.TryGetValue(propertyName, out item);
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode(_typesPropertiesCannotBeStaticallyDiscovered)]
#endif
        private Dictionary<string, PropertyStoreItem> CreatePropertyStoreItems()
        {
            var propertyStoreItems = new Dictionary<string, PropertyStoreItem>();

            var properties = TypeDescriptor.GetProperties(_type);
            foreach (PropertyDescriptor property in properties)
            {
                var item = new PropertyStoreItem(property.PropertyType, GetExplicitAttributes(property));
                propertyStoreItems[property.Name] = item;
            }

            return propertyStoreItems;
        }

        /// <summary>
        ///     Method to extract only the explicitly specified attributes from a <see cref="PropertyDescriptor"/>
        /// </summary>
        /// <remarks>
        ///     Normal TypeDescriptor semantics are to inherit the attributes of a property's type.  This method
        ///     exists to suppress those inherited attributes.
        /// </remarks>
        /// <param name="propertyDescriptor">The property descriptor whose attributes are needed.</param>
        /// <returns>A new <see cref="AttributeCollection"/> stripped of any attributes from the property's type.</returns>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("The Type of propertyDescriptor.PropertyType cannot be statically discovered.")]
#endif
        private static AttributeCollection GetExplicitAttributes(PropertyDescriptor propertyDescriptor)
        {
            AttributeCollection propertyDescriptorAttributes = propertyDescriptor.Attributes;
            List<Attribute> attributes = new List<Attribute>(propertyDescriptorAttributes.Count);
            foreach (Attribute attribute in propertyDescriptorAttributes)
            {
                attributes.Add(attribute);
            }

            AttributeCollection typeAttributes = TypeDescriptor.GetAttributes(propertyDescriptor.PropertyType);
            bool removedAttribute = false;
            foreach (Attribute attr in typeAttributes)
            {
                for (int i = attributes.Count - 1; i >= 0; --i)
                {
                    // We must use ReferenceEquals since attributes could Match if they are the same.
                    // Only ReferenceEquals will catch actual duplications.
                    if (object.ReferenceEquals(attr, attributes[i]))
                    {
                        attributes.RemoveAt(i);
                        removedAttribute = true;
                    }
                }
            }
            return removedAttribute ? new AttributeCollection(attributes.ToArray()) : propertyDescriptorAttributes;
        }
    }

    /// <summary>
    ///     Private class to store data associated with a property
    /// </summary>
    private sealed class PropertyStoreItem : StoreItem
    {
        internal PropertyStoreItem(Type propertyType, AttributeCollection attributes)
            : base(attributes)
        {
            Debug.Assert(propertyType != null);
            PropertyType = propertyType!;
        }

        internal Type PropertyType { get; }
    }
}
