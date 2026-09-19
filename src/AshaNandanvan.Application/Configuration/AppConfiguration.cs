using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace AshaNandanvan.Application.Configuration;

/// <summary>
/// Shared configuration stack. Later sources override earlier ones when a key is present;
/// missing optional files are skipped.
/// </summary>
public static class AppConfiguration
{
    public const string UserSecretsId = "c3fde149-8517-4bdf-aa68-d7077fa659e4";

    public static IConfigurationBuilder AddAshaNandanvanSources(
        this IConfigurationBuilder builder,
        string contentRoot,
        string environmentName,
        string[]? commandLineArgs = null,
        Assembly? userSecretsAssembly = null)
    {
        builder.SetBasePath(contentRoot);

        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
        builder.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

        if (userSecretsAssembly is not null)
        {
            builder.AddUserSecrets(userSecretsAssembly, optional: true, reloadOnChange: true);
        }
        else
        {
            builder.AddUserSecrets(UserSecretsId, reloadOnChange: true);
        }

        builder.AddEnvironmentVariables();

        if (commandLineArgs is { Length: > 0 })
        {
            builder.AddCommandLine(commandLineArgs);
        }

        return builder;
    }

    public static IConfiguration Build(string contentRoot, string? environmentName = null, string[]? commandLineArgs = null)
    {
        var environment = environmentName
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        return new ConfigurationBuilder()
            .AddAshaNandanvanSources(contentRoot, environment, commandLineArgs)
            .Build();
    }

    public static string ResolveContentRoot(string currentDirectory)
    {
        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        var webRoot = Path.GetFullPath(Path.Combine(currentDirectory, "..", "AshaNandanvan.Web"));
        if (File.Exists(Path.Combine(webRoot, "appsettings.json")))
        {
            return webRoot;
        }

        throw new InvalidOperationException("Could not find appsettings.json. Run from the Web project or pass --startup-project.");
    }
}
