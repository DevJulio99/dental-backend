using FluentValidation;
using SistemaDental.Application.DTOs.Odontograma;

namespace SistemaDental.Application.Validators;

public class OdontogramaCreateDtoValidator : AbstractValidator<OdontogramaCreateDto>
{
    public OdontogramaCreateDtoValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El ID del paciente es requerido")
            .NotEqual(Guid.Empty).WithMessage("El ID del paciente no puede estar vacío");

        RuleFor(x => x.NumeroDiente)
            .InclusiveBetween(11, 48).WithMessage("El número de diente debe estar entre 11 y 48")
            .Must(BeValidToothNumber).WithMessage("El número de diente no es válido según la numeración dental");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado del diente es requerido")
            .MaximumLength(50).WithMessage("El estado no puede exceder 50 caracteres");
    }

    private bool BeValidToothNumber(int numeroDiente)
    {
        // Validar que el número de diente esté en el rango válido de la numeración dental
        // Cuadrantes: 11-18 (superior derecho), 21-28 (superior izquierdo),
        //             31-38 (inferior izquierdo), 41-48 (inferior derecho)
        return (numeroDiente >= 11 && numeroDiente <= 18) ||
               (numeroDiente >= 21 && numeroDiente <= 28) ||
               (numeroDiente >= 31 && numeroDiente <= 38) ||
               (numeroDiente >= 41 && numeroDiente <= 48);
    }
}

