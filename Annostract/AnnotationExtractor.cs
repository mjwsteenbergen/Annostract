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
using UglyToad.PdfPig.Core;

namespace Annostract
{
    public static class AnnotationExtractor
    {
        public static ExtractedFile Extract(string fpath)
        {
            ExtractedFile file = null;
            using (PdfDocument document = PdfDocument.Open(fpath))
            {
                
                file = new ExtractedFile(fpath, document.Information.Author);

                foreach (var page in document.Pages())
                {
                    foreach (var annotation in page.ExperimentalAccess.GetAnnotations())
                    {
                        List<PdfRectangle> rects = new List<PdfRectangle>();

                        var dict = annotation.AnnotationDictionary.Data;
                        if(!dict.Keys.Contains("QuadPoints"))
                        {
                            continue;
                        } 

                        var numberArray = (dict["QuadPoints"] as ArrayToken).Data.OfType<NumericToken>().Select(i => (short)i.Data).ToArray();

                        for (int i = 0; i < numberArray.Length; i += 8)
                        {
                            rects.Add(new PdfRectangle(numberArray[i + 0], numberArray[i + 1], numberArray[i + 6], numberArray[i + 7]));
                        }

                        string text = "";
                        foreach (var word in page.GetWords())
                        {
                            if (rects.Any(rect => word.BoundingBox.ContainedIn(rect)))
                            {
                                var avg = word.Letters.Select(i => i.Location.X).Select((i, j) => i - word.Letters[(j - 1) > 0 ? (j - 1) : 0].Location.X).Average();

                                var wordText = "";

                                for (int i = 0; i < word.Letters.Count; i++)
                                {
                                    var diff = word.Letters[i].Location.X - word.Letters[(i - 1) > 0 ? (i - 1) : 0].Location.X;
                                    wordText += word.Letters[i].Value;
                                }

                                text += wordText + " ";
                            }
                        }

                        file.Results.Add(new Result(text, annotation.Content));
                    }
                }
            }
            return file;
        }
    }

    public class ExtractedFile {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        public ExtractedFile(string filePath, string authors)
        {
            FilePath = filePath;
            Authors = authors;
            Results = new List<Result>();
        }

        public string Authors { get; set; }

        public List<Result> Results { get; set; }
    }

    public class Result
    {
        public Result(string highlight, string note, HighlightColor color = HighlightColor.Unknown)
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
        public static IEnumerable<Page> Pages(this PdfDocument document)
        {
            for (int i = 1; i < document.NumberOfPages; i++)
            {
                yield return document.GetPage(i);
            }
        }

        public static bool ContainedIn(this PdfRectangle word, PdfRectangle highlightBox)
        {
            var avgWordY = (word.BottomRight.Y + word.TopLeft.Y) / 2;
            // return (mine.BottomRight.X >= other.BottomLeft.X && mine.BottomLeft.Y >= other.BottomLeft.Y) &&
            // (mine.BottomLeft.X <= other.TopRight.X && mine.TopRight.Y <= other.TopRight.Y);
            return highlightBox.BottomRight.Y <= avgWordY && highlightBox.TopLeft.Y >= avgWordY &&
                (XTest(highlightBox, word.BottomRight) || XTest(highlightBox, word.TopLeft)) && highlightBox.Contains(word.Centroid);
            // return highlightBox.Contains(word.BottomLeft) || highlightBox.Contains(word.BottomRight) || highlightBox.Contains(word.TopLeft) || highlightBox.Contains(word.TopRight);
        }

        public static bool XTest(PdfRectangle rect, PdfPoint point)
        {
            return rect.BottomLeft.X <= point.X && rect.TopRight.X >= point.X;
        }

        public static bool Contains(this PdfRectangle rect, PdfPoint point)
        {
            return (rect.BottomLeft.X <= point.X && rect.BottomLeft.Y <= point.Y) &&
                (rect.TopRight.X >= point.X && rect.TopRight.Y >= point.Y);
        }
    }
}