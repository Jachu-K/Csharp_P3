using System.Buffers.Binary;
using System.Text;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Buffers;


namespace Chat.Common.MessageHandlers;


public class MessageReader(Stream stream) : MessageHandler, IDisposable
{
    public async Task<MessageDTO?> ReadMessage(CancellationToken ct)
    {
        var size = new byte[4];
        await stream.ReadExactlyAsync(size,ct);
        int sizeInt = BitConverter.ToInt32(size);
        if (sizeInt > MaxMessageLen)
        {
            throw new TooLongMessageException($"Rozmiar wiadomości : {sizeInt} większy od dopuszczalnego : {MaxMessageLen}\n");
        }
        var mess = new byte[sizeInt];
        await stream.ReadExactlyAsync(mess, ct);
        try
        {
            var json = Encoding.UTF8.GetString(mess);
            var message = JsonConvert.DeserializeObject<MessageDTO>(json);
        
            if (message == null) throw new InvalidMessageException("Deserializacja zwróciła null");
        
            return message;
        }
        catch (Exception ex)
        {
            throw new InvalidMessageException($"Błąd deserializacji: {ex.Message}");
        }
    }


    public void Dispose()
    {
        stream.Dispose();
    }
}