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
    public const string DockerSqlSourceVariable = "ASHA_SQL_SOURCE";
    public const string DockerSqlSourceValue = "docker";

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

        if (UsesDockerSql())
        {
            builder.AddInMemoryCollection(BuildDockerSqlSettings(contentRoot));
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

    public static bool UsesDockerSql() =>
        string.Equals(
            Environment.GetEnvironmentVariable(DockerSqlSourceVariable),
            DockerSqlSourceValue,
            StringComparison.OrdinalIgnoreCase);

    public static string BuildDockerSqlConnectionString(string contentRoot)
    {
        var values = ReadDockerSqlValues(contentRoot);
        return BuildConnectionString(values);
    }

    private static IEnumerable<KeyValuePair<string, string?>> BuildDockerSqlSettings(string contentRoot)
    {
        yield return new KeyValuePair<string, string?>(
            "ConnectionStrings:DefaultConnection",
            BuildDockerSqlConnectionString(contentRoot));
    }

    private static Dictionary<string, string> ReadDockerSqlValues(string contentRoot)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SQL_SERVER"] = "127.0.0.1,1433",
            ["SQL_DATABASE"] = "ashanandanvan-dev",
            ["SQL_USER"] = "sa",
            ["SQL_ENCRYPT"] = "True",
            ["SQL_TRUST_SERVER_CERTIFICATE"] = "True",
            ["SQL_MULTIPLE_ACTIVE_RESULT_SETS"] = "true"
        };

        var envFile = FindRepoEnvFile(contentRoot);
        if (envFile is not null)
        {
            foreach (var pair in ParseEnvFile(envFile))
            {
                if (IsDockerSqlKey(pair.Key))
                {
                    values[pair.Key] = pair.Value;
                }
            }
        }

        foreach (var key in values.Keys.ToList().Concat(["MSSQL_SA_PASSWORD"]))
        {
            var fromProcess = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromProcess))
            {
                values[key] = fromProcess;
            }
        }

        return values;
    }

    private static string BuildConnectionString(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue("MSSQL_SA_PASSWORD", out var password) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Docker SQL requires MSSQL_SA_PASSWORD. Set it in the repo-root .env (copy .env.example) or in the environment.");
        }

        var server = values.GetValueOrDefault("SQL_SERVER", "127.0.0.1,1433");
        var database = values.GetValueOrDefault("SQL_DATABASE", "ashanandanvan-dev");
        var user = values.GetValueOrDefault("SQL_USER", "sa");
        var encrypt = values.GetValueOrDefault("SQL_ENCRYPT", "True");
        var trust = values.GetValueOrDefault("SQL_TRUST_SERVER_CERTIFICATE", "True");
        var mars = values.GetValueOrDefault("SQL_MULTIPLE_ACTIVE_RESULT_SETS", "true");

        return $"Server={server};Database={database};User Id={user};Password={password};Encrypt={encrypt};TrustServerCertificate={trust};MultipleActiveResultSets={mars}";
    }

    private static bool IsDockerSqlKey(string key) =>
        key.Equals("MSSQL_SA_PASSWORD", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_SERVER", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_DATABASE", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_USER", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_ENCRYPT", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_TRUST_SERVER_CERTIFICATE", StringComparison.OrdinalIgnoreCase)
        || key.Equals("SQL_MULTIPLE_ACTIVE_RESULT_SETS", StringComparison.OrdinalIgnoreCase);

    internal static IEnumerable<KeyValuePair<string, string>> ParseEnvFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"').Trim('\'');
            if (key.Length == 0)
            {
                continue;
            }

            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static string? FindRepoEnvFile(string contentRoot)
    {
        var dir = new DirectoryInfo(contentRoot);
        while (dir is not null)
        {
            var envPath = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envPath))
            {
                return envPath;
            }

            if (File.Exists(Path.Combine(dir.FullName, "AshaNandanvan.slnx")))
            {
                return null;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
