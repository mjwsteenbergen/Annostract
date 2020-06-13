

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ApiLibs;
using ApiLibs.General;

namespace Annostract.PaperFinders.Crossref
{
    class CrossRefService : Service
    {
        public CrossRefService() : base("https://api.crossref.org/") {
            Client.Timeout = 2000;
         }

        public async Task<List<CrossRefSearchResult>> Find(string text) => (await MakeRequest<CrossRefResult>("works", parameters: new List<Param> {
                new Param("query", text)
            })).Message.Items;

        protected override async Task<string> HandleRequest(string url, Call call = Call.GET, List<Param>? parameters = null, List<Param>? headers = null, object? content = null, HttpStatusCode statusCode = HttpStatusCode.OK) {
            try {
                return await base.HandleRequest(url, call, parameters, headers, content, statusCode);
            } catch(NoInternetException e) {
                if(e.Message == "The operation has timed out." || e.Message == "The request timed-out.") {
                    Console.WriteLine("Timeout. Trying again");
                    return await HandleRequest(url, call, parameters, headers, content, statusCode);
                } else {
                    Console.WriteLine(e.Message);
                }
                throw e;
            }
        }
    }
}