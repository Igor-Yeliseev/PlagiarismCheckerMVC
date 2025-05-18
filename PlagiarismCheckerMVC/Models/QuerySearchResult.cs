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
} 