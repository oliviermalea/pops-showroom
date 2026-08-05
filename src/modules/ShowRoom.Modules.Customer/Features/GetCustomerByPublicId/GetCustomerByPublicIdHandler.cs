using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Persistence;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

public sealed class GetCustomerByPublicIdHandler(
    CustomersContext context,
    ILogger<GetCustomerByPublicIdHandler> logger)
    : IQueryHandler<GetCustomerByPublicIdQuery, Result<CustomerResponse>>
{
    private const string Feature = nameof(GetCustomerByPublicIdHandler);

    public async Task<Result<CustomerResponse>> HandleAsync(
        GetCustomerByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginModuleScope(CustomerConventions.ModuleName, Feature);

        var publicId = query.PublicId;
        logger.LogInformation("Fetching customer by public id {PublicId}", publicId.Value);

        var customer = await context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (customer is null)
        {
            logger.LogWarning("Customer {PublicId} not found", publicId.Value);
            return CustomerErrors.NotFound(publicId);
        }

        logger.LogInformation("Customer {PublicId} retrieved", publicId.Value);
        return Result<CustomerResponse>.Success(GetCustomerByPublicIdAssembler.From(customer));
    }
}
