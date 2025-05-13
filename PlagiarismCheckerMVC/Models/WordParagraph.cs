using System;
using System.Collections.Generic;
using System.Linq;

namespace PlagiarismCheckerMVC.Models
{
    public class WordParagraph
    {
        public List<string> Sentences { get; set; } = new List<string>();
        
        public int WordCount => Text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        public int SentCount => Sentences.Count;
        
        public string Text => string.Join(" ", Sentences);
    }
} 