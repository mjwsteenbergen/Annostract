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

namespace Annostract {
    public class MarkenderSerializer : MarkdownSerializer
    {

        public override Task<string> Serialize(List<ExtractedSource> sources)
        {
            string result = "# Annostract\n\n";
            var content = sources.Select(i => Serialize(i)).Combine("\n");
            result += content;
            result += "\n\n## Documents to read\n" + sources.SelectMany(i => i.Articles).Where(i => i.Notes.Count == 0).Select(i => 
            (i.Path, i.Reference) switch {
                (string p1, string r1) when string.IsNullOrEmpty(p1) && string.IsNullOrEmpty(r1)  => $" - {i.Name}",
                (string p2, string r2) when string.IsNullOrEmpty(r2) => $" - [{i.Name}]({p2})",
                (string p3, string r3) when string.IsNullOrEmpty(p3) => $" - {i.Name} [@{r3}]",
                (string bp, string br) => $" - [{i.Name}]({bp}) [@{br}]",
            }).Combine("\n");
            return Task.FromResult(result);
        }

        public override string Serialize(ExtractedSource source)
        {
            var result = $"# {source.Name}\n";
            var reviews = source.Articles.Where(i => i.Notes.Count > 0).Select(i => Serialize(i)).Combine("\n");
            result += reviews;
            result += source.Bibliography.Select(i => $"<md-bib src='./{i}'></md-bib>").Combine("\n");
            return result;
        }

        public override string Serialize(ExtractedArticle article)
        {
            string abstrac = "";
            var abs = article.Notes.OfType<Abstract>().FirstOrDefault();
            if(abs != null) {
                abstrac = "\n" + abs.Content;
            }

            string eolReference = !string.IsNullOrEmpty(article.Reference) ? " [@" + article.Reference + "]" : "";
            

            return $"## {article.Name.EscapeMarkdown()} {eolReference} {abstrac}{Serialize(article.Notes.OfType<TreeNote>(), eolReference)}{Serialize(article.Notes.OfType<Quote>(), eolReference)}{Serialize(article.Notes.OfType<ImageNote>())} \n\n";
        }


    }

     
    
}