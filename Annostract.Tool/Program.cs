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

namespace Annostract
{
    class Program
    {
        /// <summary>
        /// Extracts annotations from pdf
        /// </summary>
        /// <param name="path">Path to pdf</param>
        /// <param name="json">Return json instead of parsed text</param>
        /// <param name="o">Write to file</param>
        static async Task Main(string path, bool json, bool o)
        {
            path = Path.GetFullPath(path);
            FileAttributes attr = File.GetAttributes(path);
            var isDir = attr.HasFlag(FileAttributes.Directory);

            

            if (path == null)
            {
                Console.WriteLine("Does not contain path. Exiting");
                return;
            }

            var extractedFiles = await Run(path, json, isDir, Path.GetFileName(Path.GetDirectoryName(path)) ?? "");

            var resultString = "";

            if (json)
            {
                resultString = System.Text.Json.JsonSerializer.Serialize(extractedFiles, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });

            }
            else
            {
                resultString = await AnnoSerializer.Serialize(extractedFiles, path);
            }

            if (o)
            {
                if (isDir)
                {
                    "Writing to file".ToString();
                    await File.WriteAllTextAsync(path + "AutoLit.md", resultString);
                }
            }
            else
            {
                Console.WriteLine(resultString);
            }

        }

        public static async Task<List<ExtractedFile>> Run(string path, bool json, bool isDir, string displayPath)
        {
            List<string> paths = new List<string>();

            FileAttributes attr = File.GetAttributes(path);

            List<ExtractedFile> results = new List<ExtractedFile>();

            var dirTasks = new List<Task<List<ExtractedFile>>>();

            if (isDir)
            {
                dirTasks = Directory.GetDirectories(path).Select(dir => Run(dir, json, isDir, displayPath + "/" + Path.GetFileName(Path.GetDirectoryName(dir + "/")))).ToList();
                paths.AddRange(Directory.GetFiles(path).Where(i => i.EndsWith(".pdf")));
            }
            else
            {
                paths.Add(path);
            }

            results = (await paths.Select(pathI => Convert(json, pathI)).WhenAll()).ToList();
            await dirTasks.ToIAsyncEnumberable().Foreach(i => results.AddRange(i));



            return results;
        }

        private static async Task<ExtractedFile> Convert(bool json, string pathI)
        {
            StringBuilder builder = new StringBuilder();

            ExtractedFile res = null;

            try
            {
                res = await AnnotationExtractorClown.ExtractAsync(pathI);
            }
            catch(Exception e)
            {
                try {
                    res = AnnotationExtractor.Extract(pathI);
                } catch(Exception ex) {
                    
                }
            }

            return res;
        }
    }
}
