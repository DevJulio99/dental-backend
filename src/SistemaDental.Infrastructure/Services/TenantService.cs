namespace SistemaDental.Infrastructure.Services;

public class TenantService : ITenantService
{
    private Guid? _currentTenantId;

    public Guid? GetCurrentTenantId()
    {
        return _currentTenantId;
    }

    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    public void ClearTenant()
    {
        _currentTenantId = null;
    }
}

