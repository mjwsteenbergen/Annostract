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

namespace Annostract
{
    class Program
    {
        /// <summary>
        /// Extracts annotations from pdf files
        /// </summary>
        /// <param name="path">Path to pdf</param>
        /// <param name="o">Write to file</param>
        /// <param name="formatter">Which formatter to use (markdown(standard), markender or json)</param>
        static async Task Main(string path, bool o, string formatter = "markdown")
        {
            if (path == null)
            {
                Console.WriteLine("Does not contain path. Exiting");
                return;
            }

           

            path = Path.GetFullPath(path);
            FileAttributes attr = File.GetAttributes(path);
            var isDir = attr.HasFlag(FileAttributes.Directory);

            List<ExtractedDocument> extractedFiles = new List<ExtractedDocument>();

            if(isDir) {
                DirectoryInfo dir = new DirectoryInfo(path);
                extractedFiles.AddRange((await ExtractRecursively(dir, dir).WhenAll()).ToList());
            } else {
                var file = new FileInfo(path);
                extractedFiles = new List<ExtractedDocument> {
                    await AnnotationExtractor.Extract(file.Directory, file)
                };
            }

            extractedFiles.AddRange((await InstapaperExtractor.Extract()).OfType<ExtractedDocument>().ToList());

            "Starting to serialize".Print();

            var resultString = formatter switch {
                "markender" => await new MarkenderSerializer().Serialize(extractedFiles, path),
                "markdown" => await new AnnoSerializer().Serialize(extractedFiles, path),
                "json" => System.Text.Json.JsonSerializer.Serialize(extractedFiles, new JsonSerializerOptions()
                {
                    WriteIndented = true
                }),
                _ => throw new Exception($"Invalid format {formatter}")
            };

            if (o)
            {
                if (isDir)
                {
                    "Writing to file".Print();
                    await File.WriteAllTextAsync(path + "Annostract.md", resultString);
                }
            }
            else
            {
                Console.WriteLine(resultString);
            }

        }


        public static IEnumerable<Task<ExtractedFile>> ExtractRecursively(DirectoryInfo folder, DirectoryInfo baseDir)
        {
            var tasks = folder.EnumerateFiles().Where(i => i.Extension == ".pdf").Select(i => AnnotationExtractor.Extract(baseDir, i));

            return tasks.Concat(folder.EnumerateDirectories().SelectMany(i => ExtractRecursively(i, baseDir)));
        }
    }
}
