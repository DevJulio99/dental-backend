using Microsoft.EntityFrameworkCore.Storage;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private IDbContextTransaction? _transaction;

    private ITenantRepository? _tenants;
    private IPacienteRepository? _pacientes;
    private IUsuarioRepository? _usuarios;
    private ICitaRepository? _citas;
    private IOdontogramaRepository? _odontogramas;
    private ITratamientoRepository? _tratamientos;

    public UnitOfWork(ApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public ITenantRepository Tenants =>
        _tenants ??= new TenantRepository(_context, _tenantService);

    public IPacienteRepository Pacientes =>
        _pacientes ??= new PacienteRepository(_context, _tenantService);

    public IUsuarioRepository Usuarios =>
        _usuarios ??= new UsuarioRepository(_context, _tenantService);

    public ICitaRepository Citas =>
        _citas ??= new CitaRepository(_context, _tenantService);

    public IOdontogramaRepository Odontogramas =>
        _odontogramas ??= new OdontogramaRepository(_context, _tenantService);

    public ITratamientoRepository Tratamientos =>
        _tratamientos ??= new TratamientoRepository(_context, _tenantService);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

