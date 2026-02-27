using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Infrastructure.Data;
using IoTCircuitBuilder.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IoTCircuitBuilder.Infrastructure.Repositories;

public class ComponentRepository : IComponentRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CachePrefix = "comp_";

    public ComponentRepository(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<Component>> GetComponentsByTypesAsync(List<string> types)
    {
        var result = new List<Component>();

        foreach (var type in types)
        {
            var comp = await GetComponentByTypeAsync(type);
            if (comp != null) result.Add(comp);
        }

        return result;
    }

    public async Task<Component?> GetComponentByTypeAsync(string type)
    {
        string key = $"{CachePrefix}{type}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            return await _db.Components
                .Include(c => c.PinRequirements)
                .Include(c => c.ComponentLibraries)
                    .ThenInclude(cl => cl.Library)
                .Include(c => c.I2cAddresses)
                .Include(c => c.CodeTemplates)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Type == type && c.IsActive);
        });
    }

    public async Task<bool> CheckI2cConflictsAsync(List<int> componentIds)
    {
        var addresses = await _db.I2cAddresses
            .Where(i => componentIds.Contains(i.ComponentId))
            .Select(i => i.DefaultAddress)
            .ToListAsync();

        return addresses.Count != addresses.Distinct().Count();
    }
}
