using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract;
using Martijn.Extensions.AsyncLinq;
using Martijn.Extensions.Linq;
using Martijn.Extensions.Text;

public class LatexSerializer : Serializer
{

    public async Task<string> Serialize(List<ExtractedSource> sources)
    {
        string template = $@"
\documentclass{{article}}
\usepackage[utf8]{{inputenc}}

\title{{Annostract}}
\author{{Annostract}}
\providecommand{{\tightlist}}{{\setlength{{\itemsep}}{{0pt}}\setlength{{\parskip}}{{0pt}}}}
\usepackage{{natbib}}
\usepackage{{graphicx}}
\usepackage{{epigraph}}

\begin{{document}}

\maketitle

{(await sources.Select(i => Serialize(i)).WhenAll()).Combine("\n")}

\bibliographystyle{{plain}}

{sources.SelectMany(i => i.Bibliography).Select(i => $"\\bibliography{{{i.Replace(".bib", "")}}}").Combine("\n")};

\end{{document}}
        ";

        return template;
    }

    private Task<string> Serialize(ExtractedSource source)
    {
        string result = $"\\section{{{source.Name}}}\n";
        var reviews = source.Articles.Where(i => i.Notes.Count > 0).Select(i => Serialize(i)).Combine("\n");

        return Task.FromResult(result + reviews);
    }

    private string Serialize(ExtractedArticle article)
    {
        string abstrac = "";
        var abs = article.Notes.OfType<Abstract>().FirstOrDefault();
        if (abs != null)
        {
            abstrac = "\n" + abs.Content;
        }

        string eolReference = !string.IsNullOrEmpty(article.Reference) ? " \\citep{" + article.Reference + "}": "";


        return $"\\subsection{{{article.Name.EscapeMarkdown() + eolReference}}} {abstrac}{Serialize(article.Notes.OfType<TreeNote>(), eolReference)}{Serialize(article.Notes.OfType<Quote>(), eolReference)}{Serialize(article.Notes.OfType<ImageNote>())} \n\n";
    }

    private object Serialize(IEnumerable<ImageNote> images)
    {
        if (!images.Any())
        {
            return "";
        }
        return "\n\n\\subsubsection{{Images}}\n" + images.Distinct((i, j) => i.Url == j.Url, (i) => i.Url.GetHashCode()).Select(i => $@"
\begin{{figure}}[h!]
\centering
\includegraphics[scale=1.7]{{{i.Url}}}
\caption{{{i.Name}}}
\label{{fig:{i.Name}}}
\end{{figure}}").Combine("\n");
    }

    private object Serialize(IEnumerable<Quote> quotes, string eolReference)
    {
        if (!quotes.Any())
        {
            return "";
        }
        return "\n\n\\subsubsection{{Quotes}}\n" + quotes.Select(i => $@"\epigraph{{{i.Content}}}{{\textit{{{eolReference}}}").Combine("\n");
    }

    private string Serialize(IEnumerable<TreeNote> notes, string eolReference)
{
    if (!notes.Any())
    {
        return "";
    }
    var res = "";
    res += "\\begin{itemize}\n";
    res += "\\tightlist\n";
    res += notes.Select(i => Serialize(i, 0, eolReference)).Combine("\n");
    res += "\n\\end{itemize}";
    return "\n\n\\subsubsection{{Takeaways}}\n" + res;
}

    private string Serialize(TreeNote n, int indent, string eolReference)
    {
        string res = " ".Repeat(indent * 3) +"\\item " + n.Content + eolReference;
        if(n.Children.Count > 0)
        {
            res += "\n" + " ".Repeat(indent * 3) + "\\begin{itemize}\n";
            res += " ".Repeat(indent * 3) + "\\tightlist\n";
            res += n.Children.Select(i => Serialize(i, indent + 1, eolReference)).Combine("\n");
            res += "\n" + " ".Repeat(indent * 3) + "\\end{itemize}";
        }

        return res;
    }
}