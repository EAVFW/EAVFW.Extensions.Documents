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
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using EAVFramework.Endpoints.Results;
using Microsoft.Net.Http.Headers;

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

            builder.AddPlugin<CalculateAndSetDocumentHash<TContext, TDocument>, TContext, TDocument>(EntityPluginExecution.PreValidate, EntityPluginOperation.Update, 0, EntityPluginMode.Sync);
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
                var context = r.RequestServices.GetRequiredService<DynamicContext>();
                var files = context.Set<TDocument>();
                var fileId = Guid.Parse(r.GetRouteValue("fileid") as string);
                var file = await files.FindAsync(fileId);
                byte[] data = file.Data;

                var canGzip = r.Request.Headers.TryGetValue("Accept-Encoding", out var compressionSupported) &&
                    compressionSupported.Contains("gzip", StringComparer.OrdinalIgnoreCase);

                if (file.Compressed ?? false && !canGzip)
                {
                    using var ms = new MemoryStream(file.Data);
                    using var tinyStream = new GZipStream(ms, CompressionMode.Decompress);
                    using var msout = new MemoryStream();
                    await tinyStream.CopyToAsync(msout);
                    data = msout.ToArray();
                    canGzip = false;
                }

                if (canGzip && (file.Compressed ?? false))
                {
                    r.Response.Headers.Add("content-encoding", "gzip");
                }


                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.SetHttpFileName(file.Name);
                r.Response.Headers["Content-Disposition"] = contentDisposition.ToString();
                r.Response.Headers.Add("content-type", file.ContentType ?? "application/octet-stream");
                r.Response.Headers.Add("X-Content-Type-Options", "nosniff");


                await r.Response.BodyWriter.WriteAsync(data);
            });

            routes.MapPost("/api/files", async context =>
            {
                var db = context.RequestServices.GetRequiredService<EAVDBContext<DynamicContext>>();
                var form = context.Request.Form;
                if (context.Request.HasFormContentType && form.Files.Any())
                {
                    var uploadedFiles = new List<EntityEntry<TDocument>>();
                    foreach (var file in form.Files)
                    {
                        var compressed = false;
                        if (form.TryGetValue("compressed", out var value) && !string.IsNullOrWhiteSpace(value))
                        {
                            compressed = bool.Parse(value);
                        }

                        using var ms = new MemoryStream();
                        if (!compressed)
                        {
                            await using var gzip = new GZipStream(ms, CompressionMode.Compress);
                            await file.CopyToAsync(gzip);
                            await gzip.FlushAsync();
                        }
                        else
                        {
                            await file.CopyToAsync(ms);
                        }

                        uploadedFiles.Add(db.Context.Add(new TDocument
                        {
                            Name = file.FileName,
                            Path = form["prefix"] + file.FileName,
                            Data = ms.ToArray(),
                            Compressed = true,
                            Container = form["container"],
                            ContentType = file.ContentType,
                            Size = (int)ms.Length,
                        }));

                        await db.SaveChangesAsync(context.User);
                    }
                    await new DataEndpointResult(new
                    {
                        value = uploadedFiles.Select(c => new
                        {
                            id = c.Entity.Id,
                            name = c.Entity.Name,
                            path = c.Entity.Path,
                            compressed = c.Entity.Compressed,
                            size = c.Entity.Size,
                            contenttype = c.Entity.ContentType
                        })
                    }).ExecuteAsync(context);

                    //await context.Response.WriteAsJsonAsync(new
                    //{
                    //    value = uploadedFiles.Select(c => new
                    //    {
                    //        id = c.Entity.Id,
                    //        name = c.Entity.Name,
                    //        path = c.Entity.Path,
                    //        compressed = c.Entity.Compressed,
                    //        size = c.Entity.Size,
                    //        contenttype = c.Entity.ContentType
                    //    })
                    //});
                }
            }).WithMetadata(new AuthorizeAttribute("EAVAuthorizationPolicy"));

            return routes;
        }
    }
}