using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using UglyToad.PdfPig;
using ApiLibs.General;
using System.Threading.Tasks;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Content;
using System.Text;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Martijn.Extensions.Text;
using System.Collections;

namespace Annostract
{
    class Program
    {
        /// <summary>
        /// Extracts annotations from pdf
        /// </summary>
        /// <param name="path">Path to pdf</param>
        /// <param name="o">Write to file</param>
        /// <param name="formatter">Which formatter to use (markdown(standard), markender or json)</param>
        static async Task Main(string path, bool o, string formatter = "markdown", string? instapaper = null)
        {
            List<Extractor> extractors = new List<Extractor>();

            if (path != null)
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                extractors.Add(new AnnotationExtractor(dir.FullName));
            }

            if(instapaper != null)
            {
                extractors.Add(new InstapaperExtractor(instapaper));
            }

            var sources = await extractors.Select(i => i.Extract()).WhenAll();

            "Starting to serialize".Print();

            var resultString = formatter switch {
                "markdown" => await new MarkdownSerializer().Serialize(sources.ToList()),
                "markender" => await new MarkenderSerializer().Serialize(sources.ToList()),
                "latex" => await new LatexSerializer().Serialize(sources.ToList()),
                // "markdown" => await new MarkdownSerializer().Serialize(extractedFiles, path),
                "json" => System.Text.Json.JsonSerializer.Serialize(sources, new JsonSerializerOptions()
                {
                    WriteIndented = true
                }),
                _ => throw new Exception($"Invalid format {formatter}")
            };

            if (path != null && o)
            {
                FileAttributes attr = File.GetAttributes(path);
                var isDir = attr.HasFlag(FileAttributes.Directory);
                
                if (isDir)
                {
                    "Writing to file".Print();

                    DirectoryInfo dir = new DirectoryInfo(path);
                    string prepath = dir.FullName + Path.DirectorySeparatorChar;

                    var resultTask = formatter switch
                    {
                        "markender" => File.WriteAllTextAsync(prepath + "Annostract.md", resultString),
                        "latex" => File.WriteAllTextAsync(prepath + "Annostract.tex", resultString),
                        "markdown" => File.WriteAllTextAsync(prepath + "Annostract.md", resultString),
                        "json" => File.WriteAllTextAsync(prepath + "Annostract.json", resultString),
                        _ => throw new Exception($"Invalid format {formatter}")
                    };
                    await resultTask;
                    return;
                } 
                else 
                {
                    "Don't know where to write to. Printing instead".Print();
                }
            }

            Console.WriteLine(resultString);
        }
    }
}
