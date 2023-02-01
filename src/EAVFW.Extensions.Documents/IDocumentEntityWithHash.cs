using EAVFramework.Shared;

namespace EAVFW.Extensions.Documents
{
    [EntityInterface(EntityKey = "Document")]
    public interface IDocumentEntityWithHash
    {
        public string Hash { get; set; }
    }

    
}