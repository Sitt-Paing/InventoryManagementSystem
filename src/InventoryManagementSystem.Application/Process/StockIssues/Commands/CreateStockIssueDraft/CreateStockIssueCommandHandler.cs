using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;

public class CreateStockIssueCommandHandler : IRequestHandler<CreateStockIssueCommand, StockIssueDto>
{
    private readonly IApplicationDbContext _context;
    public CreateStockIssueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockIssueDto> Handle(CreateStockIssueCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
