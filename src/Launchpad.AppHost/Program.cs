IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
IResourceBuilder<PostgresDatabaseResource> launchpadDb = postgres.AddDatabase("launchpaddb");

builder.AddProject<Projects.Launchpad_Web>("launchpad-web")
    .WithReference(launchpadDb)
    .WaitFor(launchpadDb)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
