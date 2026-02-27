using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Infrastructure.Data;
using IoTCircuitBuilder.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IoTCircuitBuilder.Infrastructure.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CachePrefix = "board_";

    public BoardRepository(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Board?> GetBoardByNameAsync(string name)
    {
        string key = $"{CachePrefix}{name}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            return await _db.Boards
                .Include(b => b.Pins)
                    .ThenInclude(p => p.Capabilities)
                .Include(b => b.PowerDistributionRules)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Name == name && b.IsActive);
        });
    }

    public async Task<List<Board>> GetAllActiveBoardsAsync()
    {
        return await _db.Boards
            .Where(b => b.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }
}
