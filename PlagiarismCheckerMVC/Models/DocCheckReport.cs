namespace PlagiarismCheckerMVC.Models
{
    /// <summary> Отчёт поиска плагиата в документе среди источников из сети </summary>
    public class DocCheckReport
    {
        /// <summary> Название проверяемого документа </summary>
        public string DocumentName { get; set; } = string.Empty;

        /// <summary> Результаты поиска плагиата по каждому запросу </summary>
        public List<QuerySearchResult> QueryResults { get; set; } = new List<QuerySearchResult>();

        /// <summary> Дата и время проверки документа </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary> Используемая поисковая система </summary>
        public string SearchEngine { get; set; } = string.Empty;

        /// <summary> Процент плагиата в документе </summary>
        public PlagPercentageInfo PlagiarismPercentage { get; set; }
    }

    public struct PlagPercentageInfo
    {
        public double? WebPlagPercentage { get; set; }
        public double? DbPlagPercentage { get; set; }
    }
}