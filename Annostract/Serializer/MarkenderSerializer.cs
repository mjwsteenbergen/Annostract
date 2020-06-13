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

        public override string Serialize(ExtractedSource source)
        {
            var result = $"# {source.Name}\n";
            var reviews = source.Articles.Where(i => i.Notes.Count > 0).Select(i => Serialize(i)).CombineWithNewLine();
            result += reviews;
            result += source.Bibliography;
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