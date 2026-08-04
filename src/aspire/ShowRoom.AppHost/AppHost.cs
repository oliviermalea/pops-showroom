var builder = DistributedApplication.CreateBuilder(args);



builder.AddProject<Projects.ShowRoom_Business_Api>("showroom-business-api");



builder.Build().Run();
