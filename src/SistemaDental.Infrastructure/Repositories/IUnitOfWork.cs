namespace SistemaDental.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    ITenantRepository Tenants { get; }
    IPacienteRepository Pacientes { get; }
    IUsuarioRepository Usuarios { get; }
    ICitaRepository Citas { get; }
    IOdontogramaRepository Odontogramas { get; }
    ITratamientoRepository Tratamientos { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

