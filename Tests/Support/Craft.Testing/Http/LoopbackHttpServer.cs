using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Craft.Testing.Http;

/// <summary>Serves empty HTTP 200 responses on an ephemeral loopback port for network tests.</summary>
public sealed class LoopbackHttpServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _server;

    public LoopbackHttpServer()
    {
        _listener.Start();
        Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
        _server = ServeAsync();
    }

    public string Url { get; }

    private async Task ServeAsync()
    {
        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                using TcpClient client = await _listener.AcceptTcpClientAsync(_cancellation.Token);
                await using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII, leaveOpen: true);
                string? line;
                do
                {
                    line = await reader.ReadLineAsync(_cancellation.Token);
                } while (!string.IsNullOrEmpty(line));

                await stream.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), _cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        try
        {
            await _server;
        }
        finally
        {
            _listener.Stop();
            _cancellation.Dispose();
        }
    }
}