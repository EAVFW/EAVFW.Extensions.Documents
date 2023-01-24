using EAVFramework;
using System.IO;
using EAVFramework.Plugins;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text.Json;

namespace EAVFW.Extensions.Documents
{
    public class SetDocumentNameOnCreate<TContext, TDocument> : IPlugin<TContext, TDocument>
         where TContext : DynamicContext
         where TDocument : DynamicEntity, IDocumentEntity
    {
        
        

        public async Task Execute(PluginContext<TContext, TDocument> context)
        {

            if (!string.IsNullOrEmpty(context.Input.Path))
                context.Input.Name ??= Path.GetFileName(context.Input.Path);



        }
    }
}