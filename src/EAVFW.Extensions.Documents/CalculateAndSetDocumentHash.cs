using EAVFramework;
using EAVFramework.Plugins;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace EAVFW.Extensions.Documents
{
    public class CalculateAndSetDocumentHash<TContext, TDocument> : IPlugin<TContext, TDocument>
          where TContext : DynamicContext
         where TDocument : DynamicEntity, IDocumentEntityWithHash, IDocumentEntity
    {
        private readonly ILogger<CalculateAndSetDocumentHash<TContext, TDocument>> _logger;

        public CalculateAndSetDocumentHash(ILogger<CalculateAndSetDocumentHash<TContext, TDocument>> logger)
        {
            _logger = logger;
        }

        public async Task Execute(PluginContext<TContext, TDocument> context)
        {
            _logger.LogInformation("[{PluginName}] Document hash about to be calculated",
                nameof(CalculateAndSetDocumentHash<TContext, TDocument>));

            var documentEntry = context.DB.Context.Entry(context.Input);
            var dataProperty = documentEntry.Property(x => x.Data);

            if (dataProperty.OriginalValue == dataProperty.CurrentValue && documentEntry.State != Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                _logger.LogInformation("[{PluginName}] Document data have not changed",
                    nameof(CalculateAndSetDocumentHash<TContext, TDocument>));
                return;
            }

            var documentEntity = documentEntry.Entity;

            if (!documentEntity.Compressed ?? false)
            {
                _logger.LogInformation("[{PluginName}] Document not compressed",
                    nameof(CalculateAndSetDocumentHash<TContext, TDocument>));
                var md5Hash = MD5.Create().ComputeHash(documentEntity.Data);
                documentEntity.Hash = string.Join("", md5Hash.Select(x => x.ToString("X1")));  //Hash.CreateMD5(documentEntity.Data).ToString();
            }
            else
            {
                _logger.LogInformation("[{PluginName}] Document compressed - decompressing",
                    nameof(CalculateAndSetDocumentHash<TContext, TDocument>));

                var documentData = await DocumentHelpers.Decompress(documentEntity.Data);
                var documentDataAsByteArr = Encoding.UTF8.GetBytes(documentData);
                var md5Hash = MD5.Create().ComputeHash(documentDataAsByteArr);

                documentEntity.Hash = string.Join("", md5Hash.Select(x => x.ToString("X1")));
            }
        }
    }
}