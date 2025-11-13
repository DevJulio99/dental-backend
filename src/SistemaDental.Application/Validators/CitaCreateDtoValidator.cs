using FluentValidation;
using SistemaDental.Application.DTOs.Cita;

namespace SistemaDental.Application.Validators;

public class CitaCreateDtoValidator : AbstractValidator<CitaCreateDto>
{
    public CitaCreateDtoValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El ID del paciente es requerido")
            .NotEqual(Guid.Empty).WithMessage("El ID del paciente no puede estar vacío");

        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("El ID del odontólogo es requerido")
            .NotEqual(Guid.Empty).WithMessage("El ID del odontólogo no puede estar vacío");

        RuleFor(x => x.AppointmentDate)
            .NotEmpty().WithMessage("La fecha de la cita es requerida")
            .Must(BeInFutureOrToday).WithMessage("La fecha de la cita debe ser hoy o futura");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La hora de inicio es requerida");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de la cita es requerido")
            .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres");

        RuleFor(x => x.DuracionMinutos)
            .GreaterThan(0).WithMessage("La duración debe ser mayor a 0")
            .LessThanOrEqualTo(480).WithMessage("La duración no puede exceder 8 horas");
    }

    private bool BeInFutureOrToday(DateOnly fecha)
    {
        return fecha >= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

