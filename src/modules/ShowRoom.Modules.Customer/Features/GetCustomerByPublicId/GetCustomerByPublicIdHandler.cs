using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Observability;
using ShowRoom.Modules.Customer.Persistence;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

public sealed class GetCustomerByPublicIdHandler(
    CustomersContext context,
    ILogger<GetCustomerByPublicIdHandler> logger)
    : IQueryHandler<GetCustomerByPublicIdQuery, Result<CustomerResponse>>
{
    private const string FeatureName = "GetCustomerByPublicId";

    public async Task<Result<CustomerResponse>> HandleAsync(
        GetCustomerByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerConventions.ModuleName, FeatureName, requestId);
        using var activity = CustomerTelemetry.ActivitySource.StartActivity("customer.get_customer_by_public_id");

        var publicId = query.PublicId;
        activity?
            .SetCommonTags(CustomerConventions.ModuleName, FeatureName, requestId)
            .SetTag("customer.public_id", publicId.Value);

        logger.LogInformation("Fetching customer by public id {PublicId}", publicId.Value);

        var customer = await context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (customer is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Customer not found");
            logger.LogWarning("Customer {PublicId} not found", publicId.Value);
            return CustomerErrors.NotFound(publicId);
        }

        logger.LogInformation("Customer {PublicId} retrieved", publicId.Value);
        return Result<CustomerResponse>.Success(GetCustomerByPublicIdAssembler.From(customer));
    }
}
