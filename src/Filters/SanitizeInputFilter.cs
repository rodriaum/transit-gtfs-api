using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace Tranzor.Filters;

public class SanitizeInputFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parameter in context.ActionArguments)
        {
            if (parameter.Value is string stringValue)
            {
                var sanitized = SanitizeInput(stringValue);
                context.ActionArguments[parameter.Key] = sanitized;
            }
        }
    }

    private string SanitizeInput(string input)
    {
        // Remove scripts
        input = Regex.Replace(input, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Remove HTML tags
        input = Regex.Replace(input, @"<[^>]*>", "");
        
        // Remove SQL injection attempts
        input = Regex.Replace(input, @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|ALTER|EXEC|DECLARE)\b)", "", RegexOptions.IgnoreCase);
        
        return input;
    }
}