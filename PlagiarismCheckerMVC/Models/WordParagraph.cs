using System.Collections.Generic;

namespace PlagiarismCheckerMVC.Models
{
    /// <summary> Класс, представляющий параграф текста из документа Word </summary>
    public class WordParagraph
    {
        /// <summary> Список предложений в параграфе </summary>
        public List<string> Sentences { get; set; } = new List<string>();
        
        /// <summary> Количество слов в параграфе </summary>
        public int WordCount => Text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        /// <summary> Количество предложений в параграфе </summary>
        public int SentCount => Sentences.Count;
        
        /// <summary> Полный текст параграфа </summary>
        public string Text => string.Join(" ", Sentences);
    }
} 