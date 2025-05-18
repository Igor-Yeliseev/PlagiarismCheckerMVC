namespace PlagiarismCheckerMVC.Models
{
    public class DocCheckReport
    {
        public string DocumentName { get; set; } = string.Empty;
        public List<QuerySearchResult> Results { get; set; } = new List<QuerySearchResult>();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string SearchEngine { get; set; } = string.Empty;
        public decimal PlagiarismPercentage { get; set; }
    }
} 