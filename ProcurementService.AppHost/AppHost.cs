var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ProcurementAiApi>("procurementaiapi");

builder.Build().Run();
