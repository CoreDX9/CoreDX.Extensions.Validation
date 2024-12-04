using CoreDX.Extensions.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.Validation.Tests;

public class CustomAsyncValidationAttributeTests
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
        var messageTemplate = "The CustomValidationAttribute method '{0}' in type '{1}' must match the expected signature: public static Task<ValidationResult>(or ValueTask<ValidationResult>) {0}(object value, ValidationContext context, CancellationToken cancellationToken).  The value can be strongly typed.  The ValidationContext and CancellationToken parameter are optional.";
        
        var obj1 = new TestObject1();
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj1, new ValidationContext(obj1), [], true).AsTask());
        
        Assert.Equal(
            string.Format(messageTemplate, "Test1", "TestObject1"),
            exception1.Message);

        var obj2 = new TestObject2();
        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj2, new ValidationContext(obj2), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test2", "TestObject2"),
            exception2.Message);

        var obj3 = new TestObject3();
        var exception3 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj3, new ValidationContext(obj3), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test3", "TestObject3"),
            exception3.Message);

        var obj4 = new TestObject4();
        var exception4 = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncValidator.TryValidateObject(obj4, new ValidationContext(obj4), [], true).AsTask());

        Assert.Equal(
            string.Format(messageTemplate, "Test4", "TestObject4"),
            exception4.Message);
    }

    public class TestObject
    {
        [CustomAsyncValidation(typeof(TestObject), nameof(Test1))]
        [CustomAsyncValidation(typeof(TestObject), nameof(Test2))]
        [CustomAsyncValidation(typeof(TestObject), nameof(Test3))]
        [CustomAsyncValidation(typeof(TestObject), nameof(Test4))]
        public string TestProperty { get; set; }

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

    public class TestObject1
    {
        [CustomAsyncValidation(typeof(TestObject1), nameof(Test1))]
        public string TestProperty1 { get; set; }

        public static async ValueTask<ValidationResult> Test1(string value, object o)
        {
            await Task.Yield();
            return new ValidationResult(nameof(Test1));
        }
    }

    public class TestObject2
    {
        [CustomAsyncValidation(typeof(TestObject2), nameof(Test2))]
        public string TestProperty2 { get; set; }

        public static async ValueTask<ValidationResult> Test2(string value, CancellationToken cancellation, object o)
        {
            await Task.Yield();
            return new ValidationResult(nameof(Test2));
        }
    }

    public class TestObject3
    {
        [CustomAsyncValidation(typeof(TestObject3), nameof(Test3))]
        public string TestProperty3 { get; set; }

        public static async Task<ValidationResult> Test3(string value, object o, ValidationContext context)
        {
            await Task.Yield();
            return new ValidationResult(nameof(Test3));
        }
    }

    public class TestObject4
    {
        [CustomAsyncValidation(typeof(TestObject4), nameof(Test4))]
        public string TestProperty4 { get; set; }

        public static async Task<ValidationResult> Test4(string value, ValidationContext context, object o, CancellationToken cancellation)
        {
            await Task.Yield();
            return new ValidationResult(nameof(Test4));
        }
    }
}
