using FluentValidation;
using SistemaDental.Application.DTOs.Odontograma;
using SistemaDental.Domain.Enums;

namespace SistemaDental.Application.Validators;

public class OdontogramaCreateDtoValidator : AbstractValidator<OdontogramaCreateDto>
{
    // Valores válidos en español e inglés
    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        // Español
        "sano", "curado", "pendiente", "caries", "extraido", "extraído",
        "endodoncia", "corona", "implante", "fracturado", "a_extraer", "a extraer", "puente",
        // Inglés
        "healthy", "filled", "in_treatment", "in treatment", "cavity", "missing",
        "root_canal", "root canal", "crown", "implant", "fractured", "to_extract", "to extract", "bridge"
    };

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
            .Must(BeValidEstado).WithMessage("El estado del diente no es válido. Valores válidos: sano, curado, pendiente, caries, extraido, endodoncia, corona, implante, fracturado, a_extraer, puente");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).WithMessage("Las observaciones no pueden exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Observaciones));

        RuleFor(x => x.FechaRegistro)
            .Must(fecha => !fecha.HasValue || fecha.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de registro no puede ser futura")
            .When(x => x.FechaRegistro.HasValue);

        RuleFor(x => x.FechaRegistroDateTime)
            .Must(fecha => !fecha.HasValue || fecha.Value <= DateTime.UtcNow)
            .WithMessage("La fecha de registro no puede ser futura")
            .When(x => x.FechaRegistroDateTime.HasValue);
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

    private bool BeValidEstado(string estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return false;

        return EstadosValidos.Contains(estado.Trim());
    }
}

