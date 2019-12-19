using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Martijn.Extensions.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Annostract.PaperFinder.Crossref;

namespace Annostract
{
    public static class AnnoSerializer
    {
        public static string GetHeader(int indentLevel) {
            string s = "";

            for (int i = 0; i < indentLevel; i++)
            {
                s += "#";
            }

            return s;
        }

        public static async Task<StringBuilder> Serialize(ExtractedFile file, List<ToRead> reads) {
            var annotations = file.Results.Select(i => Convert(i)).ToList();

            StringBuilder builder = new StringBuilder();

            var paper = await PaperFinder.PaperFinder.Find(file.FileName);

            builder.Append($"## {file.FileName}");

            if (!string.IsNullOrEmpty(paper?.Doi))
            {
                builder.Append($" [DOI](http://dx.doi.org/{paper.Doi})");
            }
            builder.AppendLine();

            var published = paper?.PublishedPrint?.DateParts?.FirstOrDefault()?.FirstOrDefault().ToString();
            if (!string.IsNullOrEmpty(published))
            {
                builder.AppendLine($"**Published:** {published}  ");
            }

            var conference = paper?.Event?.Name;
            if (!string.IsNullOrEmpty(conference))
            {
                builder.AppendLine($"**Conference:** {conference}  ");
            }

            builder.AppendLine();

            string abs = annotations.OfType<Abstract>().Select(i => i.ToSummary("")).CombineWithNewLine();
            if(!string.IsNullOrEmpty(abs))
            {
                builder.AppendLine(abs);
            }

            builder.AppendLine($"\n### Takeaways");


            int indent = 0;
            string notes = "";
            foreach (var i in annotations)
            {
                if(i is HighlightAnnotation || i is Note || i is Indented|| i is IndentChange|| i is QuickIndent) {
                    string indentString = "   ".Repeat(indent);

                    if(i is IndentChange change) {
                        indent = change.Execute(indent);
                    }

                    notes += i.ToSummary(indentString) + "\n";
                }
            }

            if (!string.IsNullOrEmpty(notes))
            {
                builder.AppendLine(notes);
            }

            var images = annotations.OfType<ImageAnnotation>().Distinct().ToList();

            if (images.Count > 0)
            {
                builder.AppendLine($"\n### Images");

                images.ForEach(i => builder.Append(i.ToSummary()));
            }

            reads.AddRange(annotations.OfType<ToRead>());

            builder.AppendLine();

            return builder;
        }

        public static async Task<string> Serialize(List<ExtractedFile> extractedFiles, string originalPath)
        {
            var notHighlighted = extractedFiles.Where(i => i.Results.Count == 0).ToList();

            var filesGroup = extractedFiles.Where(i => i.Results.Count != 0).GroupBy(i => i.FilePath.Directory.Name);

            List<ToRead> reads = new List<ToRead>();

            StringBuilder res = new StringBuilder();
            foreach (var files in filesGroup)
            {
                if(files.Key == originalPath) {
                    res.AppendLine("# Literature Review");
                } else {
                    res.AppendLine($"\n# {files.Key.Replace(originalPath, "")}\n");
                }

                foreach(var file in files) {
                    res.Append(await Serialize(file, reads));
                }

            }

            res.AppendLine();
            res.AppendLine("# Still to read\n" + notHighlighted.Select(i => $"- {i.FilePath.FullName.Replace(originalPath, "").Replace("[", "\\[")}").CombineWithNewLine());
            res.AppendLine();

            if(reads.Count > 0) {
                res.AppendLine("# Papers to read");
            }

            var crPapers = new List<CrossRefSearchResult>();
            foreach (var file in extractedFiles)
            {
                crPapers.Add(await PaperFinder.PaperFinder.Find(file.FileName));
            }

            List<CrossRefSearchResult> papersToRead = new List<CrossRefSearchResult>();

            foreach (var read in reads)
            {
                if (Regex.IsMatch(read.Paper, @"(http(s)?:\/\/.)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)"))
                {
                    res.AppendLine(" ".Repeat(2) + $" - {read.Paper.Replace(" ", "")}".TrimEnd(new char[] { ' ', '.'}));
                    
                    continue;
                }
                
                papersToRead.Add(await PaperFinder.PaperFinder.Find(read.Paper));                
            }

            foreach(var paper in papersToRead.Distinct().Where(paper => !crPapers.Any(i => i?.Doi == paper?.Doi)).OrderByDescending(i => i.IsReferencedByCount)) {
                string url = paper.Link.FirstOrDefault(i => i.IsPdf())?.Url?.AbsoluteUri ?? $"http://dx.doi.org/{paper.Doi}" ;

                res.AppendLine(" ".Repeat(2) + $" - [{paper.Title?.CombineWithSpace() + " " + paper.Subtitle?.CombineWithSpace()}]({url}) ({paper.Created?.DateTime.Year}, {paper.IsReferencedByCount})");

                if (paper.Abstract != null)
                {
                    res.AppendLine(" ".Repeat(4) + $" - {paper.Abstract}");
                }
            }

            return res.ToString();
        }

        public static Annotation Convert(Result res) {
            return res switch {
                HighlightResult high => Convert(high),
                ImageResult imageResult => new ImageAnnotation(imageResult.Url),
                _ => throw new Exception($"Invalid result: {res?.ToString()}")
            };
        } 

        public static Annotation Convert(HighlightResult res) {
            if (res.HighlightedText == null)
            {
                res.HighlightedText = "ERROR: THERE SHOULD BE TEXT HERE";
            }

            res.HighlightedText = res.HighlightedText.Replace("[", "\\[").Replace("- ", "");

            return (res.Note?.ToLower().Trim().Replace(" ", ""), res.HighlightColor) switch
            {
                ("read", _) => new ToRead(res.HighlightedText),
                ("toread", _) => new ToRead(res.HighlightedText),
                (_, HighlightColor.Blue) => new ToRead(res.HighlightedText),
                ("abstract", _) => new Abstract(res.HighlightedText),
                ("year", _) => new Year(res.HighlightedText),
                ("doi", _) => new DoiLink(res.HighlightedText),
                ("-", _) => new Indented(res.HighlightedText),
                (">", _) => new IndentChange(res.HighlightedText, (i) => i + 1),
                ("<", _) => new IndentChange(res.HighlightedText, (i) => i - 1),
                ("<<", _) => new IndentChange(res.HighlightedText, (i) => i - 2),
                ("<>", _) => new QuickIndent(res.HighlightedText),
                (null, _) => new HighlightAnnotation(res.HighlightedText),
                (_, _) => new Note(res.HighlightedText, res.Note)
            };
        }

    }

    public class ImageAnnotation : Annotation
    {
        public ImageAnnotation(string url)
        {
            Url = url;
        }

        public string Url { get; set; }

        public override string Type => base.Type;

        public override bool Equals(object obj)
        {
            return (obj as ImageAnnotation)?.Url == this.Url;
        }

        public override int GetHashCode()
        {
            return this.Url?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override string ToSummary(string indent = "")
        {
            return $"![]({Url})\n";
        }
    }

    internal class IndentChange : Annotation
    {
        private readonly string highlightedText;
        private Func<int, int> p;

        public IndentChange(string highlightedText, Func<int, int> p)
        {
            this.highlightedText = highlightedText;
            this.p = p;
        }

        public override string ToSummary(string indent = "")
        {
            return $"{indent} - \"{highlightedText}\"";
        }

        internal int Execute(int indent)
        {
            return p(indent);
        }
    }

    public class Attribute : Annotation 
    {
        public Attribute(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        public override string ToSummary(string indent = "")
        {
            return indent + Content.Trim();
        }
    }

    public class QuickIndent : Annotation
    {
        public QuickIndent(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        public override string ToSummary(string indent = "")
        {
            return $"{indent.Substring(0, indent.Length >= 3 ? indent.Length - 3 : indent.Length)} - \"{Content.Trim()}\"";
        }
    }

    public class Abstract : Attribute
    {
        public Abstract(string content) : base(content)
        {
        }
    }

    public class DoiLink : Attribute
    {
        public DoiLink(string content) : base(content)
        {
        }
    }

    public class Year : Attribute
    {
        public Year(string content) : base(content)
        {
        }
    }

    public class Indented : Annotation
    {
        public Indented(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        public override string ToSummary(string indent = "")
        {
            return $"{indent}    - \"{Content.Trim()}\"";
        }
    }

    public abstract class Annotation {
        public virtual string Type => this.GetType().Name;
        public abstract string ToSummary(string indent = "");
    }

    public class HighlightAnnotation : Annotation {
        public HighlightAnnotation(string highlightedText)
        {
            HighlightedText = highlightedText;
        }

        public string HighlightedText { get; set; }

        public override string ToSummary(string indent = "")
        {
            return $"{indent} - \"{HighlightedText.Replace("\n", "").Trim()}\"";
        }
    }

    

    public class ToRead : Annotation
    {
        public ToRead(string paper)
        {
            Paper = paper;
        }

        public string Paper { get; set; }

        public override string ToSummary(string indent = "")
        {
            return Paper;
        }
    }

    public class Note : HighlightAnnotation
    {
        public Note(string highlightedText, string content) : base(highlightedText)
        {
            Content = content;
        }

        public string Content { get; set; }

        public override string ToSummary(string indent = "") {
            var res = $"{indent} - \"{HighlightedText}\"\n";
            res += Content.Split('\n').Select(i => $"{indent}    - {i.Trim()}").CombineWithNewLine();
            return res;
        }
    }
}