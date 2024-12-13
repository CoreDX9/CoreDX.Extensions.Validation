## About
Add async model validation services for Microsoft Asp.Net Core MVC and Minimal-APIs.

## How to Use

1. ServiceCollection
``` csharp
using Microsoft.AspNetCore.Mvc;

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
using Microsoft.AspNetCore.Http;

// Add a localization services like this (package: CoreDX.Extensions.Localization.EntityFrameworkCore)
services
    .AddEntityFrameworkCoreLocalization<ApplicationDbContext, ApplicationLocalizationRecord>(static options =>
    {
        options.ResourcesPath = "Resources";
        options.CreateLocalizationResourcesIfNotExist = true;
    });

// Or use localization services from MVC
services
    .AddMvc()
    .AddDataAnnotationsLocalization();

// Then add Minimal-APIs's data annotations localization services
services.AddEndpointParameterDataAnnotationsLocalization(static options =>
{
    // Add custom validation attribute's localization arguments adapter.
    options.Adapters.Add(new MyCustomAsyncAttributeAdapter());
});
```

4. Minimal-APIs's data annotations validation (only for .NET 7.0+)
``` csharp
using Microsoft.AspNetCore.Http;

public class MyCustomAsyncAttribute : AsyncValidationAttribute
{
    public string MyArgument1 { get; set; } = null!;

    public string MyArgument2 { get; set; } = null!;

    public string MyArgument3 { get; set; } = null!;

    public override ValueTask<bool> IsValidAsync(object? value, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(false);
    }
}

public class MyCustomAsyncAttributeAdapter : AttributeAdapterBase<MyCustomAsyncAttribute>
{
    protected override object[]? GetLocalizationArgumentsInternal(MyCustomAsyncAttribute attribute)
    {
        return [attribute.MyArgument1, attribute.MyArgument2, attribute.MyArgument3];
    }
}

public class CustomModel
{
    [Display(Name = nameof(Text1))]
    [Required]
    [StringLength(42)]
    public string Text1 { get; set; } = null!;

    [MyCustomAsync(MyArgument1 = "val1", MyArgument2 = "val2", MyArgument3 = "val3", ErrorMessage = "The field {0} must valid {1} and {2} and {3}.")]
    public string? Text2 { get; set; }

    [Required]
    public InnerModel ComplexProperty { get; set; } = null!;

    public class InnerModel
    {
        [Display(Name = nameof(Number))]
        [Range(5, 10, ErrorMessage = "The field {0} must be between {1} and {2}.")]
        public int Number { get; set; }
    }
}

app.UseEndpoints(endpoints =>
{
    endpoints
        .MapPost(
            "/api/test",
            static async Task<Results<Ok, ValidationProblem>> (
                [FromBody, Display(Name = nameof(input)), Required] CustomModel input,
                [FromQuery, Display(Name = nameof(someNumber)), Range(1, 100, ErrorMessage = "The argument {0} must be between {1} and {2}.")] int someNumber,
                HttpContext httpContext) =>
            {
                // Get validation results from filter if called 'AddEndpointParameterDataAnnotations()'.
                var results = httpContext.GetEndpointParameterDataAnnotationsValidationResults();
            
                input.Text1 = "new string value";
                someNumber = 107;
            
                // Try to revalidate and update validation results.
                var validationProcessSuccess = await httpContext.TryValidateEndpointParametersAsync([
                    new KeyValuePair<string, object?>(nameof(input), input),
                    new KeyValuePair<string, object?>(nameof(someNumber), someNumber),
                ]);
            
                // Gets validation problem details.
                // If is added Minimal-APIs's localization services, will use localized error messages.
                var problemDetails = httpContext.GetEndpointParameterDataAnnotationsProblemDetails();

                if (problemDetails?.Any() is true)
                {
                    return TypedResults.ValidationProblem(problemDetails);
                }

                return TypedResults.Ok();
            }
        )
        // Add parameter data annotations validation filter to endpoint.
        .AddEndpointParameterDataAnnotations()
        // If paramater has validation error, will return validation problem automatically.
        .AddValidationProblemResult();
}
```

## Main Types
The main types provided by this library are:
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.AsyncParamterBinder`
* `CoreDX.Extensions.AspNetCore.Mvc.ModelBinding.Validation.IAsyncObjectModelValidator`
* `CoreDX.Extensions.AspNetCore.Http.Validation.Localization.AttributeAdapterBase<TAttribute>`

## Related Packages
* `CoreDX.Extensions.Validation`