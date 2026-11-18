using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TcpServer
{
    private const int Port = 5000;
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main() 
    {
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"[Сервер] Запущен на порту {Port}. Ожидание клиента...");

        while (true)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"[Сервер] Подключён клиент: {client.Client.RemoteEndPoint}");
            await HandleClientAsync(client);
            Console.WriteLine("[Сервер] Клиент отключён. Ожидание следующего...");
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            string received = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine($"[Сервер] Получено: \"{received}\"");

            string response;

            if (received.StartsWith("PING ", StringComparison.OrdinalIgnoreCase))
            {
                string url = received.Substring(5).Trim();
                response = await PingSiteAsync(url);
            }
            else
            {
                response = received.ToUpperInvariant();
            }

            byte[] data = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"[Сервер] Отправлено: \"{response}\"");
        }
    }

    static async Task<string> PingSiteAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "ERROR: URL не указан";

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            stopwatch.Stop();

            long ms = stopwatch.ElapsedMilliseconds;
            return $"PONG: {url} — {ms} ms (HTTP {(int)response.StatusCode})";
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return $"ERROR: Таймаут ({stopwatch.ElapsedMilliseconds} ms) для {url}";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return $"ERROR: {ex.Message}";
        }
    }
}