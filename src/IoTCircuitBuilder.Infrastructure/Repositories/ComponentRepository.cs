using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Infrastructure.Data;
using IoTCircuitBuilder.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

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
                .FirstOrDefaultAsync(c => c.Type.ToLower() == type.ToLower() && c.IsActive);
        });
    }

    public async Task<bool> CheckI2cConflictsAsync(List<int> componentIds)
    {
        // Fetch all addresses for the requested instances
        var addresses = await _db.I2cAddresses
            .Where(i => componentIds.Contains(i.ComponentId))
            .ToListAsync();

        // Map componentId -> DefaultAddress
        // We need to count occurrences of each address based on how many times that componentId appears in the input list
        var addressList = new List<string>();
        foreach (var id in componentIds)
        {
            var compAddress = addresses.FirstOrDefault(a => a.ComponentId == id)?.DefaultAddress;
            if (!string.IsNullOrEmpty(compAddress))
            {
                addressList.Add(compAddress);
            }
        }

        return addressList.Count != addressList.Distinct().Count();
    }

    public async Task<List<string>> GetAllComponentTypesAsync()
    {
        return await _db.Components
            .Where(c => c.IsActive)
            .Select(c => c.Type)
            .ToListAsync();
    }
}
