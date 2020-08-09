using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annostract.PaperFinders;
using Annostract.PaperFinders.Crossref;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using static Annostract.PaperFinders.BibtexMaker;

namespace Annostract {
    public class AnnotationExtractor : Extractor
    {
        public AnnotationExtractor(string path)
        {
            this.ExtractPath = path;
        }

        public string ExtractPath { get; set; }
        public async Task<ExtractedSource> Extract()
        {
            var fullPath = Path.GetFullPath(ExtractPath);
            FileAttributes attr = File.GetAttributes(ExtractPath);
            var isDir = attr.HasFlag(FileAttributes.Directory);

            List<ExtractedArticle> extractedFiles = new List<ExtractedArticle>();

            if (isDir)
            {
                DirectoryInfo dir = new DirectoryInfo(ExtractPath);
                var files = ExtractRecursively(dir, dir);
                extractedFiles = (await files.Select(i => Task.Run(() => FileExtractor.RunExtraction(dir, i))).WhenAll()).ToList();
                
            }
            else
            {
                var file = new FileInfo(ExtractPath);
                extractedFiles = new List<ExtractedArticle> {
                    await FileExtractor.RunExtraction(file.Directory, file)
                };
            }

            var source = new ExtractedSource("From files", extractedFiles);

            source.Bibliography = await Bibliography(source);

            return source;
        }

        public static IEnumerable<FileInfo> ExtractRecursively(DirectoryInfo folder, DirectoryInfo baseDir)
        {
            var tasks = folder.EnumerateFiles().Where(i => i.FullName.EndsWith(".pdf"));

            return tasks.Concat(folder.EnumerateDirectories().SelectMany(i => ExtractRecursively(i, baseDir)));
        }

        public async Task<List<string>> Bibliography(ExtractedSource source)
        {
            var files = (await source.Articles.Select(async article =>
            {
                var lastSlash = article.Name.LastIndexOf(Path.DirectorySeparatorChar);
                var paper = await PaperFinder.Find(article.Name.Substring(lastSlash == -1 ? 0 : lastSlash + 1));
                return (article, paper);
            }).WhenAll()).Where(i => i.Item2 != null).ToList();
            var reads = await source.Articles.SelectMany(i => i.Notes.OfType<ToRead>()).Select(async i => (i, await PaperFinder.Find(i.Content))).WhenAll();

            List<(ToRead, CrossRefSearchResult)> papers = reads.Where(i => i.Item2 != null && !files.Any(j => j.Item2?.Doi == i.Item2?.Doi)).ToList();
            var unknowns = reads.Where(i => i.Item2 == null);

            papers.Select(i => i.Item2).OrderByDescending(i => i.IsReferencedByCount).Foreach(paper => source.Articles.Add(new ExtractedArticle(paper.Link?.FirstOrDefault(i => i.IsPdf())?.Url?.AbsoluteUri ?? $"http://dx.doi.org/{paper.Doi}") {
                Abstract = paper.Abstract,
                Name = paper.Title?.Combine(" ") + " " + paper.Subtitle?.Combine(" "),
                Reference = SourceToBibDoiId(paper)
            }));

            unknowns.Select(i => i.i).Foreach(i => source.Articles.Add(new ExtractedArticle("") {
                Name = i.Content
            }));




            if (ExtractPath != null)
            {
                await BibtexMaker.CreateBibtex(ExtractPath + "Annostract.Files.bib", files.Select(i => i.Item2).ToList());
                await BibtexMaker.CreateBibtex(ExtractPath + "Annostract.FutureReading.bib", papers.Select(i => i.Item2).ToList());
            }


            return new List<string> {
                "Annostract.Files.bib",
                "Annostract.FutureReading.bib"
            };
        }
    }
}