using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract;
using Annostract.PaperFinder.Crossref;
using ApiLibs.General;
using ApiLibs.Instapaper;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Memory = Martijn.Extensions.Memory.Memory;

namespace Annostract
{
    public class InstapaperExtractor {

        public static async Task<List<ExtractedInstapaperArticle>> Extract() {
            Passwords passwords = await Passwords.ReadPasswords();

            InstapaperService instaper = new InstapaperService(passwords.Instaper_ID, passwords.Instaper_secret, passwords.Instaper_user_token, passwords.Instaper_user_secret);

            try {
                await instaper.GetBookmarks(null, 1);
            } catch (ForbiddenException e) {
                return new List<ExtractedInstapaperArticle>();
            }

            var bminfo = await instaper.GetAllBookmarkInfo("archive");

            return bminfo.highlights.GroupBy(i => i.bookmark_id).Select(i => {
                var bm = bminfo.bookmarks.Find(j => j.bookmark_id == i.Key);

                var desc = bm.description == "" ? new List<Result>() : new List<Result> { new HighlightResult(bm.description, "abstract") };
                return new ExtractedInstapaperArticle(bm.title) {
                    Results = i.Select(j => new HighlightResult(j.text, null)).Concat(desc).ToList(),
                    Url = bm.url
                };
            }).ToList();
        }
    }

    public class ExtractedInstapaperArticle : ExtractedDocument
    {
        public ExtractedInstapaperArticle(string title) : base(title)
        {
        }

        public string Url { get; internal set; }
    }
}