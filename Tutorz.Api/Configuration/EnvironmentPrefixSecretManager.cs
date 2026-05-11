using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Tutorz.Api.Configuration;

/// <summary>
/// Controls which Key Vault secrets are loaded and how their names map to
/// .NET configuration keys, based on the current hosting environment.
///
/// Secret naming convention in Key Vault:
///   {Prefix}--{Section}--{Key}
///
/// Prefixes:
///   Shared--       loaded by ALL environments   (e.g. Shared--SmsSettings--ApiToken)
///   DevStaging--   loaded by Development + Staging (e.g. DevStaging--Jwt--Key)
///   Production--   loaded by Production only   (e.g. Production--ConnectionStrings--DefaultConnection)
///   Development--  loaded by Development only  (for dev-specific overrides)
///   Staging--      loaded by Staging only       (for staging-specific overrides)
///
/// The prefix is stripped and '--' is replaced with ':' to produce the final
/// config key, so 'Production--ConnectionStrings--DefaultConnection'
/// becomes 'ConnectionStrings:DefaultConnection'.
/// </summary>
internal sealed class EnvironmentPrefixSecretManager : KeyVaultSecretManager
{
    private readonly string _envPrefix;       // e.g. "Production--"
    private const string SharedPrefix     = "Shared--";
    private const string DevStagingPrefix  = "DevStaging--";

    private readonly bool _loadDevStaging;    // true for Development and Staging

    public EnvironmentPrefixSecretManager(string environment)
    {
        _envPrefix      = $"{environment}--";
        _loadDevStaging = environment is "Development" or "Staging";
    }

    /// <summary>
    /// Return true for any secret this environment should load.
    /// Key Vault only fetches secrets where this returns true — avoids
    /// unnecessary reads and respects least-privilege access.
    /// </summary>
    public override bool Load(SecretProperties secret) =>
        secret.Name.StartsWith(_envPrefix,    StringComparison.OrdinalIgnoreCase) ||
        secret.Name.StartsWith(SharedPrefix,  StringComparison.OrdinalIgnoreCase) ||
        (_loadDevStaging && secret.Name.StartsWith(DevStagingPrefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Strip the environment/shared prefix, then replace '--' with ':'
    /// so the secret maps to the right .NET IConfiguration key.
    /// </summary>
    public override string GetKey(KeyVaultSecret secret)
    {
        var name = secret.Name;

        if (name.StartsWith(_envPrefix, StringComparison.OrdinalIgnoreCase))
            name = name[_envPrefix.Length..];
        else if (_loadDevStaging && name.StartsWith(DevStagingPrefix, StringComparison.OrdinalIgnoreCase))
            name = name[DevStagingPrefix.Length..];
        else if (name.StartsWith(SharedPrefix, StringComparison.OrdinalIgnoreCase))
            name = name[SharedPrefix.Length..];

        return name.Replace("--", ConfigurationPath.KeyDelimiter);
    }
}
