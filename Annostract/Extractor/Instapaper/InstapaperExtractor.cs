using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annostract;
using ApiLibs.General;
using ApiLibs.Instapaper;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Memory = Martijn.Extensions.Memory.Memory;

namespace Annostract
{
    public class InstapaperExtractor : Extractor
    {
        public string Folder { get; set; }
        public string LocalFolderPath { get; }

        public InstapaperExtractor(string folder, string localfolderPath)
        {
            Folder = folder;
            LocalFolderPath = localfolderPath;
        }

        public async Task<ExtractedSource?> Extract()
        {
            Passwords passwords;
            try {
                passwords = await Passwords.ReadPasswords();
            } 
            catch(Exception e)
            {
                throw new Exception("There are no credentials stored for instapaper. Please add them.", e);
            }

            InstapaperService instaper = new InstapaperService(passwords.Instaper_ID, passwords.Instaper_secret, passwords.Instaper_user_token, passwords.Instaper_user_secret);

            try
            {
                await instaper.GetBookmarks(null, 1);
            }
            catch (ForbiddenException)
            {
                return null;
            }

            var bminfo = await instaper.GetAllBookmarkInfo((await instaper.GetFolder(Folder)).folder_id.ToString());


            var arts = bminfo.bookmarks.Select(bm => (bm, i: bminfo.highlights.Where(i => i.bookmark_id == bm.bookmark_id))).Select(j => {
                var bm = j.bm;
                var i = j.i;
                var desc = bm.description == "" ? new List<Note>() : new List<Note> { new Abstract(bm.description) };
                return new ExtractedArticle(bm.url)
                {
                    Name = bm.title,
                    Notes = i.Select(j => new TreeNote(j.text)).Concat(desc).ToList(),
                    Reference = $"{bm.bookmark_id}"
                };
            });

            List<string> bib = new List<string>();
            if(LocalFolderPath != null)
            {
                var biber = bminfo.bookmarks.Select(i => $@"
@misc{{{i.bookmark_id},title = {{{i.title}}}, 
	url         = {{{i.url}}}, 
	author      = {{{ParseAuthor(i.url)}}}, 
	year        = {{{DateTime.Now.Year}}}, 
	note        = {{(Accessed on {DateTime.Now.ToString()})}}
}}").Combine("\n");
                File.WriteAllText(LocalFolderPath + "Annostract.Instapaper.bib", biber);
                bib.Add("Annostract.Instapaper.bib");
                // "<md-bib>\n" + bminfo.bookmarks.Select(i => $"\t<md-bib-url id=\"{i.bookmark_id}\">{i.url}</md-bib-url>").Combine("\n") + "\n<md-bib/>"
            }

            return new ExtractedSource("Instapaper", arts.ToList())
            {
                Bibliography = bib
            };
        }

        private object ParseAuthor(string url)
        {
            if(url != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return "";
        }
    }
}