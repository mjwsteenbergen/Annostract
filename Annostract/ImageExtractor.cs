
using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig.Content;

namespace Annostract
{
    public class ImageExtractor
    {
        public string BaseUrl { get; set; }

        

        public static Result Extract(DirectoryInfo resourcesFolder, string filename, Page page, UglyToad.PdfPig.Annotations.Annotation anno) {
            var allImages = page.GetImages().ToList();
            var images = allImages.Where(i => anno.Rectangle.ContainedIn(i.Bounds)).ToList();
            
            resourcesFolder.CreateSubdirectory("resources");

            int count = 0;
            foreach (var image in images)
            {
                try
                {
                    var name = $"{filename.Replace(" ","")}.page{page.Number}.{allImages.IndexOf(image)}.jpeg";
                    using (var fs = new FileStream(resourcesFolder.FullName + Path.DirectorySeparatorChar + "resources" + Path.DirectorySeparatorChar + name , FileMode.Create, FileAccess.Write))
                    {
                        byte[] v = image.RawBytes.ToArray();
                        fs.Write(v, 0, v.Length);
                    }
                    return new ImageResult {
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

        public static void WriteToFile() {

        }

        public ImageExtractor(string BaseUrl)
        {
            this.BaseUrl = BaseUrl;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class ImageResult : Result
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}