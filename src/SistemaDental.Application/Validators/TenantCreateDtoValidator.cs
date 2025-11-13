using FluentValidation;
using SistemaDental.Application.DTOs.Tenant;

namespace SistemaDental.Application.Validators;

public class TenantCreateDtoValidator : AbstractValidator<TenantCreateDto>
{
    public TenantCreateDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del consultorio es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Subdominio)
            .NotEmpty().WithMessage("El subdominio es requerido")
            .MaximumLength(100).WithMessage("El subdominio no puede exceder 100 caracteres")
            .Matches("^[a-z0-9-]+$").WithMessage("El subdominio solo puede contener letras minúsculas, números y guiones");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El email no es válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres");

        RuleFor(x => x.AdminNombre)
            .NotEmpty().WithMessage("El nombre del administrador es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.AdminApellido)
            .NotEmpty().WithMessage("El apellido del administrador es requerido")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("El email del administrador es requerido")
            .EmailAddress().WithMessage("El email no es válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("La contraseña es requerida")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres");
    }
}

