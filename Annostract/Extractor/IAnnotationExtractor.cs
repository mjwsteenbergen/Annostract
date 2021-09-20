using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract;
using Martijn.Extensions.AsyncLinq;

namespace Annostract.Extractor
{
    public class AnnotationFolder 
    {
        public AnnotationFolder(string name)
        {
            Name = name;
            Files = new List<IAnnotatedDocument>();
            Folders = new List<AnnotationFolder>();
        }

        public string Name { get; set; }
        public List<IAnnotatedDocument> Files { get; set; }
        public List<AnnotationFolder> Folders { get; set; }

        public async Task<ExtractedFolder> Extract()
        {
            return new ExtractedFolder(Name) {
                Files = (await Files.Select(i => i.Extract()).WhenAll()).ToList(),
                Folders = (await Folders.Select(i => i.Extract()).WhenAll()).ToList()
            };
        }
    }

    public interface IAnnotatedDocument
    {
        Task<ExtractedDocument> Extract();
    }
}