using FluentValidation;
using Microsoft.Extensions.Localization;

namespace AlbertoBizzini.Web.Models;

public class ContactFormModel
{
    public string Name { get; set; }
    public string Email { get; set; }

    public string Message { get; set; }

    public bool PrivacyAccepted { get; set; }
}

public class ContactFormModelFluentValidator : AbstractValidator<ContactFormModel>
{
    public int NameMaxLength = 50;
    public int MessageMaxLength = 1000;

    public ContactFormModelFluentValidator(IStringLocalizer<ContactFormModel> l)
    {
        RuleFor(x => x.Name)
            .Length(1, NameMaxLength)
            .WithMessage(x => l["NameLengthValidationError", NameMaxLength]);

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(x => l["EmailRequiredValidationError"]);
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(x => l["EmailNotValidValidationError"]);

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage(x => l["MessageRequiredValidationError"]);
        RuleFor(x => x.Message)
            .Length(1, MessageMaxLength)
            .WithMessage(x => l["MessageLengthValidationError", MessageMaxLength]);

        RuleFor(x => x.PrivacyAccepted)
            .NotEmpty()
            .WithMessage(x => l["MustAcceptPrivacyValidationError"]);
        ;
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<ContactFormModel>.CreateWithOptions((ContactFormModel)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}