#pragma warning disable ASPIREPOSTGRES001

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresUser = builder.AddParameter("postgres-user", "postgres");
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithDataVolume();
IResourceBuilder<PostgresDatabaseResource> launchpadDb = postgres.AddDatabase("launchpaddb");
launchpadDb.WithPostgresMcp();

builder.AddProject<Projects.Launchpad_Web>("launchpad-web")
    .WithReference(launchpadDb)
    .WaitFor(launchpadDb)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
