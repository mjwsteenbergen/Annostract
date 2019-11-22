using System.Threading.Tasks;
using ApiLibs;

namespace Annostract.PaperFinder
{
    class DoiFind : Service
    {
        public DoiFind() : base("http://dx.doi.org/") { }

        public Task<string> GetContentFromDoi(string doi) {
            return MakeRequest<string>(doi);
        }
    }
}