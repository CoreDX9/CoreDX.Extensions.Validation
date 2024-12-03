using CoreDX.Extensions.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.Validation.Tests;

public class AsyncValidatorTests2
{
    [Fact]
    public static async Task CustomAsyncValidationTest()
    {
        var obj = new TestObject();

        List<ValidationResult> results = [];
        var isValid = await AsyncValidator.TryValidateObject(obj, new ValidationContext(obj), results, true);
        
        Assert.False(isValid);
        Assert.Equal(4, results.Count);

        var store = new ValidationResultStore();
        var isValid2 = ObjectGraphValidator.TryValidateObject(
            obj,
            new ValidationContext(obj),
            store,
            AsyncValidationBehavior.TrySynchronously,
            true,
            null);

        Assert.False(isValid2);
        Assert.Equal(4, store.First().Value.Count);
    }

    [Fact]
    public static async Task CustomAsyncValidationThrowIfArgumentInvalid()
    {
        var messageTemplate = "The CustomValidationAttribute method '{0}' in type '{1}' must match the expected signature: public static Task<ValidationResult>(or ValueTask<ValidationResult>) {0}(object value, ValidationContext context) or public static ValidationResult {0}(object value, ValidationContext context, CancellationToken cancellationToken).  The value can be strongly typed.  The ValidationContext parameter is optional.";
        
        var obj1 = new TestObject1();
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj1, new ValidationContext(obj1), [], true).AsTask());
        
        Assert.Equal(
            string.Format(messageTemplate, "Test1", "TestPropertyObject1"),
            exception1.Message);

        var obj2 = new TestObject2();
        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj2, new ValidationContext(obj2), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test2", "TestPropertyObject2"),
            exception2.Message);

        var obj3 = new TestObject3();
        var exception3 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj3, new ValidationContext(obj3), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test3", "TestPropertyObject3"),
            exception3.Message);

        var obj4 = new TestObject4();
        var exception4 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj4, new ValidationContext(obj4), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test4", "TestPropertyObject4"),
            exception4.Message);
    }

    public class TestObject
    {
        [CustomAsyncValidation(typeof(TestPropertyObject), nameof(TestPropertyObject.Test1))]
        [CustomAsyncValidation(typeof(TestPropertyObject), nameof(TestPropertyObject.Test2))]
        [CustomAsyncValidation(typeof(TestPropertyObject), nameof(TestPropertyObject.Test3))]
        [CustomAsyncValidation(typeof(TestPropertyObject), nameof(TestPropertyObject.Test4))]
        public TestPropertyObject TestProperty { get; set; }

        public class TestPropertyObject
        {
            public static async ValueTask<ValidationResult> Test1(string value)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test1));
            }

            public static async ValueTask<ValidationResult> Test2(string value, CancellationToken cancellation)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test2));
            }

            public static async Task<ValidationResult> Test3(string value, ValidationContext context)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test3));
            }

            public static async Task<ValidationResult> Test4(string value, ValidationContext context, CancellationToken cancellation)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test4));
            }
        }
    }

    public class TestObject1
    {
        [CustomAsyncValidation(typeof(TestPropertyObject1), nameof(TestPropertyObject1.Test1))]
        public TestPropertyObject1 TestProperty1 { get; set; }

        public class TestPropertyObject1
        {
            public static async ValueTask<ValidationResult> Test1(string value, object o)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test1));
            }
        }
    }

    public class TestObject2
    {
        [CustomAsyncValidation(typeof(TestPropertyObject2), nameof(TestPropertyObject2.Test2))]
        public TestPropertyObject2 TestProperty2 { get; set; }

        public class TestPropertyObject2
        {
            public static async ValueTask<ValidationResult> Test2(string value, CancellationToken cancellation, object o)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test2));
            }
        }
    }

    public class TestObject3
    {
        [CustomAsyncValidation(typeof(TestPropertyObject3), nameof(TestPropertyObject3.Test3))]
        public TestPropertyObject3 TestProperty3 { get; set; }

        public class TestPropertyObject3
        {
            public static async Task<ValidationResult> Test3(string value, object o, ValidationContext context)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test3));
            }
        }
    }

    public class TestObject4
    {
        [CustomAsyncValidation(typeof(TestPropertyObject4), nameof(TestPropertyObject4.Test4))]
        public TestPropertyObject4 TestProperty4 { get; set; }

        public class TestPropertyObject4
        {
            public static async Task<ValidationResult> Test4(string value, ValidationContext context, object o, CancellationToken cancellation)
            {
                await Task.Yield();
                return new ValidationResult(nameof(Test4));
            }
        }
    }
}
