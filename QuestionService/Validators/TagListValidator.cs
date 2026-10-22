using System.ComponentModel.DataAnnotations;

namespace QuestionService.Validators;

public class TagListValidator(int minTags, int maxTags) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is List<string> tags)
        {
            if (tags.Count >= minTags && tags.Count <= maxTags)
            {
                return ValidationResult.Success;
            }
        }
        
        return new ValidationResult($"You must provide at least {minTags} and max {maxTags}");
    }
}