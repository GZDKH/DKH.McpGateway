using Google.Protobuf;

namespace DKH.McpGateway.Tests.Tools.Inventory;

/// <summary>Controlled external Inventory transport; it supplies no native authority qualification.</summary>
internal sealed class InventoryCallInvoker : CallInvoker
{
    internal Func<object, CallOptions, Task<object>> RespondAsync { get; set; } = (_, _) => throw new InvalidOperationException("Unexpected native Inventory call.");

    internal Action OnDispose { get; set; } = () => { };

    internal List<(string Path, IMessage Request, byte[] Bytes, CallOptions Options)> Calls { get; } = [];

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        var message = (IMessage)request;
        Calls.Add((method.FullName, message, message.ToByteArray(), options));
        return new AsyncUnaryCall<TResponse>(ReadAsync<TResponse>(RespondAsync(request, options)), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], OnDispose);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();

    private static async Task<T> ReadAsync<T>(Task<object> task) => (T)await task;
}
