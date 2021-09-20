using System;
using System.Collections.Generic;
using System.IO;
using Annostract.Extractor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Annostract
{
    public abstract class ExtractedDocument {
        public ExtractedDocument(string title)
        {
            Title = title;
            Results = new List<Result>();
        }

        public List<Result> Results { get; set; }

        public string Title { get; set; }
    }

    public class ExtractedFile : ExtractedDocument
    {
        private FileInfo _filePath;

        [JsonIgnore]
        public FileInfo FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                FilePathString = value.FullName;
            }
        }

        public string FilePathString
        {
            get => _filePath.FullName;
            set
            {
                _filePath = new FileInfo(value);
            }
        }

        public ExtractedFile() : base("")
        {
            Authors = "";
            _filePath = new FileInfo(this.GetType().Assembly.Location);
        }

        public ExtractedFile(FileInfo filePath, string authors) : base("")
        {
            FilePath = filePath;
            Authors = authors;
            Title = FilePath.Name.Replace(FilePath.Extension, "");
        }

        public string Authors { get; set; }
    }

    public class ResultConverter : JsonConverter<Result>
    {
        public override Result ReadJson(JsonReader reader, Type objectType, Result? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jObject = JToken.ReadFrom(reader);

            string? type = null;

            try
            {
                type = jObject["GetResultType"]?.ToObject<string>();
            }
            catch { }

            Result result = type switch
            {
                "highlight" => new HighlightResult(),
                "image" => new ImageResult(),
                _ => throw new ArgumentOutOfRangeException("Cannot convert type " + type)
            };


            serializer.Populate(jObject.CreateReader(), result);
            return result;
        }

        public override bool CanWrite => false;


        public override void WriteJson(JsonWriter writer, Result? value, JsonSerializer serializer)
        {

        }
    }

    [JsonConverter(typeof(ResultConverter))]
    public abstract class Result
    {
        public abstract string GetResultType { get; }
    }

    public class HighlightResult : Result
    {
        public HighlightResult()
        {
            this.HighlightedText = "";
            this.Note = "";
            this.HighlightColor = HighlightColor.Unknown;
        }

        public HighlightResult(string highlight, string note, HighlightColor color = HighlightColor.Unknown)
        {
            this.HighlightedText = highlight;
            this.Note = note;
            this.HighlightColor = color;
        }

        public string HighlightedText { get; set; }

        public HighlightColor HighlightColor { get; set; }

        public string Note { get; set; }

        public override string GetResultType => "highlight";
    }

    public class ImageResult : Result
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public override string GetResultType => "image";
    }

    public class ExtractedFolder 
    {
        public ExtractedFolder(string name)
        {
            Name = name;
            Files = new List<ExtractedDocument>();
            Folders = new List<ExtractedFolder>();
        }

        public string Name { get; set; }
        public List<ExtractedDocument> Files { get; set; }
        public List<ExtractedFolder> Folders { get; set; }
    }
}