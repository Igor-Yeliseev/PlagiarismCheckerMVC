
namespace PlagiarismCheckerMVC.Models
{
    /// <summary> Элемент поисковой выдачи </summary>
    public class SearchItem
    {
        /// <summary> Заголовок найденного элемента </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary> Ссылка на найденный элемент </summary>
        public string Link { get; set; } = string.Empty;

        /// <summary> Краткое описание найденного элемента </summary>
        public string Snippet { get; set; } = string.Empty;
    }
} 