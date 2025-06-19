
namespace PlagiarismCheckerMVC.Models
{
    /// <summary> Результат поиска плагиата по одному запросу </summary>
    public class QuerySearchResult
    {
        /// <summary> Номер параграфа в документе </summary>
        public int ParaNum { get; set; }

        /// <summary> Номер предложения в параграфе </summary>
        public int SentNum { get; set; }

        /// <summary> URL источника плагиата </summary>
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary> Заголовок источника плагиата </summary>
        public string SourceTitle { get; set; } = string.Empty;

        /// <summary> Коэффициент схожести текста (от 0 до 1) </summary>
        public double SimilarityScore { get; set; }
    }
} 