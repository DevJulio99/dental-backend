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

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es requerido")
            .MaximumLength(50).WithMessage("El tipo de documento no puede exceder 50 caracteres");

        RuleFor(x => x.DniPasaporte)
            .NotEmpty().WithMessage("El DNI/Pasaporte es requerido")
            .MaximumLength(50).WithMessage("El DNI/Pasaporte no puede exceder 50 caracteres");

        RuleFor(x => x.FechaNacimiento)
            .NotEmpty().WithMessage("La fecha de nacimiento es requerida")
            .LessThan(DateTime.Today).WithMessage("La fecha de nacimiento debe ser anterior a hoy");

        RuleFor(x => x.Genero)
            .MaximumLength(20).WithMessage("El género no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Genero));

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.TelefonoAlternativo)
            .MaximumLength(50).WithMessage("El teléfono alternativo no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TelefonoAlternativo));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no es válido")
            .When(x => !string.IsNullOrEmpty(x.Email))
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Ciudad)
            .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Ciudad));

        RuleFor(x => x.TipoSangre)
            .MaximumLength(10).WithMessage("El tipo de sangre no puede exceder 10 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TipoSangre));

        RuleFor(x => x.ContactoEmergenciaNombre)
            .MaximumLength(255).WithMessage("El nombre del contacto de emergencia no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ContactoEmergenciaNombre));

        RuleFor(x => x.ContactoEmergenciaTelefono)
            .MaximumLength(50).WithMessage("El teléfono del contacto de emergencia no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ContactoEmergenciaTelefono));

        RuleFor(x => x.SeguroDental)
            .MaximumLength(255).WithMessage("El seguro dental no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.SeguroDental));

        RuleFor(x => x.NumeroSeguro)
            .MaximumLength(100).WithMessage("El número de seguro no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.NumeroSeguro));
    }
}

