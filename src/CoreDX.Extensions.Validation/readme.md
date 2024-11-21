## About
Async validation and object graph validation extensions.

## How to Use
``` csharp
var objectToBeValidated = new CustomObjcet();
var validationContext = new ValidationContext(objectToBeValidated);
await AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true);

var validationResults = new List<ValidationResult>();
bool isValid = await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true);

var objectGraphValidationContext = new ValidationContext(objectToBeValidated);
await ObjectGraphValidator.ValidateObjectAsync(objectToBeValidated, objectGraphValidationContext, true);

var objectGraphValidationContext2 = new ValidationContext(objectToBeValidated);
var validationResultStore = new ValidationResultStore();
bool isValid = await ObjectGraphValidator.TryValidateObjectAsync(objectToBeValidated, objectGraphValidationContext2, validationResultStore, true);
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.ComponentModel.DataAnnotations.AsyncValidationAttribute`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.IAsyncValidatableObject`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.AsyncValidator`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.ObjectGraphValidator`
* `CoreDX.Extensions.ComponentModel.DataAnnotations.ValidationResultStore`