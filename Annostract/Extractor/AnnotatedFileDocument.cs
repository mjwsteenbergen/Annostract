using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Tokens;
using System.Threading.Tasks;
using System.IO;
using Martijn.Extensions.Linq;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Core;
using Martijn.Extensions.Memory;
using System.Security.Cryptography;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Annostract.Extractor
{
    public class AnnotatedFileDocument : IAnnotatedDocument
    {
        public DirectoryInfo BaseDir { get; }
        public FileInfo PdfFile { get; }

        public AnnotatedFileDocument(DirectoryInfo baseDir, FileInfo pdfFile)
        {
            BaseDir = baseDir;
            PdfFile = pdfFile;
        }

        public Task<ExtractedDocument> Extract()
        {
            return new Memory()
            {
                Application = "Annostract" + Path.DirectorySeparatorChar + "cache", 
                CreateDirectoryIfNotExists = true
            }.ReadOrCalculate<ExtractedDocument>(PdfFile.Name + "-" + CalculateMD5(PdfFile.FullName) + ".json", () => RunExtraction());
        }

        private ExtractedFile RunExtraction()
        {
            ExtractedFile file = null;
            using (PdfDocument document = PdfDocument.Open(PdfFile.FullName))
            {
                DateTime time = DateTime.Now;

                file = new ExtractedFile(PdfFile, document.Information.Author);
                try
                {
                    foreach (var page in document.GetPages())
                    {
                        string? allText = null;
                        IEnumerable<Word> words = null;


                        // var images = 
                        var annos = page.ExperimentalAccess.GetAnnotations();
                        foreach (var annotation in annos.Where(i => i.Type == AnnotationType.Highlight || i.Type == AnnotationType.Ink))
                        {
                            if (allText == null || words == null)
                            {
                                allText = page.Text;
                                words = page.GetWords(new NearestNeighbourWordExtractor());
                            }

                            var result = annotation.Type switch
                            {
                                AnnotationType.Highlight => ExtractHighlight(annotation, page, allText, words),
                                AnnotationType.Ink => ExtractImage(BaseDir, PdfFile.Name, page, annotation),
                                _ => null
                            };

                            if (result != null)
                            {
                                file.Results.Add(result);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                Console.Write((DateTime.Now - time).TotalSeconds.ToString("F3").PadLeft(12));
                Console.WriteLine(" Finished Extracting " + file.Title);

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
                rects.Add(new PdfRectangle((double)(numberArray[i + 0]), (double)(numberArray[i + 1]), (double)(numberArray[i + 6]), (double)(numberArray[i + 7])));
            }

            string text = "";
            List<decimal> avgs = new List<decimal>();
            int counter = 0;
            
            foreach ((Word word, string actualText) in words.Select(i => (i, i.Text)))
            {
                if (rects.Any(rect => rect.Contains(word.BoundingBox)))
                {
                    text += word.Text.Trim() + " ";
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

        public static Result ExtractImage(DirectoryInfo resourcesFolder, string filename, Page page, UglyToad.PdfPig.Annotations.Annotation anno)
        {
            var allImages = page.GetImages().ToList();
            var images = allImages.Where(i => anno.Rectangle.ContainedIn(i.Bounds)).ToList();

            resourcesFolder.CreateSubdirectory("resources");

            int count = 0;
            foreach (var image in images)
            {
                try
                {
                    var name = $"{filename.Replace(" ", "")}.page{page.Number}.{allImages.IndexOf(image)}.jpeg";
                    using (var fs = new FileStream(resourcesFolder.FullName + Path.DirectorySeparatorChar + "resources" + Path.DirectorySeparatorChar + name, FileMode.Create, FileAccess.Write))
                    {
                        byte[] v = image.RawBytes.ToArray();
                        fs.Write(v, 0, v.Length);
                    }
                    return new ImageResult
                    {
                        Url = "resources" + Path.DirectorySeparatorChar + name
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception caught in process: {0}", ex);
                }
            }
            // Console.WriteLine($"WARN: No image found for ink on page {page.Number}");

            return null;
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

        //https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
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
            // return (word.BottomRight.X >= highlightBox.BottomLeft.X && word.BottomLeft.Y >= highlightBox.BottomLeft.Y) &&
            // (word.BottomLeft.X <= highlightBox.TopRight.X && word.TopRight.Y <= highlightBox.TopRight.Y);
            var avgWordY = (word.BottomRight.Y + word.TopLeft.Y) / 2;
            // return highlightBox.BottomRight.Y <= avgWordY + (highlightBox.Height/10)
                // && highlightBox.TopLeft.Y >= avgWordY - (highlightBox.Height/10);
            // return XTest(highlightBox, word.Centroid);
            return highlightBox.Contains(word.Centroid);
            // return highlightBox.Contains(word.BottomLeft) || highlightBox.Contains(word.BottomRight) || highlightBox.Contains(word.TopLeft) || highlightBox.Contains(word.TopRight);
        }

        public static bool XTest(PdfRectangle rect, PdfPoint point)
        {
            return rect.BottomLeft.X <= (point.X + rect.Width / 20) && rect.TopRight.X >= (point.X - rect.Width / 20);
        }

        public static bool Contains(this PdfRectangle rect, PdfPoint point)
        {
            return (rect.Left <= point.X && rect.Bottom <= point.Y) &&
                (rect.Right >= point.X && rect.Top >= point.Y);
        }

        public static bool Contains(this PdfRectangle biggerRect, PdfRectangle smallerRect)
        {
            return biggerRect.Contains(smallerRect.Centroid);
        }
    }
}