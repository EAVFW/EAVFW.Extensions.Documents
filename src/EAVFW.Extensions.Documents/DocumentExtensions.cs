using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;

namespace EAVFW.Extensions.Documents
{
    public static class DocumentExtensions
    {
        public static JToken ReadDocumentAsync(this IDocumentEntity document)
        {
            var ms = new MemoryStream(document.Data) as Stream;

            if (document.Compressed ?? false)
            {
                ms = new GZipStream(ms, CompressionMode.Decompress, false);
            }


            return JToken.ReadFrom(new JsonTextReader(new StreamReader(ms)));
        }
        public static void WriteDocumentData(this IDocumentEntity document, JToken data)
        {
            var ms = new MemoryStream();



            data.WriteTo(new JsonTextWriter(new StreamWriter(document.Compressed == true ? new GZipStream(ms, CompressionMode.Compress, false) : ms as Stream)));



            document.Data = ms.ToArray();
        }

    }
}