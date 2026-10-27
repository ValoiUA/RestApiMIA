namespace Validators;
using MyTodoApi.DTO;
using FluentValidation;

public class WorkspaceValidator: AbstractValidator<WorkspaceDTO>
{
    public WorkspaceValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive is required.");

        RuleFor(x => x.RoomType)
            .IsInEnum().WithMessage("Invalid room type.");

    }
}