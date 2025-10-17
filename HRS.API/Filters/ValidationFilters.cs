using FluentValidation;
using HRS.API.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRS.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());

            if (_serviceProvider.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(arg);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    var errors = result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                    var response = ApiResponse<object>.FailResponse("Validation failed", errors.ToList());

                    context.Result = new BadRequestObjectResult(response);
                    return;
                }
            }
        }

        await next();
    }
}
