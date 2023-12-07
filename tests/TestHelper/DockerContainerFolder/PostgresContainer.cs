using System.Collections.Concurrent;
using System.Diagnostics;
using Docker.DotNet.Models;
using Xunit.Abstractions;

namespace TestHelper.DockerContainerFolder;

public static class PostgresContainer
{
    private const string PostgresTag = "16.1";
    private const string DatabaseName = "item-tracker";
    private const string Username = "postgres";
    private const string Password = "123";

    private static readonly ConcurrentBag<string> ContainerNames = new();
    private static readonly object ContainerNamesLock = new();

    private static string GetContainerNameId()
    {
        var containerNameId = Guid.NewGuid().ToString().Replace("-", "");
        containerNameId = containerNameId[..(containerNameId.Length / 4)];
        lock (ContainerNamesLock)
        {
            while (ContainerNames.Any(s => s.Equals(containerNameId)))
            {
                containerNameId = Guid.NewGuid().ToString().Replace("-", "");
                containerNameId = containerNameId[..(containerNameId.Length / 4)];
            }

            ContainerNames.Add(containerNameId);
        }

        return containerNameId;
    }

    public static async Task<(string id, string containerName, string connectionString)> StartNew(
        ITestOutputHelper outputHelper)
    {
        var name = $"postgres-{GetContainerNameId()}";
        await DockerApi.DockerClient.Images
            .CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = "postgres",
                    Tag = PostgresTag
                },
                new AuthConfig(),
                new Progress<JSONMessage>());

        var response = await DockerApi.DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = $"postgres:{PostgresTag}",
            Name = name,
            Env = new List<string> { $"POSTGRES_PASSWORD={Password}", $"POSTGRES_DB={DatabaseName}" },
            HostConfig = new HostConfig
            {
                PublishAllPorts = true
            }
        });

        await DockerApi.DockerClient.Containers.StartContainerAsync(response.ID, null);
        var inspectResponse = await DockerApi.DockerClient.Containers.InspectContainerAsync(response.ID);
        var port = inspectResponse.NetworkSettings.Ports.First().Value.First().HostPort;

        var postgresStarted = false;
        var progress = new Progress<string>();
        progress.ProgressChanged += (_, logLine) =>
        {
            outputHelper.WriteLine(logLine);
            if (logLine.EndsWith("database system is ready to accept connections"))
            {
                postgresStarted = true;
            }
        };
        var cancellationTokenSource = new CancellationTokenSource();
        _ = DockerApi.DockerClient.Containers.GetContainerLogsAsync(response.ID, new ContainerLogsParameters()
        {
            Timestamps = true,
            Follow = true,
            ShowStdout = true,
            ShowStderr = true
        }, cancellationTokenSource.Token, progress);
        var sw = Stopwatch.StartNew();
        while (true)
        {
            await Task.Delay(10);
            if (sw.ElapsedMilliseconds >= 3000)
            {
                await cancellationTokenSource.CancelAsync();
                throw new Exception("Postgres container failed to start");
            }

            if (postgresStarted)
            {
                await cancellationTokenSource.CancelAsync();
                break;
            }
        }

        await Task.Delay(1000);
        outputHelper.WriteLine($"Postgres container name: {name}");
        outputHelper.WriteLine($"Postgres container port: {port}");
        outputHelper.WriteLine($"Postgres container database name: {DatabaseName}");
        outputHelper.WriteLine($"Postgres container username: {Username}");
        outputHelper.WriteLine($"Postgres container password: {Password}");
        return (response.ID, name,
            $"Host=127.0.0.1:{port};Database={DatabaseName};Username={Username};Password={Password}");
    }

    public static async Task Kill(string id)
    {
        await DockerApi.DockerClient.Containers.KillContainerAsync(id, new ContainerKillParameters());
        await DockerApi.DockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters());
    }
}