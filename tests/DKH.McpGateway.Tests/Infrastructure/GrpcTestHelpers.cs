namespace DKH.McpGateway.Tests.Infrastructure;

internal static class GrpcTestHelpers
{
    internal static AsyncUnaryCall<T> CreateAsyncUnaryCall<T>(T response) where T : class
        => new(
            Task.FromResult(response),
            Task.FromResult<Metadata>([]),
            () => Status.DefaultSuccess,
            static () => [],
            () => { });

    internal static AsyncUnaryCall<T> CreateFaultedAsyncUnaryCall<T>(StatusCode statusCode, string detail = "")
        where T : class
    {
        var status = new Status(statusCode, detail);
        return new AsyncUnaryCall<T>(
            Task.FromException<T>(new RpcException(status)),
            Task.FromResult<Metadata>([]),
            () => status,
            static () => [],
            () => { });
    }
}
