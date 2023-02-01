using EAVFramework;
using EAVFramework.Endpoints;
using EAVFramework.Extensions;
using EAVFramework.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Linq;
using EAVFramework.Plugins;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using EAVFramework.Configuration;

namespace EAVFW.Extensions.Documents
{


    public static class EndpointExtensions
    {
        public static IEAVFrameworkBuilder AddDocumentPlugins<TContext,TDocument>(this IEAVFrameworkBuilder builder)
             where TContext : DynamicContext
            where TDocument : DynamicEntity, IDocumentEntity
        {
            builder.AddPlugin<SetDocumentNameOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentContentTypeOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 1, EntityPluginMode.Sync);

            builder.AddPlugin<SetDocumentNameOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Update, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentContentTypeOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Update, 1, EntityPluginMode.Sync);


            return builder;
        }
        public static IEAVFrameworkBuilder AddDocumentHashPlugins<TContext, TDocument>(this IEAVFrameworkBuilder builder)
            where TContext : DynamicContext
           where TDocument : DynamicEntity, IDocumentEntity,IDocumentEntityWithHash
        {
            builder.AddPlugin<CalculateAndSetDocumentHash<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentNameOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentContentTypeOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 1, EntityPluginMode.Sync);

            builder.AddPlugin<CalculateAndSetDocumentHash<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentNameOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Update, 0, EntityPluginMode.Sync);
            builder.AddPlugin<SetDocumentContentTypeOnCreate<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Update, 1, EntityPluginMode.Sync);

            
            return builder;
        }

        public static IEndpointRouteBuilder MapDocumentsApiEndpoints<TContext, TDocument>(this IEndpointRouteBuilder routes)
            where TContext : DynamicContext
            where TDocument : DynamicEntity, IDocumentEntity,new()
        {

            routes.MapPost("/api/containers/{containerName}/{files}/{**filename}", async r =>
            {
                var auth = await r.AuthenticateAsync("EasyAuth");
              //  var identity = Guid.Parse(auth.Principal.FindFirstValue("sub"));

                var context = r.RequestServices.GetRequiredService<EAVDBContext<TContext>>();
                var files = context.Set<TDocument>();
                var containerName = r.GetRouteValue("containerName") as string;
                var filename = r.GetRouteValue("filename")?.ToString()?.Trim('/');

                MemoryStream ms = new MemoryStream();
                await r.Request.BodyReader.AsStream().CopyToAsync(ms);

                var entry = await files.AddAsync(new TDocument
                {
                    Container = containerName,
                    Path = $"/{filename}",
                    Name = Path.GetFileName(filename),
                    Data = ms.ToArray(),
                    //ModifiedById = identity,
                    //CreatedById = identity,
                    //OwnerId = identity,
                    //CreatedOn = DateTime.UtcNow,
                    //ModifiedOn = DateTime.UtcNow
                });
                await context.SaveChangesAsync(auth.Principal);

                await r.Response.WriteJsonAsync(new { id = entry.CurrentValues.GetValue<Guid>("Id") });
            });


            routes.MapGet("/api/files/{fileid}", async r =>
            {
                var gzip = r.Request.Headers.TryGetValue("Accept-Encoding", out var compressionSupported) && compressionSupported.Contains("gzip", StringComparer.OrdinalIgnoreCase);

                var context = r.RequestServices.GetRequiredService<DynamicContext>();
                var files = context.Set<TDocument>();
                var fileId = Guid.Parse(r.GetRouteValue("fileid") as string);
                var file = await files.FindAsync(fileId);
                byte[] data = file.Data;

                if (file.Compressed??false && !gzip)
                {
                    using var ms = new MemoryStream(file.Data);
                    using var tinyStream = new GZipStream(ms, CompressionMode.Decompress);
                    using var msout = new MemoryStream();
                    await tinyStream.CopyToAsync(msout);
                    data = msout.ToArray();
                    gzip = false;
                }

                if (gzip)
                {
                    r.Response.Headers.Add("content-encoding", "gzip");
                }
                r.Response.Headers.Add("content-type", file.ContentType ?? "application/octet-stream");
                await r.Response.BodyWriter.WriteAsync(data);
            });

            return routes;
        }
    }
}