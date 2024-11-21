## About
Add async model validation services for Microsoft Asp.Net Core MVC.

## How to Use
``` csharp
services
    .AddMvc()
    .AddAsyncValidation();
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.AsyncParamterBinder`
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.IAsyncObjectModelValidator`

## Related Packages
* `CoreDX.Extensions.Validation`