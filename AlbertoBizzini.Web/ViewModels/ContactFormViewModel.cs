using FluentValidation;
using Microsoft.Extensions.Localization;

namespace AlbertoBizzini.Web.ViewModels;

public class ContactFormViewModel
{
    public string Email { get; set; }

    public string Message { get; set; }

    public bool PrivacyAccepted { get; set; }
}

public class ContactFormViewModelFluentValidator : AbstractValidator<ContactFormViewModel>
{
    public static int MessageMaxLength = 10;


    public ContactFormViewModelFluentValidator(IStringLocalizer<ContactFormViewModel> l)
    {
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
        var result = await ValidateAsync(ValidationContext<ContactFormViewModel>.CreateWithOptions((ContactFormViewModel)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}