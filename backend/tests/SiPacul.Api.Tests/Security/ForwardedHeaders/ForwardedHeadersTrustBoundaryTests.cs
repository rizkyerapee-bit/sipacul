using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SiPacul.Api.Security.ForwardedHeaders;

namespace SiPacul.Api.Tests.Security.ForwardedHeaders;

public sealed class ForwardedHeadersTrustBoundaryTests
{
    [Fact]
    public void Configuration_ShouldUseHardenedOneHopDefaults()
    {
        var knownProxy = IPAddress.Parse("10.20.30.40");
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string?>(
                "ForwardedHeaders:KnownProxies:0",
                knownProxy.ToString()));
        var services = new ServiceCollection();

        services.AddSiPaculForwardedHeaders(configuration);

        using var provider =
            services.BuildServiceProvider();
        var options =
            provider.GetRequiredService<
                IOptions<ForwardedHeadersOptions>>()
                .Value;

        Assert.Equal(
            Microsoft.AspNetCore.HttpOverrides
                .ForwardedHeaders.XForwardedFor |
            Microsoft.AspNetCore.HttpOverrides
                .ForwardedHeaders.XForwardedProto,
            options.ForwardedHeaders);
        Assert.True(options.ForwardLimit.HasValue);
        Assert.Equal(
            1,
            options.ForwardLimit.GetValueOrDefault());
        Assert.True(options.RequireHeaderSymmetry);
        Assert.Empty(options.KnownIPNetworks);
        Assert.Equal(
            knownProxy,
            Assert.Single(options.KnownProxies));
    }

    [Fact]
    public void Configuration_ShouldFallBackToExactLoopbackProxies()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddSiPaculForwardedHeaders(configuration);

        using var provider =
            services.BuildServiceProvider();
        var options =
            provider.GetRequiredService<
                IOptions<ForwardedHeadersOptions>>()
                .Value;

        Assert.Empty(options.KnownIPNetworks);
        Assert.Equal(
            new[]
            {
                IPAddress.Loopback,
                IPAddress.IPv6Loopback
            },
            options.KnownProxies);
    }

    [Fact]
    public void Configuration_ShouldRejectAutomaticCloudMode()
    {
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string?>(
                ForwardedHeadersServiceCollectionExtensions
                    .AutomaticForwardingSettingName,
                "true"));
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddSiPaculForwardedHeaders(
                configuration));

        Assert.Contains(
            "not allowed",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("::ffff:0.0.0.0")]
    public void Configuration_ShouldRejectNonExactKnownProxy(
        string configuredProxy)
    {
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string?>(
                "ForwardedHeaders:KnownProxies:0",
                configuredProxy));
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddSiPaculForwardedHeaders(
                configuration));
    }

    [Fact]
    public async Task Middleware_ShouldIgnoreHeadersFromUnknownPeer()
    {
        var knownProxy = IPAddress.Parse("10.20.30.40");
        var unknownPeer = IPAddress.Parse("10.20.30.41");
        var spoofedClient = IPAddress.Parse("198.51.100.25");
        var context = CreateContext(
            unknownPeer,
            spoofedClient,
            "https");

        var observed = await InvokeMiddlewareAsync(
            context,
            CreateOptions(knownProxy));

        Assert.Equal(
            unknownPeer,
            observed.RemoteIpAddress);
        Assert.Equal("http", observed.Scheme);
    }

    [Fact]
    public async Task Middleware_ShouldAcceptSymmetricHeadersFromKnownProxy()
    {
        var knownProxy = IPAddress.Parse("10.20.30.40");
        var forwardedClient = IPAddress.Parse("198.51.100.25");
        var context = CreateContext(
            knownProxy,
            forwardedClient,
            "https");

        var observed = await InvokeMiddlewareAsync(
            context,
            CreateOptions(knownProxy));

        Assert.Equal(
            forwardedClient,
            observed.RemoteIpAddress);
        Assert.Equal("https", observed.Scheme);
    }

    private static IConfiguration CreateConfiguration(
        params KeyValuePair<string, string?>[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static DefaultHttpContext CreateContext(
        IPAddress peer,
        IPAddress forwardedClient,
        string forwardedScheme)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = peer;
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] =
            forwardedClient.ToString();
        context.Request.Headers["X-Forwarded-Proto"] =
            forwardedScheme;
        return context;
    }

    private static ForwardedHeadersOptions CreateOptions(
        IPAddress knownProxy)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                Microsoft.AspNetCore.HttpOverrides
                    .ForwardedHeaders.XForwardedFor |
                Microsoft.AspNetCore.HttpOverrides
                    .ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            RequireHeaderSymmetry = true
        };

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(knownProxy);

        return options;
    }

    private static async Task<ObservedRequest>
        InvokeMiddlewareAsync(
            HttpContext context,
            ForwardedHeadersOptions options)
    {
        ObservedRequest? observed = null;
        RequestDelegate next = requestContext =>
        {
            observed = new ObservedRequest(
                requestContext.Connection.RemoteIpAddress,
                requestContext.Request.Scheme);
            return Task.CompletedTask;
        };
        var middleware = new ForwardedHeadersMiddleware(
            next,
            NullLoggerFactory.Instance,
            Options.Create(options));

        await middleware.Invoke(context);

        return Assert.IsType<ObservedRequest>(observed);
    }

    private sealed record ObservedRequest(
        IPAddress? RemoteIpAddress,
        string Scheme);
}
