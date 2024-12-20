using System.Collections;
using System.ComponentModel;

namespace CoreDX.Extensions.ComponentModel.DataAnnotations;

public static partial class ObjectGraphValidator
{
    private static readonly Type[] _genericEnumerableTypes = [typeof(IEnumerable<>), typeof(IAsyncEnumerable<>)];

    /// <summary>
    /// <para>Check if the validatable target exists in the object type graph.</para>
    /// <para>
    /// Validatable target means that the target type or property has <see cref="ValidationAttribute"/>,
    /// or implements <see cref="IValidatableObject"/> or <see cref="IAsyncValidatableObject"/>.
    /// </para>
    /// </summary>
    /// <param name="type">The type to be checked.</param>
    /// <param name="enumerableAsValidatableTarget">Should non generic enumerable interface be considered as validatable target.</param>
    /// <param name="predicate">A predicate to filter whether to recursively validate the properties of this type.</param>
    /// <returns>If there is any validatable target in the object type graph, return <see langword="true"/>; otherwise return <see langword="false"/>.</returns>
    /// <remarks>For generic enumerable interfaces, only check the first implementation.</remarks>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool HasValidatableTarget(
        Type type,
        bool enumerableAsValidatableTarget = false,
        Func<Type, bool>? predicate = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return HasValidatableTargetRecursive(type, enumerableAsValidatableTarget, predicate, []);
    }

    private static bool HasValidatableTargetRecursive(
        Type type,
        bool enumerableAsValidatableTarget,
        Func<Type, bool>? predicate,
        HashSet<Type> visited)
    {
        if (!IsValidatableType(type, predicate)) return false;
        if (!visited.Add(type)) return false;

        if (typeof(IValidatableObject).IsAssignableFrom(type)) return true;
        if (typeof(IAsyncValidatableObject).IsAssignableFrom(type)) return true;

        if (_store.GetTypeValidationAttributes(type).Any()) return true;

        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);

        foreach (PropertyDescriptor property in properties)
        {
            var attrs = _store.GetPropertyValidationAttributes(type, property.Name);

            if (attrs.Any()) return true;

            if (HasValidatableTargetRecursive(property.PropertyType, enumerableAsValidatableTarget, predicate, visited)) return true;
        }

        var enumerableElementTypeIsValidatableTarget = false;
        if (enumerableAsValidatableTarget)
        {
            enumerableElementTypeIsValidatableTarget = typeof(IEnumerable).IsAssignableFrom(type);
        }

        if (!enumerableElementTypeIsValidatableTarget)
        {
            var elementType = type.GetInterfaces()
                .Select(GenericEnumerableElementType)
                .Where(static t => t is not null)
                .FirstOrDefault();

            if (elementType is not null)
            {
                if (HasValidatableTargetRecursive(elementType, enumerableAsValidatableTarget, predicate, visited)) return true;
            }
        }

        return false;

        static Type? GenericEnumerableElementType(Type test)
        {
            var genericTest = test.IsGenericType ? test.GetGenericTypeDefinition() : test;
            if (_genericEnumerableTypes.Contains(genericTest))
            {
                return genericTest.GenericTypeArguments[0];
            }

            return null;
        }
    }
}
