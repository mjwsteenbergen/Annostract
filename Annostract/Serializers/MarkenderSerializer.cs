using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Martijn.Extensions.Text;
using static Annostract.PaperFinder.PaperFinder;

namespace Annostract.Serializer {
    public class MarkenderSerializer
    {
        public MarkenderSerializer()
        {
            UnreadLocalDocs = new List<ExtractedDocument>();
            ToReads = new List<ToRead>();
        }

        public List<ExtractedDocument> UnreadLocalDocs { get; set; }
        public List<ToRead> ToReads { get; set; }

        public async Task Serialize(IEnumerable<ExtractedFolder> folders, AnnostractSettings settings)
        {
            var res = "# Annostract Annotations";
            res += (await folders.Select(i => Serialize(i, 1)).WhenAll()).CombineWithNewLine();
            res += "\n";

            res += "# Papers to read";
            res += ToReads.Select(i => i.ToSummary()).CombineWithNewLine();

            res += "# Unread papers";
            res += UnreadLocalDocs.Select(i => SerializeToRead(i)).CombineWithNewLine();

            Console.WriteLine(res);
        }

        public virtual string SerializeToRead(ExtractedDocument i)
        {
            return i switch
            {
                ExtractedFile f => $" - {f.Title} {f.FilePathString}",
                ExtractedInstapaperArticle ins => $" - [{ins.Title}]({ins.Url})",
                _ => throw new ArgumentException()
            };
        }

        public virtual async Task<string> Serialize(ExtractedFolder folder, int headerLevel)
        {
            foreach (var file in folder.Files)
            {
                var result = await FindPaper(file.Title);
            }

            var res = $"## {folder.Name}\n\n";
            res += folder.Files.Select(i => Serialize(i, headerLevel + 1)).CombineWithNewLine();
            res += (await folder.Folders.Select(i => Serialize(i, headerLevel + 1)).WhenAll()).CombineWithNewLine();
            res += "\n";
            return res;
        }

        public virtual string Serialize(ExtractedDocument document, int headerLevel)
        {
            if (document.Results.Count == 0)
            {
                UnreadLocalDocs.Add(document);
                return "";
            }

            var res = $"### {document.Title}";

            res += Serialize(document.Results);

            return res + "\n";
        }

        public virtual string Serialize(List<Result> results)
        {
            var res = "";
            var annos = results.Select(i => Annotation.Convert(i)).ToList();

            int indent = 0;
            foreach (var i in annos)
            {
                if (i is HighlightAnnotation || i is Note || i is Indented || i is IndentChange || i is QuickIndent)
                {
                    string indentString = "   ".Repeat(indent);

                    if (i is IndentChange change)
                    {
                        indent = change.Execute(indent);
                    }

                    res += i.ToSummary(indentString) + "\n";
                }
            }

            var images = annos.OfType<ImageAnnotation>().Distinct().ToList();

            if (images.Count > 0)
            {
                res += $"\n### Images";

                images.ForEach(i => res += i.ToSummary());
            }

            ToReads.AddRange(annos.OfType<ToRead>());

            return res;
        }
    }
}