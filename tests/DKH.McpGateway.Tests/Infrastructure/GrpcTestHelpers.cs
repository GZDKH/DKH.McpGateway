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

    internal static AsyncServerStreamingCall<T> CreateServerStreamingCall<T>(params T[] responses)
        where T : class
        => new(
            new InMemoryAsyncStreamReader<T>(responses),
            Task.FromResult<Metadata>([]),
            () => Status.DefaultSuccess,
            static () => [],
            () => { });

    private sealed class InMemoryAsyncStreamReader<T>(IReadOnlyList<T> responses) : IAsyncStreamReader<T>
        where T : class
    {
        private int _index = -1;

        public T Current { get; private set; } = default!;

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            _index++;
            if (_index >= responses.Count)
            {
                return Task.FromResult(false);
            }

            Current = responses[_index];
            return Task.FromResult(true);
        }
    }
}
