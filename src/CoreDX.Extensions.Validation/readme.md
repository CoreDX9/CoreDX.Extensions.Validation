## About
Async validation and object graph validation extensions.

## How to Use
``` csharp
public class AsyncFailValidationAttribute : AsyncValidationAttribute
{
    public override ValueTask<bool> IsValidAsync(object? value, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(false);
    }
}

public class CustomObjcet
{
    [AsyncFailValidation]
    public string? TestProp { get; set; }
}

var objectToBeValidated = new CustomObjcet();
var validationContext = new ValidationContext(objectToBeValidated);
await AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true);

var validationResults = new List<ValidationResult>();
bool isValid = await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true);

var objectGraphValidationContext = new ValidationContext(objectToBeValidated);
ObjectGraphValidator.ValidateObject(objectToBeValidated, objectGraphValidationContext, AsyncValidationBehavior.TrySynchronously, true);

var objectGraphValidationContext2 = new ValidationContext(objectToBeValidated);
await ObjectGraphValidator.ValidateObjectAsync(objectToBeValidated, objectGraphValidationContext2, true);

var objectGraphValidationContext3 = new ValidationContext(objectToBeValidated);
var validationResultStore = new ValidationResultStore();
bool isValid = await ObjectGraphValidator.TryValidateObjectAsync(objectToBeValidated, objectGraphValidationContext3, validationResultStore, true);
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.ComponentModel.DataAnnotations.AsyncValidationAttribute`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.CustomAsyncValidationAttribute`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.IAsyncValidatableObject`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.AsyncValidator`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.ObjectGraphValidator`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.ValidationResultStore`