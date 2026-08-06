using ShowRoom.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin(c =>
    {
        c.WithHostPort(5050);
        c.WithImageTag("9.17");
    });

// Database-per-service: each API owns its own database.
var businessDb = postgres.AddDatabase("showroom-business");   // Order + Product
var customerDb = postgres.AddDatabase("showroom-customers");  // Customer

// RabbitMQ broker for machine-to-machine messaging (Wolverine AMQP request/reply).
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Order + Product service — CONSUMES the Order query over the bus and replies.
builder.AddProject<Projects.ShowRoom_Business_Api>("showroom-business-api")
    .WithHttpEndpoint(port: 5204, name: "http")
    .WithHttpsEndpoint(port: 7106, name: "https")
    .WithReference(businessDb)
    .WaitFor(businessDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithScalar();

// Customer service — PRODUCES the Order query over the bus (GetCustomerWithOrders).
builder.AddProject<Projects.ShowRoom_Customer_Api>("showroom-customer-api")
    .WithHttpEndpoint(port: 5205, name: "http")
    .WithHttpsEndpoint(port: 7107, name: "https")
    .WithReference(customerDb)
    .WaitFor(customerDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithScalar();

builder.Build().Run();
