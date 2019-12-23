using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract.PaperFinder.Crossref;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;

namespace Annostract
{
    public class MarkenderSerializer : AnnoSerializer
    {
        public override Annotation Convert(HighlightResult res) {
            res.HighlightedText = res.HighlightedText.Replace("[", "\\[");
            return base.Convert(res);
        }

        internal override string GetPaperHeader(ExtractedFile file, CrossRefSearchResult paper) {
            var res = $"## {file.FileName}";

            if (!string.IsNullOrEmpty(paper?.Doi))
            {
                res += $"[{paper.Doi.Replace("/","-")}]";
            }
            return res;
        }

        public override async Task<string> Serialize(List<ExtractedFile> extractedFiles, string originalPath) {
            var start = await base.Serialize(extractedFiles, originalPath);

            List<CrossRefSearchResult> ress = new List<CrossRefSearchResult>();
            foreach (var item in extractedFiles)
            {
                ress.Add(await PaperFinder.PaperFinder.Find(item.FileName));
            }

            start += "<md-bib>\n" + ress.Where(i => !string.IsNullOrEmpty(i?.Doi)).Select(i => $"\t<md-bib-doi id=\"{i.Doi.Replace("/", "-")}\">{i.Doi}</md-bib-doi>").CombineWithNewLine() + "\n</md-bib>";

            return start;
        }
    }
}