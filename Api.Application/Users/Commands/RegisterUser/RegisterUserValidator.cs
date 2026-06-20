namespace Api.Application.Users.Commands.RegisterUser;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(user => user.Email)
            .MaximumLength(200).WithMessage("Email length must not exceed 200 characters")
            .NotEmpty().WithMessage("Email is required.");
        RuleFor(user => user.Password)
            .MaximumLength(200).WithMessage("Password length must not exceed 200 characters")
            .NotEmpty().WithMessage("Password is required.");
        RuleFor(user => user.FirstName)
            .MaximumLength(200).WithMessage("First name length must not exceed 200 characters")
            .NotEmpty().WithMessage("First name is required.");
        RuleFor(user => user.LastName)
            .MaximumLength(200).WithMessage("Last name length must not exceed 200 characters");
    }
}