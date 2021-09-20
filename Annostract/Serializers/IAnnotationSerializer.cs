using System.Collections.Generic;
using System.Threading.Tasks;

namespace Annostract.Serializer
{
    public interface IAnnotationSerializer
    {
        Task Serialize(IEnumerable<ExtractedFolder> folders);
    }

    public enum OutputMethod 
    {
        Console, File
    }
}