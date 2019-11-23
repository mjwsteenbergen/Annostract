using System.Collections.Generic;
using System.Linq;
using org.pdfclown.documents;
using org.pdfclown.files;
using org.pdfclown.documents.interaction.annotations;
using org.pdfclown.objects;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig;
using WordExtractor;
using System.Threading.Tasks;
using UglyToad.PdfPig.Graphics.Colors;
using org.pdfclown.documents.contents.colorSpaces;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using System.Text.RegularExpressions;
using System;
using org.pdfclown.tools;
using Martijn.Extensions.Memory;
using ApiLibs;

namespace Annostract
{
    public static class AnnotationExtractorClown
    {
        public static async Task<ExtractedFile> ExtractAsync(string fpath)
        {
            ExtractedFile extract = null;

            var file = new File(fpath);

            WordInferer inferer = new WordInferer(await GetStandardDictionary());
            
            // var annos = file.Document.Pages.SelectMany(i => i.Annotations).ToList();
            using (PdfDocument document = PdfDocument.Open(fpath))
            {
                extract = new ExtractedFile(fpath, document.Information.Author);

                foreach (var page in file.Document.Pages)
                {
                    var res = new List<((decimal x, decimal y), Result res)>();
                    
                    foreach (var high in page.Annotations.OfType<TextMarkup>())
                    {
                        // if(high.Color is DeviceRGBColor col) {
                        //     col.R.Print();
                        // }
                        var quad = (high.DataContainer.DataObject as PdfDictionary).First(i => i.Key.RawValue == "QuadPoints").Value as PdfArray;
                        var corners = quad.OfType<PdfReal>().Select(i => new decimal(i.RawValue)).ToArray();
                        (int index, string highlight) = CombineWithWord(corners, file.Document.Pages.IndexOf(page) + 1, document);

                        if(highlight != null) {

                            var original = highlight;
                            // highlight = (await highlight.Replace("\n", "").Replace("- ", "").Split(' ').Select(i => Task.FromResult(i)).WhenAll()).CombineWithSpace();
                            var highlightPieces = await highlight.Replace("\n", "").Replace("- ", "").Replace("ﬁ", "fi").Split(' ').Select(i => inferer.Infer(i)).WhenAll(); 
                            highlight = highlightPieces.CombineWithSpace();
                        }

                        var tos = high.BaseObject.ToString();
                        int id = int.Parse(Regex.Match(tos, "(\\d+) \\d* R").Groups[1].Value);

                        HighlightColor color = HighlightColor.Unknown;

                        if(high.Color is DeviceRGBColor col) {
                            color = ColorConverter.Convert(col.R, col.G, col.B);
                        }

                        res.Add(((corners[0], corners[1]), new Result(highlight, high.Text, color)));
                        // res.Add(((index, corners[1]), new Result(highlight, high.Text)));

                    }
                    var boundary = page.Box.Width / 2;
                    extract.Results.AddRange(res.Where(i => (float)i.Item1.x < boundary).OrderByDescending(i => i.Item1.y).Concat(res.Where(i => (float)i.Item1.x >= boundary).OrderByDescending(i => i.Item1.y)).Select(i => i.res));
                    // extract.Results.AddRange(res.OrderByDescending(i => i.Item1.x).Select(i => i.res));
                }
            }


            return extract;
        }

        public async static Task<Dictionary<string, int>> GetStandardDictionary()
        {
            return await new Memory
            {
                Application = "WordCount",
                CreateDirectoryIfNotExists = true
            }.ReadOrCalculate<Dictionary<string, int>>("wrangle-words.json", () => {
                return new GistService().GetGist<Dictionary<string, int>>("mjwsteenbergen", "9ca33ead61d0c42475194ba1706139e2", "word-list.json");
            });
        }

        class GistService : Service
        {
            public GistService() : base("https://gist.githubusercontent.com/") { }

            public Task<T> GetGist<T>(string username, string gistId, string file) => MakeRequest<T>($"{username}/{gistId}/raw/{file}");
        }

        public static (int index, string word) CombineWithWord(decimal[] numberArray, int pageNumber, PdfDocument document) {
            List<PdfRectangle> rects = new List<PdfRectangle>();
            for (int i = 0; i < numberArray.Length; i += 8)
            {
                rects.Add(new PdfRectangle(numberArray[i + 0], numberArray[i + 1], numberArray[i + 6], numberArray[i + 7]));
            }

            string text = "";
            int index = 0;
            int minIndex = int.MaxValue;
            foreach (var word in document.GetPage(pageNumber).GetWords())
            {
                index++;
                if (rects.Any(rect => word.BoundingBox.ContainedIn(rect)))
                {
                    text += word.Text + " ";
                    if(minIndex == int.MaxValue) {
                        minIndex = index;
                    } else {
                        minIndex = Math.Max(minIndex, index);
                    }
                }
            }

            return (minIndex, text);
        }
    }
}