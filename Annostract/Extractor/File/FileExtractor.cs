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
using System.Text.RegularExpressions;
using static Annostract.PaperFinders.BibtexMaker;
using Annostract.PaperFinders;

namespace Annostract
{
    public static class FileExtractor
    {
        public static async Task<ExtractedArticle> RunExtraction(DirectoryInfo baseDir, FileInfo pdfFile)
        {
            using (PdfDocument document = PdfDocument.Open(pdfFile.FullName))
            {
                DateTime time = DateTime.Now;
                
                string shortPath = pdfFile.FullName.Replace(pdfFile.Extension, "").Replace(baseDir.FullName, "");
                string reference = "";
                var lastSlash = shortPath.LastIndexOf(Path.DirectorySeparatorChar);
                var paper = await PaperFinder.Find(shortPath.Substring(lastSlash == -1 ? 0 : lastSlash + 1));
                if (paper != null)
                {
                    reference = SourceToBibDoiId(paper);
                }

                //pdfFile, document.Information.Author
                ExtractedArticle file = new ExtractedArticle(pdfFile.FullName)
                {
                    Name = pdfFile.FullName.Replace(pdfFile.Extension, "").Replace(baseDir.FullName, ""),
                    Reference = reference
                };
                try
                {
                    foreach (var page in document.GetPages())
                    {
                        string? allText = null;
                        IEnumerable<Word>? words = null;


                        // var images = 
                        var annos = page.ExperimentalAccess.GetAnnotations().ToList();
                        foreach (var annotation in annos.Where(i => i.Type == AnnotationType.Highlight))
                        {
                            if (allText == null || words == null)
                            {
                                allText = page.Text;
                                words = page.GetWords(new NearestNeighbourWordExtractor());
                            }

                            file.Notes.Add(ExtractHighlight(annotation, page, allText, words));
                        }

                        foreach (var annotation in annos.Where(i => i.Type == AnnotationType.Ink))
                        {
                            file.Notes.Add(ImageExtractor.Extract(baseDir, pdfFile.Name, page, annotation));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                Console.WriteLine((DateTime.Now - time).TotalSeconds.ToString("F3").PadLeft(12) + " Finished Extracting " + file.Name);

                file.Notes = file.Notes.Where(i => i != null).ToList();

                var hlnotes = file.Notes.OfType<HighlightNote>().ToList();
                hlnotes.Foreach(i => file.Notes.Remove(i));
                hlnotes.Reverse();
                file.Notes.AddRange(TreeFy(new Stack<HighlightNote>(hlnotes), new TreeNote("")).Children);

                return file;
            }
        }

        public static TreeNote TreeFy(Stack<HighlightNote> notes, TreeNote note) {
            while(notes.Count > 0)
            {
                var newh = notes.Pop();

                if(newh.note == null)
                {
                    newh.ToTextNote(note);
                    continue;
                }

                var parsednote = newh.note.ToLower().Trim().Replace(" ", "");

                if (parsednote == ">")
                {
                    var newn = newh.ToTextNote(note);
                    TreeFy(notes, newn);
                }
                else if (parsednote == "<")
                {
                    newh.ToTextNote(note);
                    if(note.Parent != null)
                    {
                        break;
                    }
                }
                else if (parsednote == "<>" || parsednote == "-")
                {
                    newh.ToTextNote((note.Children.LastOrDefault() ?? note));
                }
                else if (parsednote == "<<" && note.Parent == null)
                {
                    newh.ToTextNote(note);
                }
                else if (parsednote == "<<")
                {
                    notes.Push(newh);
                    break;
                }
                else {
                    var newn = newh.ToTextNote(note);
                    newn.Children.Add(new TreeNote(newh.note) {
                        Parent = newn
                    });
                }
            }
            return note;
        }

        public static Note ExtractHighlight(UglyToad.PdfPig.Annotations.Annotation annotation, Page page, string allText, IEnumerable<Word> words)
        {
            List<PdfRectangle> rects = new List<PdfRectangle>();

            var dict = annotation.AnnotationDictionary.Data;
            if (!dict.Keys.Contains("QuadPoints"))
            {
                throw new Exception("Highlight does not contain QuadPoints");
            }

            var numberArray = (dict["QuadPoints"] as ArrayToken ?? throw new NullReferenceException("Is not a ArrayToken")).Data.OfType<NumericToken>().Select(i => i.Data).ToArray();

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
                    text += word.Text + " ";
                }
                counter += word.Text.Length;
            }

            text = Regex.Replace(text, @"\s+", " ");
            text = text.Replace("  ", " ");

            var color = (dict["C"] as ArrayToken ?? throw new NullReferenceException("Is not a ArrayToken")).Data.OfType<NumericToken>().ToArray();

            return ToNote(text, annotation.Content, ColorConverter.Convert((double)color[0].Data, (double)color[1].Data, (double)color[2].Data));
        }

        private static Note ToNote(string highlightedText, string note, HighlightColor highlightColor)
        {
            return (note?.ToLower().Trim().Replace(" ", ""), highlightColor) switch
            {
                ("read", _) => new ToRead(highlightedText),
                ("toread", _) => new ToRead(highlightedText),
                (_, HighlightColor.Blue) => new ToRead(highlightedText),
                ("abstract", _) => new Abstract(highlightedText),
                ("year", _) => new YearNote(highlightedText),
                ("doi", _) => new DoiLink(highlightedText),
                (_, _) => new HighlightNote(highlightedText, note)
            };
        }
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