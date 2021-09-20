using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract.Extractor;
using Annostract.Serializer;
using Martijn.Extensions.AsyncLinq;

namespace Annostract
{
    public class Annostract {

        public async Task Run(AnnostractSettings settings)
        {
            var folders = await settings.Path.Select(i => i.Extract()).WhenAll();
            await settings.Serializer.Serialize(folders);
        }
    }

    public class AnnostractSettings {
        public AnnostractSettings(List<AnnotationFolder> path, bool writeToFile, IAnnotationSerializer serializer)
        {
            Path = path;
            WriteToFile = writeToFile;
            Serializer = serializer;
        }

        public List<AnnotationFolder> Path { get; set; }
        public bool WriteToFile { get; set; }

        public IAnnotationSerializer Serializer { get; set; }
    }
}