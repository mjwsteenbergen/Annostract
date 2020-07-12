using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Annostract.PaperFinders.Crossref;
using ApiLibs;
using ApiLibs.General;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;

namespace Annostract.PaperFinders
{
    public static class BibtexMaker
    {
        public static async Task CreateBibtex(string path, List<CrossRefSearchResult> results)
        {
            path = path ?? throw new ArgumentNullException(nameof(path));

            var downloader = new BibtexDownloader();

            // var res = await results.Select(async i => )).WhenAll();

            List<(CrossRefSearchResult, string)> res = new List<(CrossRefSearchResult, string)>();
            try
            {
                foreach (var item in results)
                {
                    res.Add((item, await downloader.Get(item.Doi)));
                }

                var contents = res.Select(i => Regex.Replace(i.Item2, "(@[^\\{]+\\{)[^,]+", "$1" + SourceToBibDoiId(i.Item1))).CombineWithNewLine();
                File.WriteAllText(path, contents);
            } 
            catch(NoInternetException)
            {
                Console.WriteLine("No internet. Could not create .bib file");
            }
        }


        class BibtexDownloader : Service
        {
            public BibtexDownloader() : base("https://dx.doi.org/", false)
            {
            }

            public Task<string> Get(string doi) => MakeRequest<string>(doi, headers: new List<Param>{
                new Param("Accept", "application/x-bibtex; q = 1")
            });

            protected override async Task<string> HandleRequest(string url, Call call = Call.GET, List<Param>? parameters = null, List<Param>? headers = null, object? content = null, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                try
                {
                    return await base.HandleRequest(url, call, parameters, headers, content, statusCode);
                }
                catch (NoInternetException e)
                {
                    if (e.Message == "The operation has timed out." || e.Message == "The request timed-out.")
                    {
                        return await HandleRequest(url, call, parameters, headers, content, statusCode);
                    }
                    throw e;
                }
            }
        }

        public static string SourceToBibDoiId(CrossRefSearchResult item) => item != null ? (item.Author?.Take(3).Select(i => i.Family).Combine((i, j) => i + j) + (item.Title.FirstOrDefault(i => i != null) ?? "").Split(' ').First()).Where(i => char.IsLetterOrDigit(i)).Select(i => i.ToString()).Combine((i ,j) => i+j) : "";

    }
}