namespace ScepAdmin.Services;

public interface IBootstrapService
{
    Task<object> SeedDemoDataAsync(CancellationToken cancellationToken = default);
}
