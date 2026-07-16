using DKH.McpGateway.Api;
using DKH.Platform.Grpc.Client;
using DKH.Platform.Identity;
using DKH.Platform.Identity.Interceptors;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using Microsoft.Extensions.DependencyInjection;
using PlatformHost = DKH.Platform.Platform;

namespace DKH.McpGateway.Tests.Auth;

public sealed class McpGatewayIdentityPropagationRegistrationTests
{
    [Fact]
    public void AddMcpHttpCurrentUserPropagation_RegistersCurrentUserAndInterceptor()
    {
        using var app = PlatformHost.CreateWeb([])
            .AddMcpHttpCurrentUserPropagation()
            .Build();
        using var scope = app.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPlatformCurrentUser>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<PlatformUserIdentityPropagationInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public void AddMcpGatewayEndpoints_HttpMode_RegistersIdentityInterceptorAndClients()
    {
        var grpc = Substitute.For<IPlatformGrpcClientBuilder>();

        grpc.AddMcpGatewayEndpoints(propagateCurrentUser: true);

        grpc.Received(1).AddGlobalInterceptor<PlatformUserIdentityPropagationInterceptor>();
        grpc.Received(1).AddEndpointFromConfiguration<StorefrontsCrudService.StorefrontsCrudServiceClient>();
    }

    [Fact]
    public void AddMcpGatewayEndpoints_StdioMode_RegistersClientsWithoutIdentityInterceptor()
    {
        var grpc = Substitute.For<IPlatformGrpcClientBuilder>();

        grpc.AddMcpGatewayEndpoints(propagateCurrentUser: false);

        grpc.DidNotReceive().AddGlobalInterceptor<PlatformUserIdentityPropagationInterceptor>();
        grpc.Received(1).AddEndpointFromConfiguration<StorefrontsCrudService.StorefrontsCrudServiceClient>();
    }
}
