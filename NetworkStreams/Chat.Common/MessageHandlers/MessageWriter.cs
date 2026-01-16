using System.Buffers.Binary;
using System.Text;
using Newtonsoft.Json;


namespace Chat.Common.MessageHandlers;


public class MessageWriter(Stream stream) : MessageHandler, IDisposable
{
    public async Task WriteMessage(MessageDTO message, CancellationToken ct)
    {
        var size = message.Content.Length;
        if (size > MaxMessageLen)
        {
            throw new TooLongMessageException("");
        }

        var serialized = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(serialized);
        var header = new byte[HeaderLen];
        BinaryPrimitives.WriteInt32BigEndian(header, bytes.Length);

        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(bytes, ct);
        await stream.FlushAsync(ct);
    }


    public void Dispose()
    {
        stream.Dispose();
    }
}
