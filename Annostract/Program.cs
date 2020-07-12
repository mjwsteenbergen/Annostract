using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Annostract
{
    interface Serializer
    {
        Task<string> Serialize(List<ExtractedSource> sources);
    }

    public interface Extractor
    {
        Task<ExtractedSource?> Extract();
    }

    public class ExtractedSource
    {
        public ExtractedSource(string name, List<ExtractedArticle> articles)
        {
            Name = name;
            Articles = articles;
            Bibliography = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Bibliography { get; set; }
        public List<ExtractedArticle> Articles { get; set; }
    }

    public class ExtractedArticle
    {
        public string? Abstract { get; set; }
        public string Path { get; set; }
        public List<Note> Notes { get; set; }

        public string Reference { get; set; }

        public ExtractedArticle(string path)
        {
            Notes = new List<Note>();
            Path = path;
            Reference = "";
            Name = "";
        }

        public string Name { get; internal set; }
    }
}



