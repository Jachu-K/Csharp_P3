using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Buffers;

namespace SensorMonitoringClient
{
    /// <summary>
    /// Dane z czujnika - struktura używana w systemie
    /// </summary>
    [Serializable]
    public class SensorData
    {
        public int SensorId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime MeasurementTime { get; set; }
        public bool IsActive { get; set; }
        public int BatteryLevel { get; set; } // 0-100%

        public override string ToString()
        {
            return $"Sensor {SensorId}: {Temperature}°C, {Humidity}%, " +
                   $"Battery: {BatteryLevel}%, Active: {IsActive}, " +
                   $"Time: {MeasurementTime:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Nagłówek wiadomości - zawiera metadane o przesyłanych danych
    /// </summary>
    public class MessageHeader
    {
        public int DataLength { get; set; }    // Długość danych w bajtach
        public MessageType Type { get; set; }  // Typ wiadomości
        public int Version { get; set; }       // Wersja protokołu
    }

    public enum MessageType
    {
        JsonData = 1,
        BinaryData = 2,
        Acknowledgement = 3,
        Error = 4
    }

    /// <summary>
    /// Główna klasa klienta TCP
    /// </summary>
    public class SensorTcpClient : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _isConnected;
        private bool _disposed;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// Inicjalizuje klienta TCP
        /// </summary>
        /// <param name="host">Adres IP lub nazwa hosta</param>
        /// <param name="port">Port TCP</param>
        public SensorTcpClient(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host nie może być pusty", nameof(host));
            
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port musi być w zakresie 1-65535");
            
            _host = host;
            _port = port;
            _tcpClient = new TcpClient();
            _tcpClient.NoDelay = true; // Wyłącz algorytm Nagle'a dla mniejszych opóźnień
        }

        /// <summary>
        /// Nawiązuje połączenie z serwerem
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_isConnected)
                return;
            
            Console.WriteLine($"Łączenie z {_host}:{_port}...");
            
            try
            {
                // Próba parsowania jako IP
                if (IPAddress.TryParse(_host, out IPAddress ipAddress))
                {
                    // Połączenie przez IP
                    await _tcpClient.ConnectAsync(ipAddress, _port);
                }
                else
                {
                    // Połączenie przez hostname (DNS resolution)
                    await _tcpClient.ConnectAsync(_host, _port);
                }
                
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                Console.WriteLine("Połączono pomyślnie!");
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"Nie można nawiązać połączenia z {_host}:{_port}. Błąd: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas łączenia: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Przetwarza dane z serwera
        /// </summary>
        public async Task ProcessDataAsync()
        {
            if (!_isConnected || _networkStream == null)
                throw new InvalidOperationException("Najpierw nawiąż połączenie!");
            
            Console.WriteLine("Rozpoczynam przetwarzanie danych...");
            
            try
            {
                // 1. Odbierz nagłówek wiadomości (4 bajty długości + 1 bajt typu)
                byte[] headerBuffer = await ReadExactlyAsync(5);
                
                // Parsuj nagłówek
                int dataLength = BitConverter.ToInt32(headerBuffer, 0);
                MessageType messageType = (MessageType)headerBuffer[4];
                
                Console.WriteLine($"Otrzymano nagłówek: Długość={dataLength}, Typ={messageType}");
                
                if (messageType != MessageType.JsonData)
                {
                    throw new InvalidOperationException($"Oczekiwano typu JsonData, otrzymano: {messageType}");
                }
                
                // 2. Odbierz dane JSON
                byte[] jsonBuffer = await ReadExactlyAsync(dataLength);
                string jsonData = Encoding.UTF8.GetString(jsonBuffer);
                
                Console.WriteLine($"Odebrano JSON ({dataLength} bajtów): {jsonData}");
                
                // 3. Deserializuj JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                
                var sensorData = JsonSerializer.Deserialize<SensorData>(jsonData, options);
                
                if (sensorData == null)
                {
                    throw new InvalidOperationException("Nie udało się deserializować danych JSON");
                }
                
                Console.WriteLine($"Deserializowano: {sensorData}");
                
                // 4. Konwertuj do formatu binarnego
                byte[] binaryData = ConvertToBinary(sensorData);
                
                // 5. Wyślij dane binarne z powrotem do serwera
                await SendBinaryDataAsync(binaryData);
                
                Console.WriteLine("Przetwarzanie zakończone pomyślnie!");
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Połączenie zostało zamknięte przez serwer.");
                _isConnected = false;
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas przetwarzania: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Konwertuje liczbę całkowitą na tablicę bajtów (big-endian)
        /// </summary>
        public byte[] ConvertIntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            
            // Upewnij się, że używamy big-endian (standard sieciowy)
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            
            return bytes;
        }

        /// <summary>
        /// Konwertuje tablicę bajtów na liczbę całkowitą (big-endian)
        /// </summary>
        public int ConvertBytesToInt(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            
            if (bytes.Length != 4)
                throw new ArgumentException("Tablica musi mieć dokładnie 4 bajty", nameof(bytes));
            
            byte[] workingBytes = new byte[4];
            Array.Copy(bytes, workingBytes, 4);
            
            // Konwertuj z big-endian jeśli system jest little-endian
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(workingBytes);
            }
            
            return BitConverter.ToInt32(workingBytes, 0);
        }

        /// <summary>
        /// Odczytywanie dokładnej liczby bajtów z strumienia
        /// </summary>
        private async Task<byte[]> ReadExactlyAsync(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Liczba bajtów musi być większa od 0");
            
            var buffer = new byte[count];
            int totalRead = 0;
            
            while (totalRead < count)
            {
                int read = await _networkStream.ReadAsync(buffer, totalRead, count - totalRead);
                
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        $"Osiągnięto koniec strumienia po odczytaniu {totalRead} z {count} bajtów");
                }
                
                totalRead += read;
            }
            
            return buffer;
        }

        /// <summary>
        /// Wysyła dane binarne do serwera
        /// </summary>
        private async Task SendBinaryDataAsync(byte[] binaryData)
        {
            if (binaryData == null || binaryData.Length == 0)
                throw new ArgumentException("Dane nie mogą być puste", nameof(binaryData));
            
            // Nagłówek: długość danych (4 bajty) + typ wiadomości (1 bajt)
            byte[] lengthBytes = ConvertIntToBytes(binaryData.Length);
            byte[] header = new byte[5];
            
            Array.Copy(lengthBytes, 0, header, 0, 4);
            header[4] = (byte)MessageType.BinaryData;
            
            // Wyślij nagłówek
            await _networkStream.WriteAsync(header, 0, header.Length);
            
            // Wyślij dane
            await _networkStream.WriteAsync(binaryData, 0, binaryData.Length);
            await _networkStream.FlushAsync();
            
            Console.WriteLine($"Wysłano dane binarne: {binaryData.Length} bajtów");
        }

        /// <summary>
        /// Konwertuje obiekt SensorData do formatu binarnego
        /// Format: [SensorId:4b][Temperature:8b][Humidity:8b][Timestamp:8b][IsActive:1b][BatteryLevel:1b]
        /// </summary>
        public byte[] ConvertToBinary(SensorData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // SensorId - 4 bajty
                writer.Write(ConvertIntToBytes(data.SensorId));
                
                // Temperature - 8 bajtów (double)
                byte[] tempBytes = BitConverter.GetBytes(data.Temperature);
                if (BitConverter.IsLittleEndian) Array.Reverse(tempBytes);
                writer.Write(tempBytes);
                
                // Humidity - 8 bajtów (double)
                byte[] humidityBytes = BitConverter.GetBytes(data.Humidity);
                if (BitConverter.IsLittleEndian) Array.Reverse(humidityBytes);
                writer.Write(humidityBytes);
                
                // Timestamp - 8 bajtów (DateTime jako ticks)
                long ticks = data.MeasurementTime.ToUniversalTime().Ticks;
                byte[] timestampBytes = BitConverter.GetBytes(ticks);
                if (BitConverter.IsLittleEndian) Array.Reverse(timestampBytes);
                writer.Write(timestampBytes);
                
                // IsActive - 1 bajt (bool jako byte)
                writer.Write((byte)(data.IsActive ? 1 : 0));
                
                // BatteryLevel - 1 bajt
                if (data.BatteryLevel < 0 || data.BatteryLevel > 100)
                    throw new ArgumentOutOfRangeException(nameof(data.BatteryLevel), "BatteryLevel musi być w zakresie 0-100");
                
                writer.Write((byte)data.BatteryLevel);
                
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializuje dane binarne do obiektu SensorData
        /// </summary>
        public SensorData ConvertFromBinary(byte[] binaryData)
        {
            if (binaryData == null)
                throw new ArgumentNullException(nameof(binaryData));
            
            if (binaryData.Length < 30) // Minimalny rozmiar: 4+8+8+8+1+1 = 30 bajtów
                throw new ArgumentException("Dane binarne są za krótkie", nameof(binaryData));
            
            using (var memoryStream = new MemoryStream(binaryData))
            using (var reader = new BinaryReader(memoryStream))
            {
                // SensorId - 4 bajty
                byte[] sensorIdBytes = reader.ReadBytes(4);
                int sensorId = ConvertBytesToInt(sensorIdBytes);
                
                // Temperature - 8 bajtów
                byte[] tempBytes = reader.ReadBytes(8);
                if (BitConverter.IsLittleEndian) Array.Reverse(tempBytes);
                double temperature = BitConverter.ToDouble(tempBytes, 0);
                
                // Humidity - 8 bajtów
                byte[] humidityBytes = reader.ReadBytes(8);
                if (BitConverter.IsLittleEndian) Array.Reverse(humidityBytes);
                double humidity = BitConverter.ToDouble(humidityBytes, 0);
                
                // Timestamp - 8 bajtów
                byte[] timestampBytes = reader.ReadBytes(8);
                if (BitConverter.IsLittleEndian) Array.Reverse(timestampBytes);
                long ticks = BitConverter.ToInt64(timestampBytes, 0);
                DateTime timestamp = new DateTime(ticks, DateTimeKind.Utc);
                
                // IsActive - 1 bajt
                byte isActiveByte = reader.ReadByte();
                bool isActive = isActiveByte != 0;
                
                // BatteryLevel - 1 bajt
                byte batteryLevelByte = reader.ReadByte();
                int batteryLevel = batteryLevelByte;
                
                return new SensorData
                {
                    SensorId = sensorId,
                    Temperature = temperature,
                    Humidity = humidity,
                    MeasurementTime = timestamp,
                    IsActive = isActive,
                    BatteryLevel = batteryLevel
                };
            }
        }

        /// <summary>
        /// Odbiera i przetwarza wiele pakietów danych
        /// </summary>
        public async Task ProcessMultiplePacketsAsync(int packetCount)
        {
            for (int i = 0; i < packetCount; i++)
            {
                try
                {
                    Console.WriteLine($"\n--- Przetwarzanie pakietu {i + 1}/{packetCount} ---");
                    await ProcessDataAsync();
                    
                    // Małe opóźnienie między pakietami
                    if (i < packetCount - 1)
                        await Task.Delay(500);
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("Serwer zamknął połączenie.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas przetwarzania pakietu {i + 1}: {ex.Message}");
                    break;
                }
            }
        }

        /// <summary>
        /// Rozłącza klienta
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected)
                return;
            
            try
            {
                _isConnected = false;
                
                // Wyślij komunikat o rozłączeniu
                if (_networkStream != null && _networkStream.CanWrite)
                {
                    byte[] disconnectMsg = Encoding.UTF8.GetBytes("DISCONNECT");
                    _networkStream.Write(disconnectMsg, 0, disconnectMsg.Length);
                    _networkStream.Flush();
                }
            }
            catch
            {
                // Ignoruj błędy przy rozłączaniu
            }
            finally
            {
                _networkStream?.Close();
                _tcpClient?.Close();
                Console.WriteLine("Rozłączono");
            }
        }

        /// <summary>
        /// Testuje konwersję int ↔ bytes
        /// </summary>
        public void TestConversion()
        {
            Console.WriteLine("\n=== Test konwersji ===");
            
            int[] testValues = { 0, 1, -1, 255, 65535, 123456789, int.MaxValue, int.MinValue };
            
            foreach (int value in testValues)
            {
                byte[] bytes = ConvertIntToBytes(value);
                int converted = ConvertBytesToInt(bytes);
                
                bool success = value == converted;
                Console.WriteLine($"{value} -> {BitConverter.ToString(bytes)} -> {converted} : {(success ? "✓" : "✗")}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                    _networkStream?.Dispose();
                    _tcpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Program główny
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Klient monitoringu czujników ===");
            
            // Konfiguracja połączenia
            string host = "localhost"; // Można zmienić na IP: "127.0.0.1"
            int port = 8888;
            
            if (args.Length >= 1) host = args[0];
            if (args.Length >= 2) int.TryParse(args[1], out port);
            
            using var client = new SensorTcpClient(host, port);
            
            try
            {
                // Test konwersji
                client.TestConversion();
                
                // 1. Nawiąż połączenie
                await client.ConnectAsync();
                
                // 2. Przetwarzaj dane (w pętli dla wielu pakietów)
                await client.ProcessMultiplePacketsAsync(3);
                
                Console.WriteLine("\n✅ Wszystkie dane przetworzone pomyślnie!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Błąd: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("\nNaciśnij dowolny klawisz, aby zakończyć...");
                Console.ReadKey();
            }
        }
    }
}