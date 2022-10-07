using EAVFramework;
using EAVFramework.Plugins;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;

namespace EAVFW.Extensions.Documents
{
    public class SetDocumentContentTypeOnCreate<TContext, TDocument> : IPlugin<TContext, TDocument>
         where TContext : DynamicContext
         where TDocument : DynamicEntity, IDocumentEntity
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new FileExtensionContentTypeProvider();

       

        public async Task Execute(PluginContext<TContext, TDocument> context)
        {


            if (!string.IsNullOrEmpty(context.Input.Name) && ContentTypeProvider.TryGetContentType(context.Input.Name, out var contentType))
                context.Input.ContentType ??= contentType;



        }
    }
}