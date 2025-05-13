using System;
using System.Collections.Generic;

namespace PlagiarismCheckerMVC.Models
{
    public class QuerySearchResult
    {
        public int ParaNum { get; set; }
        public int SentNum { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public string SourceTitle { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
    }

    public class DocCheckReport
    {
        public string DocumentName { get; set; } = string.Empty;
        public List<QuerySearchResult> Results { get; set; } = new List<QuerySearchResult>();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string SearchEngine { get; set; } = string.Empty;
        public decimal PlagiarismPercentage { get; set; }
    }

    public class SearchItem
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }
}