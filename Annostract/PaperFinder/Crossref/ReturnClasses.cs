using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using N = Newtonsoft.Json.NullValueHandling;
using System.Linq;
using Martijn.Extensions.Linq;

#pragma warning disable CS8618

namespace Annostract.PaperFinders.Crossref
{
    public partial class CrossRefResult
    {
        [J("status")] public string Status { get; set; }
        [J("message-type")] public string MessageType { get; set; }
        [J("message-version")] public string MessageVersion { get; set; }
        [J("message")] public Message Message { get; set; }
    }

    public partial class Message
    {
        [J("facets")] public Facets Facets { get; set; }
        [J("total-results")] public long TotalResults { get; set; }
        [J("items")] public List<CrossRefSearchResult> Items { get; set; }
        [J("items-per-page")] public long ItemsPerPage { get; set; }
        [J("query")] public Query Query { get; set; }
    }

    public partial class Facets
    {
    }

    public partial class CrossRefSearchResult
    {
        [J("indexed")] public Created Indexed { get; set; }
        [J("publisher-location", NullValueHandling = N.Ignore)] public string PublisherLocation { get; set; }
        [J("reference-count")] public long ReferenceCount { get; set; }
        [J("publisher")] public string Publisher { get; set; }
        [J("isbn-type", NullValueHandling = N.Ignore)] public List<NType> IsbnType { get; set; }
        [J("license", NullValueHandling = N.Ignore)] public List<License> License { get; set; }
        [J("content-domain")] public ContentDomain ContentDomain { get; set; }
        [J("published-print", NullValueHandling = N.Ignore)] public PublishedOnline PublishedPrint { get; set; }
        [J("DOI")] public string Doi { get; set; }
        [J("type")] public string Type { get; set; }
        [J("created")] public Created Created { get; set; }
        [J("source")] public string Source { get; set; }
        [J("is-referenced-by-count")] public long IsReferencedByCount { get; set; }
        [J("title", NullValueHandling = N.Ignore)] public List<string> Title { get; set; }
        [J("prefix")] public string Prefix { get; set; }
        [J("author", NullValueHandling = N.Ignore)] public List<Author> Author { get; set; }
        [J("member")] public string Member { get; set; }
        [J("reference", NullValueHandling = N.Ignore)] public List<Reference> Reference { get; set; }
        [J("event", NullValueHandling = N.Ignore)] public Event Event { get; set; }
        [J("container-title", NullValueHandling = N.Ignore)] public List<string> ContainerTitle { get; set; }
        [J("link", NullValueHandling = N.Ignore)] public List<Link> Link { get; set; }
        [J("deposited")] public Created Deposited { get; set; }
        [J("score")] public double Score { get; set; }
        [J("subtitle", NullValueHandling = N.Ignore)] public List<string> Subtitle { get; set; }
        [J("issued")] public Issued Issued { get; set; }
        [J("ISBN", NullValueHandling = N.Ignore)] public List<string> Isbn { get; set; }
        [J("references-count")] public long ReferencesCount { get; set; }
        [J("URL")] public Uri Url { get; set; }
        [J("relation", NullValueHandling = N.Ignore)] public Relation Relation { get; set; }
        [J("page", NullValueHandling = N.Ignore)] public string Page { get; set; }
        [J("update-policy", NullValueHandling = N.Ignore)] public Uri UpdatePolicy { get; set; }
        [J("published-online", NullValueHandling = N.Ignore)] public PublishedOnline PublishedOnline { get; set; }
        [J("ISSN", NullValueHandling = N.Ignore)] public List<string> Issn { get; set; }
        [J("issn-type", NullValueHandling = N.Ignore)] public List<NType> IssnType { get; set; }
        [J("abstract", NullValueHandling = N.Ignore)] public string Abstract { get; set; }
        [J("institution", NullValueHandling = N.Ignore)] public Institution Institution { get; set; }
        [J("issue", NullValueHandling = N.Ignore)] public string Issue { get; set; }
        [J("short-container-title", NullValueHandling = N.Ignore)] public List<string> ShortContainerTitle { get; set; }
        [J("volume", NullValueHandling = N.Ignore)] public string Volume { get; set; }
        [J("language", NullValueHandling = N.Ignore)] public string Language { get; set; }
        [J("journal-issue", NullValueHandling = N.Ignore)] public JournalIssue JournalIssue { get; set; }
        [J("alternative-id", NullValueHandling = N.Ignore)] public List<string> AlternativeId { get; set; }
        [J("subject", NullValueHandling = N.Ignore)] public List<string> Subject { get; set; }
        [J("edition-number", NullValueHandling = N.Ignore)] public string EditionNumber { get; set; }
        [J("editor", NullValueHandling = N.Ignore)] public List<Author> Editor { get; set; }
        [J("assertion", NullValueHandling = N.Ignore)] public List<Assertion> Assertion { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if(obj != null && obj is CrossRefSearchResult other) {
                return (this.Doi ?? this.Issn?.FirstOrDefault() ?? this.Isbn?.FirstOrDefault() ?? this.Title?.Combine(" ") ?? "HOW") == (other.Doi ?? other.Issn?.FirstOrDefault() ?? other.Isbn?.FirstOrDefault() ?? other.Title?.Combine(" ") ?? "HOW");
            }
            return false;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return (this.Doi ?? this.Issn?.FirstOrDefault() ?? this.Isbn?.FirstOrDefault() ?? this.Title?.Combine(" ") ?? "HOW").GetHashCode();
        }
    }

    public partial class Assertion
    {
        [J("value")] public string Value { get; set; }
        [J("order")] public long Order { get; set; }
        [J("name")] public string Name { get; set; }
        [J("label")] public string Label { get; set; }
        [J("URL", NullValueHandling = N.Ignore)] public Uri Url { get; set; }
    }

    public partial class Author
    {
        [J("given", NullValueHandling = N.Ignore)] public string Given { get; set; }
        [J("family")] public string Family { get; set; }
        [J("sequence")] public string Sequence { get; set; }
        [J("affiliation")] public List<Affiliation> Affiliation { get; set; }
    }

    public partial class Affiliation
    {
        [J("name")] public string Name { get; set; }
    }

    public partial class ContentDomain
    {
        [J("domain")] public List<string> Domain { get; set; }
        [J("crossmark-restriction")] public bool CrossmarkRestriction { get; set; }
    }

    public partial class Created
    {
        [J("date-parts")] public List<List<long>> DateParts { get; set; }
        [J("date-time")] public DateTimeOffset DateTime { get; set; }
        [J("timestamp")] public long Timestamp { get; set; }
    }

    public partial class Event
    {
        [J("name")] public string Name { get; set; }
        [J("location")] public string Location { get; set; }
        [J("sponsor", NullValueHandling = N.Ignore)] public List<string> Sponsor { get; set; }
        [J("acronym", NullValueHandling = N.Ignore)] public string Acronym { get; set; }
        [J("number", NullValueHandling = N.Ignore)] public string Number { get; set; }
        [J("start")] public PublishedOnline Start { get; set; }
        [J("end")] public PublishedOnline End { get; set; }
    }

    public partial class PublishedOnline
    {
        [J("date-parts")] public List<List<long>> DateParts { get; set; }
    }

    public partial class Institution
    {
        [J("name")] public string Name { get; set; }
        [J("place")] public List<string> Place { get; set; }
        [J("acronym")] public List<string> Acronym { get; set; }
    }

    public partial class NType
    {
        [J("value")] public string Value { get; set; }
        [J("type")] public string Type { get; set; }
    }

    public partial class Issued
    {
        [J("date-parts")] public List<List<long?>> DateParts { get; set; }
    }

    public partial class JournalIssue
    {
        [J("published-print")] public PublishedOnline PublishedPrint { get; set; }
        [J("issue")] public string Issue { get; set; }
        [J("published-online", NullValueHandling = N.Ignore)] public PublishedOnline PublishedOnline { get; set; }
    }

    public partial class License
    {
        [J("URL")] public Uri Url { get; set; }
        [J("start")] public Created Start { get; set; }
        [J("delay-in-days")] public long DelayInDays { get; set; }
        [J("content-version")] public string ContentVersion { get; set; }
    }

    public partial class Link
    {
        [J("URL")] public Uri Url { get; set; }
        [J("content-type")] public string ContentType { get; set; }
        [J("content-version")] public string ContentVersion { get; set; }
        [J("intended-application")] public string IntendedApplication { get; set; }

        public bool IsPdf() {
            if(Url.LocalPath.Contains("pdf")) {
                return true;
            }
            
            if(Url.Authority == "dl.acm.org") {
                return true;
            }

            return false;
        }
    }

    public partial class Reference
    {
        [J("key")] public string Key { get; set; }
        [J("unstructured", NullValueHandling = N.Ignore)] public string Unstructured { get; set; }
        [J("DOI", NullValueHandling = N.Ignore)] public string Doi { get; set; }
        [J("doi-asserted-by", NullValueHandling = N.Ignore)] public string DoiAssertedBy { get; set; }
        [J("issue", NullValueHandling = N.Ignore)] public string Issue { get; set; }
        [J("first-page", NullValueHandling = N.Ignore)] public string FirstPage { get; set; }
        [J("volume", NullValueHandling = N.Ignore)] public string Volume { get; set; }
        [J("author", NullValueHandling = N.Ignore)] public string Author { get; set; }
        [J("year", NullValueHandling = N.Ignore)] public string Year { get; set; }
        [J("journal-title", NullValueHandling = N.Ignore)] public string JournalTitle { get; set; }
        [J("volume-title", NullValueHandling = N.Ignore)] public string VolumeTitle { get; set; }
        [J("series-title", NullValueHandling = N.Ignore)] public string SeriesTitle { get; set; }
    }

    public partial class Relation
    {
        [J("cites")] public List<object> Cites { get; set; }
    }

    public partial class Query
    {
        [J("start-index")] public long StartIndex { get; set; }
        [J("search-terms")] public string SearchTerms { get; set; }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
