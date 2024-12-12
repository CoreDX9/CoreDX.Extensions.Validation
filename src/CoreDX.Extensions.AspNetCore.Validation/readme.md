## About
Add async model validation services for Microsoft Asp.Net Core MVC and Minimal-APIs.

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

3. Minimal-APIs's data annotations localization (only for .NET 7.0+)
``` csharp
// Add a localization services like this (package: CoreDX.Extensions.Localization.EntityFrameworkCore)
services
    .AddEntityFrameworkCoreLocalization<ApplicationDbContext,ApplicationLocalizationRecord>(static options =>
    {
        options.ResourcesPath = "Resources";
        options.CreateLocalizationResourcesIfNotExist = true;
    });

// Or use localization services from MVC
services
    .AddMvc()
    .AddDataAnnotationsLocalization();

// Then add Minimal-APIs's data annotations localization services
services.AddEndpointParameterDataAnnotationsLocalization();
```

4. Minimal-APIs's data annotations validation (only for .NET 7.0+)
``` csharp
public class CustomModel
{
    [Required]
    [StringLength(42)]
    public string Text1 { get; set; } = null!;

    [MyCustomAsyncValidation]
    public string? Text2 { get; set; }

    [Required]
    public InnerModel ComplexProperty { get; set; } = null!;

    public class InnerModel
    {
        [Range(5, 10)]
        public int Number { get; set; }
    }
}

app.UseEndpoints(endpoints =>
{
    endpoints
        .MapPost(
            "/api/test",
            static async Task<Results<Ok, ValidationProblem>> (
                [FromBody, Required] CustomModel input,
                [FromQuery, Range(1, 100)] int someNumber,
                HttpContext httpContext) =>
            {
                // Gets validation results from filter if called 'AddEndpointParameterDataAnnotations()'.
                var results = httpContext.GetEndpointParameterDataAnnotationsValidationResults();
            
                input.Text1 = "new string value";
                someNumber = 107;
            
                // Try to revalidate and update validation results.
                var validationProcessSuccess = await httpContext.TryValidateEndpointParameters([
                    new KeyValuePair<string, object?>(nameof(input), input),
                    new KeyValuePair<string, object?>(nameof(someNumber), someNumber),
                ]);
            
                // Gets validation problem details.
                // If is added Minimal-APIs's localization services, will use localized error message.
                var problemDetails = httpContext.GetEndpointParameterDataAnnotationsProblemDetails();

                if (problemDetails?.Any() is true)
                {
                    return TypedResults.ValidationProblem(problemDetails);
                }

                return TypedResults.Ok();
            }
        )
        // Adds parameter data annotations validation filter to endpoint.
        .AddEndpointParameterDataAnnotations()
        // If paramater has validation error, will return validation problem automatically.
        .AddValidationProblemResult();
}
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.AsyncParamterBinder`
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.IAsyncObjectModelValidator`

## Related Packages
* `CoreDX.Extensions.Validation`