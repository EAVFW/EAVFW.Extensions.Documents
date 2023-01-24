using System.IO;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text.Json;

namespace EAVFW.Extensions.Documents
{
    public static class DocumentHelpers
    {
        /// <summary>
        /// Decompress gzipped json document
        /// </summary>
        /// <param name="compressedData">Gzipped Json</param>
        /// <returns>JToken</returns>
        public static async Task<JToken> DecompressJsonDocument(byte[] compressedData)
        {
            using var memoryStreamIn = new MemoryStream(compressedData);
            await using var decompressor = new GZipStream(memoryStreamIn, CompressionMode.Decompress);

            using var textReader = new StreamReader(decompressor);
            using var jsonReader = new JsonTextReader(textReader);

            return await JToken.LoadAsync(jsonReader);
        }

        /// <summary>
        /// Decompress gzipped to simple string
        /// </summary>
        /// <param name="compressedData">Gzipped text</param>
        /// <returns>String</returns>
        public static async Task<string> Decompress(byte[] compressedData)
        {
            using var memoryStreamIn = new MemoryStream(compressedData);
            await using var decompressor = new GZipStream(memoryStreamIn, CompressionMode.Decompress);

            using var textReader = new StreamReader(decompressor);
            return await textReader.ReadToEndAsync();
        }

        /// <summary>
        /// Compress json document to gzipped byte array
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static async Task<byte[]> CompressJsonDocument(JToken manifest)
        {
            var a = Encoding.UTF8.GetBytes(manifest.ToString());

            using (var data = new MemoryStream())
            {
                using (var stream = new GZipStream(data, CompressionMode.Compress))
                {
                    await stream.WriteAsync(a, 0, a.Length);
                }


                return data.ToArray();
            }
        }

        /// <summary>
        /// Compress json document to gzipped byte array
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static async Task<byte[]> CompressJsonDocument(JsonDocument manifest)
        {
            using var mem = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(mem, new JsonWriterOptions { Indented = true });
            manifest.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync();
            var a = mem.ToArray();

            using (var data = new MemoryStream())
            {
                using (var stream = new GZipStream(data, CompressionMode.Compress))
                {
                    await stream.WriteAsync(a, 0, a.Length);
                }

                return data.ToArray();
            }
        }

        /// <summary>
        /// Compress json document to gzipped byte array
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static async Task<byte[]> CompressText(string text)
        {
            var a = Encoding.UTF8.GetBytes(text);

            using (var data = new MemoryStream())
            {
                using (var stream = new GZipStream(data, CompressionMode.Compress))
                {
                    await stream.WriteAsync(a, 0, a.Length);
                }


                return data.ToArray();
            }
        }

        public static async Task<byte[]> CompressJson2(JToken jToken)
        {
            var bytes = Encoding.UTF8.GetBytes(jToken.ToString());

            using var inputStream = new MemoryStream(bytes);
            using var outputStream = new MemoryStream();
            await using var gs = new GZipStream(outputStream, CompressionMode.Compress);

            await inputStream.CopyToAsync(gs);
            await gs.FlushAsync();
            await outputStream.FlushAsync(); // Den her tilføjede vi

            return outputStream.ToArray();
        }
    }
}