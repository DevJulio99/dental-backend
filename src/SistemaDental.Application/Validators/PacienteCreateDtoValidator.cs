using FluentValidation;
using SistemaDental.Application.DTOs.Paciente;

namespace SistemaDental.Application.Validators;

public class PacienteCreateDtoValidator : AbstractValidator<PacienteCreateDto>
{
    public PacienteCreateDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.DniPasaporte)
            .NotEmpty().WithMessage("El DNI/Pasaporte es requerido")
            .MaximumLength(50).WithMessage("El DNI/Pasaporte no puede exceder 50 caracteres");

        RuleFor(x => x.FechaNacimiento)
            .NotEmpty().WithMessage("La fecha de nacimiento es requerida")
            .LessThan(DateTime.Today).WithMessage("La fecha de nacimiento debe ser anterior a hoy");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no es válido")
            .When(x => !string.IsNullOrEmpty(x.Email))
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");
    }
}

