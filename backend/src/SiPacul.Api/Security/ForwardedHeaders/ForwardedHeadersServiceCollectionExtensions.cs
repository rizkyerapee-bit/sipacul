using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace SiPacul.Api.Security.ForwardedHeaders;

public static class ForwardedHeadersServiceCollectionExtensions
{
    public const string AutomaticForwardingSettingName =
        "ASPNETCORE_FORWARDEDHEADERS_ENABLED";

    public const string KnownProxiesSectionName =
        "ForwardedHeaders:KnownProxies";

    public static IServiceCollection AddSiPaculForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RejectAutomaticCloudForwarding(configuration);

        var knownProxies = ReadKnownProxies(configuration);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                Microsoft.AspNetCore.HttpOverrides
                    .ForwardedHeaders.XForwardedFor |
                Microsoft.AspNetCore.HttpOverrides
                    .ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var knownProxy in knownProxies)
            {
                if (!options.KnownProxies.Contains(knownProxy))
                {
                    options.KnownProxies.Add(knownProxy);
                }
            }
        });

        return services;
    }

    private static void RejectAutomaticCloudForwarding(
        IConfiguration configuration)
    {
        var configuredValue =
            configuration[AutomaticForwardingSettingName];

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return;
        }

        if (!bool.TryParse(
                configuredValue.Trim(),
                out var automaticForwardingEnabled))
        {
            throw new InvalidOperationException(
                $"{AutomaticForwardingSettingName} must be false or unset.");
        }

        if (automaticForwardingEnabled)
        {
            throw new InvalidOperationException(
                $"{AutomaticForwardingSettingName}=true is not allowed. " +
                $"Configure exact proxy addresses under " +
                $"{KnownProxiesSectionName} instead.");
        }
    }

    private static IReadOnlyList<IPAddress> ReadKnownProxies(
        IConfiguration configuration)
    {
        var section =
            configuration.GetSection(KnownProxiesSectionName);
        var configuredValues =
            new List<string?>();

        if (section.Value is not null)
        {
            configuredValues.Add(section.Value);
        }

        configuredValues.AddRange(
            section.GetChildren()
                .Select(child => child.Value));

        var knownProxies =
            new List<IPAddress>();

        foreach (var configuredValue in configuredValues)
        {
            if (string.IsNullOrWhiteSpace(configuredValue) ||
                !IPAddress.TryParse(
                    configuredValue.Trim(),
                    out var knownProxy))
            {
                throw new InvalidOperationException(
                    $"Every {KnownProxiesSectionName} entry must be " +
                    "an exact, non-wildcard IP address.");
            }

            var wildcardCandidate =
                knownProxy.IsIPv4MappedToIPv6
                    ? knownProxy.MapToIPv4()
                    : knownProxy;

            if (wildcardCandidate.Equals(IPAddress.Any) ||
                wildcardCandidate.Equals(IPAddress.IPv6Any) ||
                wildcardCandidate.Equals(IPAddress.None))
            {
                throw new InvalidOperationException(
                    $"Every {KnownProxiesSectionName} entry must be " +
                    "an exact, non-wildcard IP address.");
            }

            if (!knownProxies.Contains(knownProxy))
            {
                knownProxies.Add(knownProxy);
            }
        }

        if (knownProxies.Count == 0)
        {
            knownProxies.Add(IPAddress.Loopback);
            knownProxies.Add(IPAddress.IPv6Loopback);
        }

        return knownProxies;
    }
}
