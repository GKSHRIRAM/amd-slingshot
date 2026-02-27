using IoTCircuitBuilder.Domain.Entities;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface IBoardRepository
{
    Task<Board?> GetBoardByNameAsync(string name);
    Task<List<Board>> GetAllActiveBoardsAsync();
}
