using FluentValidation;
using SistemaDental.Application.DTOs.Tratamiento;

namespace SistemaDental.Application.Validators;

public class TratamientoCreateDtoValidator : AbstractValidator<TratamientoCreateDto>
{
    public TratamientoCreateDtoValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El ID del paciente es requerido")
            .NotEqual(Guid.Empty).WithMessage("El ID del paciente no puede estar vacío");

        RuleFor(x => x.TreatmentPerformed)
            .NotEmpty().WithMessage("El nombre del tratamiento realizado es requerido")
            .MaximumLength(500).WithMessage("El nombre del tratamiento no puede exceder 500 caracteres");

        RuleFor(x => x.Diagnosis)
            .MaximumLength(1000).WithMessage("El diagnóstico no puede exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Diagnosis));

        RuleFor(x => x.Costo)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo")
            .When(x => x.Costo.HasValue);
    }
}

