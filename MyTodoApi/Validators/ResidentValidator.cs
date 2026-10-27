namespace MyTodoApi.Validators;
using FluentValidation;
using MyTodoApi.DTO;

public class ResidentCreateDTOValidator: AbstractValidator<ResidentDTO>
{
    public ResidentCreateDTOValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 50).WithMessage("Name must be between 2 and 50 characters.");

        RuleFor(x => x.SurName)
            .NotEmpty().WithMessage("SurName is required.")
            .Length(2, 50).WithMessage("SurName must be between 2 and 50 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid resident type.");
    }
}