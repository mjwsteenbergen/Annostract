using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiLibs;
using Newtonsoft.Json;

namespace Annostract.PaperFinder
{
    class MsftAcademic : Service
    {
        public MsftAcademic(string key) : base("https://api.labs.cognitive.microsoft.com/academic/v1.0/")
        {
            AddStandardHeader("Ocp-Apim-Subscription-Key", key);
        }

        public async Task<List<Paper>> SearchFor(string input) {
            string res = "";

            foreach (var item in input.Split(' '))
            {
                if(!string.IsNullOrEmpty(res)) {
                    res = $"AND(W='{item}', {res})";
                } else {
                    res = $"W='{item}'";
                }
            }
            
            return(await MakeRequest<EvaluateResultRoot>("evaluate", parameters: new List<Param> {
                new Param("expr", res),
                new Param("attributes", "Id,Ti,DOI,S,D,Y")
            })).entities;
        }
    }

#pragma warning disable CS8618  

    public class Paper
    {
        public double logprob { get; set; }
        public double prob { get; set; }
        
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("Ti")]
        public string Title { get; set; }

        [JsonProperty("CC")]
        public long NumberOfCitations { get; set; }

        [JsonProperty("DN")]
        public string OriginalTitle { get; set; }

        [JsonProperty("VFN")]
        public string FullNameOfConference { get; set; }

        public string DOI { get; set; }

        [JsonProperty("D")]
        public string DatePublished { get; set; }
        
        [JsonProperty("Y")]
        public int YearPublished { get; set; }

        [JsonProperty("S")]
        public List<PaperSource> Source { get; set; }
    }

    public class PaperSource {

        [JsonProperty("U")]
        public string Source { get; set; }

        [JsonConverter(typeof(SourceTypeConverter))]
        [JsonProperty("Ty")]
        public SourceType Type { get; set; }
    }

    public class SourceTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(SourceType) || t == typeof(SourceType?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<int>(reader);
            switch (value)
            {
                case 1:
                    return SourceType.HTML;
                case 2:
                    return SourceType.Text;
                case 3:
                    return SourceType.PDF;
                case 4:
                    return SourceType.DOC;
                case 5:
                    return SourceType.PPT;
                case 6:
                    return SourceType.XLS;
                case 7:
                    return SourceType.PS;
                case 999:
                    return SourceType.Unknown;
            }
            throw new Exception("Cannot unmarshal type SourceType");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (SourceType)untypedValue;
            switch (value)
            {
                case SourceType.HTML:
                    serializer.Serialize(writer, 1);
                    return;
                case SourceType.Text:
                    serializer.Serialize(writer, 2);
                    return;
                case SourceType.PDF:
                    serializer.Serialize(writer, 3);
                    return;
                case SourceType.DOC:
                    serializer.Serialize(writer, 4);
                    return;
                case SourceType.PPT:
                    serializer.Serialize(writer, 5);
                    return;
                case SourceType.XLS:
                    serializer.Serialize(writer, 5);
                    return;
                case SourceType.PS:
                    serializer.Serialize(writer, 6);
                    return;
                case SourceType.Unknown:
                    serializer.Serialize(writer, 999);
                    return;
            }
            throw new Exception("Cannot marshal type SourceType");
        }
    }

    public enum SourceType {
        HTML, Text, PDF, DOC, PPT, XLS, PS, Unknown
    }

    public class EvaluateResultRoot
    {
        public string expr { get; set; }
        public List<Paper> entities { get; set; }
        public bool timed_out { get; set; }
    }

    
}