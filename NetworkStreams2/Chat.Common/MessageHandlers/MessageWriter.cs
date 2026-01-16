using System.Buffers.Binary;
using System.Text;
using Newtonsoft.Json;


namespace Chat.Common.MessageHandlers;


public class MessageWriter(Stream stream) : MessageHandler, IDisposable
{
    public async Task WriteMessage(MessageDTO message, CancellationToken ct)
    {
        var serialized = JsonConvert.SerializeObject(message);
        var size = Encoding.UTF8.GetByteCount(serialized);
        var buffer = Encoding.UTF8.GetBytes(serialized);
        if (size > MaxMessageLen)
        {
            throw new TooLongMessageException($"Rozmiar wiadomości : {size} większy od dopuszczalnego : {MaxMessageLen}\n");
        }

        var sizeBuff = BitConverter.GetBytes(size);
        await stream.WriteAsync(sizeBuff,ct);
        await stream.WriteAsync(buffer, ct);
    }


    public void Dispose()
    {
        stream.Dispose();
    }
}
