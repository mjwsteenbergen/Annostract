using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annostract.PaperFinders;
using Annostract.PaperFinders.Crossref;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Martijn.Extensions.Text;
using static Annostract.PaperFinders.BibtexMaker;

namespace Annostract
{
    public class MarkdownSerializer : Serializer
    {

        public Task<string> Serialize(List<ExtractedSource> sources)
        {
            string result = "# Annostract\n\n";
            var content = sources.Select(i => Serialize(i)).CombineWithNewLine();
            result += content;
            result += "\n\n## Documents to read\n" + sources.SelectMany(i => i.Articles).Where(i => i.Notes.Count == 0).Select(i => $" - <a href=\"{i.Path}\">{i.Name}<a/>").CombineWithNewLine();
            return Task.FromResult(result);
        }

        public virtual string Serialize(ExtractedSource source)
        {
            var result = $"# {source.Name}\n";
            var reviews = source.Articles.Where(i => i.Notes.Count > 0).Select(i => Serialize(i)).CombineWithNewLine();
            result += reviews;
            return result;
        }



        public virtual string Serialize(ExtractedArticle article)
        {
            string abstrac = "";
            var abs = article.Notes.OfType<Abstract>().FirstOrDefault();
            if (abs != null)
            {
                abstrac = "\n" + abs.Content;
            }

            return $"## {article.Name.EscapeMarkdown()} {abstrac}{Serialize(article.Notes.OfType<TreeNote>(), "")}{Serialize(article.Notes.OfType<Quote>(), "")}{Serialize(article.Notes.OfType<ImageNote>())} \n\n";
        }

        internal string Serialize(IEnumerable<ImageNote> images)
        {
            if (!images.Any())
            {
                return "";
            }
            return "\n\n### Images\n" + images.Distinct((i, j) => i.Url == j.Url, (i) => i.Url.GetHashCode()).Select(i => $"![{i.Name}]({i.Url})").CombineWithNewLine();
        }

        internal string Serialize(IEnumerable<Quote> quotes, string eolReference)
        {
            if (!quotes.Any())
            {
                return "";
            }
            return "\n\n### Quotes\n" + quotes.Select(i => $"\"{i.Content}\"{eolReference}").CombineWithNewLine();
        }

        internal string Serialize(IEnumerable<TreeNote> notes, string eolReference)
        {
            if (!notes.Any())
            {
                return "";
            }
            return "\n\n### Takeaways\n" + notes.Select(i => Serialize(i, 0, eolReference)).CombineWithNewLine();
        }

        internal string Serialize(TreeNote n, int indent, string eolReference)
        {
            return " ".Repeat(indent * 3) + " - " + n.Content + eolReference + (n.Children.Count > 0 ? "\n" : "") + n.Children.Select(i => Serialize(i, indent + 1, eolReference)).CombineWithNewLine();
        }
    }
    public static class TelegramServiceExtension
    {
        public static string? EscapeMarkdown(this string? input)
        {
            foreach (char c in new char[] { '_', '*', '`', '[' })
            {
                input = input?.Replace(c.ToString(), "\\" + c);
            }

            return input;
        }
    }
}