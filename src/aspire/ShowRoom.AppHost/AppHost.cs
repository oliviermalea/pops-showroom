using ShowRoom.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin(c =>
    {
        c.WithHostPort(5050);
        c.WithImageTag("9.17");
    });

var customerDb = postgres.AddDatabase("showroom-business");

// RabbitMQ broker for machine-to-machine messaging (Wolverine AMQP request/reply).
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

builder.AddProject<Projects.ShowRoom_Business_Api>("showroom-business-api")
    .WithHttpEndpoint(port: 5204, name: "http")
    .WithHttpsEndpoint(port: 7106, name: "https")
    .WithReference(customerDb)
    .WaitFor(customerDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithScalar();

builder.Build().Run();
