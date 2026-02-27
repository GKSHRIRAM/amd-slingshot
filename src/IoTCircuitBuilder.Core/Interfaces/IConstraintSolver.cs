using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.ValueObjects;

namespace IoTCircuitBuilder.Core.Interfaces;

public interface IConstraintSolver
{
    Task<SolverResult> SolveAsync(Board board, List<Component> components);
}
