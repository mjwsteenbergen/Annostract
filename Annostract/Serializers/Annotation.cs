using System;
using System.Linq;
using Annostract.Extractor;
using Martijn.Extensions.Linq;

namespace Annostract.Serializer
{
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

    public abstract class Annotation
    {
        public virtual string Type => this.GetType().Name;
        public abstract string ToSummary(string indent = "");

        public static Annotation Convert(Result res)
        {
            return res switch
            {
                HighlightResult high => Convert(high),
                ImageResult imageResult => new ImageAnnotation(imageResult.Url),
                _ => throw new Exception($"Invalid result: {res?.ToString()}")
            };
        }

        public static Annotation Convert(HighlightResult res)
        {
            if (res.HighlightedText == null)
            {
                res.HighlightedText = "ERROR: THERE SHOULD BE TEXT HERE";
            }

            res.HighlightedText = res.HighlightedText.Replace("- ", "");

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

    public class HighlightAnnotation : Annotation
    {
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

        public override string ToSummary(string indent = "")
        {
            var res = $"{indent} - \"{HighlightedText}\"\n";
            res += Content.Split('\n').Select(i => $"{indent}    - {i.Trim()}").CombineWithNewLine();
            return res;
        }
    }
}