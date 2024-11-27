## About
Add async model validation services for Microsoft Asp.Net Core MVC.

## How to Use

1. ServiceCollection
``` csharp
services
    .AddMvc()
    .AddAsyncDataAnnotations();

// Or
services
    .AddMvcCore()
    .AddDataAnnotations()
    .AddAsyncDataAnnotations();
```

2. MVC and Razor pages
``` csharp
using Microsoft.AspNetCore.Mvc;

public class MyController : ControllerBase
{
    public async Task Post(CustomModel input)
    {
        // TryValidateModel(input) // This will ignore async validation attributes.
        await this.TryValidateModelAsync(input);
    }
}

public class MyModel : PageModel
{
    public CustomModel Input { get; set; }

    public async Task OnPostAsync()
    {
        // TryValidateModel(Input) // This will ignore async validation attributes.
        await this.TryValidateModelAsync(Input);
    }
}
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.AsyncParamterBinder`
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.IAsyncObjectModelValidator`

## Related Packages
* `CoreDX.Extensions.Validation`