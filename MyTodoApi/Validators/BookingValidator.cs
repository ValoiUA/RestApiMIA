namespace MyTodoApi.Models;
using MyTodoApi.DTO;
using FluentValidation;

public class BookingValidator: AbstractValidator<BookingDTO>
{
    public BookingValidator()
    {
        RuleFor(x => x.BookingDate)
            .NotNull().WithMessage("BookingDate is required.")
            .GreaterThanOrEqualTo(DateTime.Now).WithMessage("BookingDate must be in the future.");

        RuleFor(x => x.TimeBooking)
            .InclusiveBetween(60, 360).WithMessage("TimeBooking must be between 60 and 360 minutes.");
        RuleFor(x => x.TimeBooking).Must(time => time % 30 == 0).WithMessage("TimeBooking must be a multiple of 30 minutes.");

        RuleFor(x => x.ResidentName)
            .NotEmpty().WithMessage("ResidentName is required.")
            .MaximumLength(50).WithMessage("ResidentName cannot exceed 100 characters.");
         RuleFor(x => x.ResidentSurName)
            .NotEmpty().WithMessage("ResidentSurName is required.")
            .MaximumLength(50).WithMessage("ResidentSurName cannot exceed 100 characters.");

        RuleFor(x => x.ResidentMaxCount)
            .InclusiveBetween(1, 15).WithMessage("ResidentMaxCount must be between 1 and 15.");
        
    }
}