using EAVFramework.Shared;
using System;

namespace EAVFW.Extensions.Documents
{
    [EntityInterface(EntityKey = "Document")]
    public interface IDocumentEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Container { get; set; }
        public string Path { get; set; }
        public byte[] Data { get; set; }
        public bool? Compressed { get; set; }
        public string ContentType { get; set; }
        public int? Size { get; set; }
    }

    
}