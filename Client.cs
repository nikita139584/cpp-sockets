using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    private const int DEFAULT_BUFLEN = 512;
    private const int DEFAULT_PORT = 27015;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "CLIENT SIDE";
        try
        {
            var ipAddress = IPAddress.Loopback;
            var remoteEndPoint = new IPEndPoint(ipAddress, DEFAULT_PORT);

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(remoteEndPoint);
            Console.WriteLine("Підключення до сервера встановлено.");

            // завдання надсилання повідомлень
            var sendingTask = Task.Run(async () =>
            {
                while (true)
                {
                    Console.Write("Введіть повідомлення для надсилання серверу: ");
                    var message = Console.ReadLine();
                    if (message == "exit") break;
                    var messageBytes = Encoding.UTF8.GetBytes(message!);
                    await clientSocket.SendAsync(messageBytes);
                }

                // повідомляємо сервер, що відправлення завершено, але при цьому продовжуємо отримувати відповіді від сервера
                clientSocket.Shutdown(SocketShutdown.Send);
            });

            // завдання отримання повідомлень
            var receivingTask = Task.Run(async () =>
            {
                var buffer = new byte[DEFAULT_BUFLEN];

                while (true)
                {
                    int bytesReceived = await clientSocket.ReceiveAsync(buffer);
                    if (bytesReceived > 0)
                    {
                        // !!! використовується тільки отримана кількість байт !!!
                        var response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        Console.WriteLine($"\nВідповідь від сервера: {response}");
                    }
                    else
                    {
                        // якщо сервер закрив з'єднання, виходимо з циклу
                        Console.WriteLine("Сервер закрив з'єднання.");
                        break;
                    }
                }
            });

            // очікування завершення обох завдань
            await Task.WhenAll(sendingTask, receivingTask);

            clientSocket.Close();
            Console.WriteLine("З’єднання з сервером закрито.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Сталася помилка: {ex.Message}");
        }
    }
}
