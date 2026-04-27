using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    private const int DEFAULT_BUFLEN = 512;
    private const int DEFAULT_PORT = 27015;

    // черга тепер зберігає точну кількість отриманих байт !!!
    private static ConcurrentQueue<(Socket client, byte[] data, int length)> messageQueue = new ConcurrentQueue<(Socket, byte[], int)>();

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "SERVER SIDE";
        Console.WriteLine("Процес сервера запущено!");

        try
        {
            var ipAddress = IPAddress.Any;
            var localEndPoint = new IPEndPoint(ipAddress, DEFAULT_PORT);

            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            Console.WriteLine("Починається прослуховування інформації від клієнта.\nБудь ласка, запустіть клієнтську програму!");

            var clientSocket = await listener.AcceptAsync();
            Console.WriteLine("Підключення з клієнтською програмою встановлено успішно!");

            listener.Close();

            // обробка повідомлень у окремому таску
            _ = ProcessMessages();

            var buffer = new byte[DEFAULT_BUFLEN];

            while (true)
            {
                int bytesReceived = await clientSocket.ReceiveAsync(buffer);
                if (bytesReceived > 0)
                {
                    // !!! копіюються тільки отримані байти
                    var messageBytes = new byte[bytesReceived];
                    Buffer.BlockCopy(buffer, 0, messageBytes, 0, bytesReceived);

                    messageQueue.Enqueue((clientSocket, messageBytes, bytesReceived));

                    var message = Encoding.UTF8.GetString(messageBytes);
                    //Console.WriteLine($"Додано повідомлення до черги: {message} ({bytesReceived} байт)");
                }
                else
                {
                    Console.WriteLine("Клієнт закрив з'єднання.");
                    break;
                }
            }

            clientSocket.Close();
            Console.WriteLine("Процес сервера завершує свою роботу!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Сталася помилка: {ex.Message}");
        }
    }

    private static async Task ProcessMessages()
    {
        while (true)
        {
            if (messageQueue.TryDequeue(out var item))
            {
                var (clientSocket, data, length) = item;

                var message = Encoding.UTF8.GetString(data, 0, length);
                Console.WriteLine($"Процес клієнта надіслав повідомлення: {message}");
                var response = new string(message.ToArray());


                if (int.TryParse(response, out int number))
                {
                    number++; 
                    var responseBytes = Encoding.UTF8.GetBytes(number.ToString());
                    await clientSocket.SendAsync(responseBytes);
                }
                else
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await clientSocket.SendAsync(responseBytes);

                }
            }

            await Task.Delay(100); 
        }
    }
}
