using FluentValidation;
using Microsoft.Extensions.Localization;
using Soenneker.Blazor.Turnstile;

namespace AlbertoBizzini.Web.Models;

public class ContactFormModel
{
    public static bool IgnorePrivacyPolicy = true;

    public string Name { get; set; }
    public string Email { get; set; }

    public string Message { get; set; }

    public bool PrivacyPolicyViewed { get; set; }
    public bool ConfirmedPrivacyPolicyViewed { get; set; }

    public bool ResponsibilityTaken { get; set; }

    public string? TurnstileToken { get; set; } = null;
    public bool TurnstileTokenIsNull => TurnstileToken is null;

    public bool SubmitButtonDisabled => TurnstileTokenIsNull || !ResponsibilityTaken;

    public void Reset()
    {
        Name = Email = Message = string.Empty;
        PrivacyPolicyViewed = ConfirmedPrivacyPolicyViewed = ResponsibilityTaken = false;
        TurnstileToken = null;
    }
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

        if (!ContactFormModel.IgnorePrivacyPolicy)
        {
            RuleFor(x => x.PrivacyPolicyViewed)
                .NotEmpty()
                .WithMessage(x => l["MustViewPrivacyPolicyValidationError"]);

            RuleFor(x => x.ConfirmedPrivacyPolicyViewed)
                .NotEmpty()
                .WithMessage(x => l["MustConfirmPrivacyPolicyViewedValidationError"]);
        }

        RuleFor(x => x.ResponsibilityTaken)
            .NotEmpty()
            .WithMessage(x => l["ResponsibilityTakenRequiredValidationError"]);

    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<ContactFormModel>.CreateWithOptions((ContactFormModel)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}