using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;
using System.Text.Json;
using UglyToad.PdfPig.Tokens;
using System.Threading.Tasks;
using System.IO;
using Martijn.Extensions.Linq;
using System.Text.Json.Serialization;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Annostract
{
    public static class AnnotationExtractor
    {
        public static ExtractedFile Extract(DirectoryInfo baseDir, FileInfo pdfFile)
        {
            ExtractedFile file = null;
            using (PdfDocument document = PdfDocument.Open(pdfFile.FullName))
            {
                DateTime time = DateTime.Now;
                
                file = new ExtractedFile(pdfFile, document.Information.Author);
                try {
                    foreach (var page in document.GetPages())
                    {
                        string? allText = null;
                        IEnumerable<Word> words = null;


                        // var images = 
                        var annos = page.ExperimentalAccess.GetAnnotations();
                        foreach (var annotation in annos.Where(i => i.Type == AnnotationType.Highlight || i.Type == AnnotationType.Ink))
                        {
                            if(allText == null || words == null) {
                                allText = page.Text;
                                words = page.GetWords(new NearestNeighbourWordExtractor());
                            }

                            var result = annotation.Type switch
                            {
                                AnnotationType.Highlight => ExtractHighlight(annotation, page, allText, words),
                                AnnotationType.Ink => ImageExtractor.Extract(baseDir, pdfFile.Name, page, annotation),
                                _ => null
                            };

                            if (result != null)
                            {
                                file.Results.Add(result);
                            }
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }

                Console.Write((DateTime.Now - time).TotalSeconds.ToString("F3").PadLeft(12));
                Console.WriteLine(" Finished Extracting " + file.FileName);

                return file;
            }
        }

        public static Result ExtractHighlight(UglyToad.PdfPig.Annotations.Annotation annotation, Page page, string allText, IEnumerable<Word> words) {
            List<PdfRectangle> rects = new List<PdfRectangle>();

            var dict = annotation.AnnotationDictionary.Data;
            if (!dict.Keys.Contains("QuadPoints"))
            {
                throw new Exception("Highlight does not contain QuadPoints");
            }

            var numberArray = (dict["QuadPoints"] as ArrayToken).Data.OfType<NumericToken>().Select(i => i.Data).ToArray();

            for (int i = 0; i < numberArray.Length; i += 8)
            {
                rects.Add(new PdfRectangle(numberArray[i + 0], numberArray[i + 1], numberArray[i + 6], numberArray[i + 7]));
            }

            string text = "";
            List<decimal> avgs = new List<decimal>();
            int counter = 0;
            
            foreach ((Word word, string actualText) in FixText(allText, words))
            {
                if (rects.Any(rect => word.BoundingBox.ContainedIn(rect)))
                {
                    var letters = word.Letters.Select(i => (content: i.Value, br: i.StartBaseLine.X + i.Width, bl: i.StartBaseLine.X)).ToList();
                    List<string> chars = new List<string> {
                        letters[0].content
                    };

                    var actactualText = actualText;

                    if(actualText.Length != word.Text.Length) {
                        actactualText = word.Text;
                    }

                    text += actactualText + " ";
                }
                counter += word.Text.Length;
            }

            if(avgs.Count > 0) {
                Console.WriteLine(avgs.Average());
                Console.WriteLine(avgs.Min());
            }

            var color = (dict["C"] as ArrayToken).Data.OfType<NumericToken>().ToArray();


            //, AnnoSerializer.Convert(annotation.)
            return new HighlightResult(text, annotation.Content, ColorConverter.Convert((double)color[0].Data, (double) color[1].Data, (double) color[2].Data));
        }

        private static List<(Word w, string actualText)> FixText(string allText, IEnumerable<Word> words)
        {
            List<(Word w, string actualText)> list = new List<(Word w, string actualText)>();

            var allChars = new Stack<char>(allText.Reverse());
            var wordStack = new Stack<Word>(words.Reverse());
            var currentWord = wordStack.Pop();
            var downWord = currentWord.Text.ToList();
            var actualText = "";

            while(allChars.Count != 0) {
                var newChar = allChars.Pop();
                if(!downWord.Contains(newChar)) {
                    if(wordStack.Count > 2 && wordStack.Peek().Text.Contains(newChar)) {
                        list.Add((currentWord, actualText));

                        currentWord = wordStack.Pop();
                        downWord = currentWord.Text.ToList();
                        actualText = "";
                    } else {
                        continue;
                    }
                } 

                actualText += newChar;
                downWord.Remove(newChar);
            }

            
            list.Add((currentWord, actualText));

            while(wordStack.Count > 0) {
                list.Add((wordStack.Peek(), wordStack.Pop().Text));
            }

            return list;
        }

        private static string FixText(string allText, int counter, string text)
        {
            return allText.Substring(counter, text.Length);
        }
    }

    public class ExtractedFile {
        [JsonIgnore]
        public FileInfo FilePath { get; set; }
        public string FileName => FilePath.Name.Replace(FilePath.Extension, "");

        public ExtractedFile(FileInfo filePath, string authors)
        {
            FilePath = filePath;
            Authors = authors;
            Results = new List<Result>();
        }

        public string Authors { get; set; }

        public List<Result> Results { get; set; }
    }

    public interface Result {}

    public class HighlightResult : Result
    {
        public HighlightResult(string highlight, string note, HighlightColor color = HighlightColor.Unknown)
        {
            this.HighlightedText = highlight;
            this.Note = note;
            this.HighlightColor = color;
        }

        public string HighlightedText { get; set; }

        public HighlightColor HighlightColor { get; set; }

        public string Note { get; set; }
    }

    public static class ColorConverter {

        public static HighlightColor Convert(double r, double g, double b) {
            return (r, g, b) switch {
                //Microsoft Edge Colours
                (1, 0.90196, 0) => HighlightColor.Yellow,
                (0.26667, 0.78431, 0.96078) => HighlightColor.Blue,
                (0.14902, 0.90196, 0) => HighlightColor.Green,
                (0.92549, 0, 0.54902) => HighlightColor.Pink,
                (_, _, _) => HighlightColor.Unknown
            };
        }

    }

    public enum HighlightColor {
        Yellow, Blue, Green, Pink, Unknown
    }

    internal static class Extensions
    {
        public static bool ContainedIn(this PdfRectangle word, PdfRectangle highlightBox)
        {
            var avgWordY = (word.BottomRight.Y + word.TopLeft.Y) / 2;
            // return (mine.BottomRight.X >= other.BottomLeft.X && mine.BottomLeft.Y >= other.BottomLeft.Y) &&
            // (mine.BottomLeft.X <= other.TopRight.X && mine.TopRight.Y <= other.TopRight.Y);
            return
            highlightBox.BottomRight.Y <= avgWordY + (highlightBox.Height/10) && highlightBox.TopLeft.Y >= avgWordY - (highlightBox.Height/10) &&
                XTest(highlightBox, word.Centroid);
                // && highlightBox.Contains(word.Centroid);
            // return highlightBox.Contains(word.BottomLeft) || highlightBox.Contains(word.BottomRight) || highlightBox.Contains(word.TopLeft) || highlightBox.Contains(word.TopRight);
        }

        public static bool XTest(PdfRectangle rect, PdfPoint point)
        {
            return rect.BottomLeft.X <= (point.X + rect.Width / 20) && rect.TopRight.X >= (point.X - rect.Width / 20);
        }

        public static bool Contains(this PdfRectangle rect, PdfPoint point)
        {
            return (rect.BottomLeft.X <= point.X && rect.BottomLeft.Y <= point.Y) &&
                (rect.TopRight.X >= point.X && rect.TopRight.Y >= point.Y);
        }
    }
}