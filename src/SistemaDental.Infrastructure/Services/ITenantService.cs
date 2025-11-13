namespace SistemaDental.Infrastructure.Services;

public interface ITenantService
{
    Guid? GetCurrentTenantId();
    void SetCurrentTenant(Guid tenantId);
    void ClearTenant();
}

