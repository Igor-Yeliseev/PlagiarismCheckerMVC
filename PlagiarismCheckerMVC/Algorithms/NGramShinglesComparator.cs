using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlagiarismCheckerMVC.Algorithms
{
    public class NGramShingleComparator
    {
        // Генерация n-грамм для текста
        private static IEnumerable<string> GenerateNGrams(string text, int n)
        {
            if (text.Length < n)
            {
                yield return text; // Возвращаем весь текст, если он короче n
                yield break;
            }

            for (int i = 0; i <= text.Length - n; i++)
            {
                yield return text.Substring(i, n);
            }
        }

        // Получение хешей n-грамм
        public static HashSet<string> GetNGramHashes(string text, int n)
        {
            var ngrams = GenerateNGrams(text, n);
            var hashes = new HashSet<string>();

            foreach (var ngram in ngrams)
            {
                hashes.Add(GetHash(ngram));
            }

            return hashes;
        }

        // Вычисление схожести (коэффициент Жаккара)
        public static double CalculateSimilarity(HashSet<string> set1, HashSet<string> set2)
        {
            if (set1.Count == 0 || set2.Count == 0)
                return 0.0;

            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();

            return (intersection / (double)union) * 100;
        }

        // Генерация MD5 хеша (для примера; можно заменить на SHA256)
        private static string GetHash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }

    // Пример использования
    //public class Program
    //{
    //    public static void Main()
    //    {
    //        string text1 = "Кот сидит на ковре";
    //        string text2 = "Кот сидит на ковре и смотрит в окно";
    //        int nGramSize = 3; // Размер n-граммы (например, 3 символа)

    //        // Удаляем пробелы для чистого сравнения символов
    //        string cleanText1 = text1.Replace(" ", "");
    //        string cleanText2 = text2.Replace(" ", "");

    //        var set1 = NGramShingleComparator.GetNGramHashes(cleanText1, nGramSize);
    //        var set2 = NGramShingleComparator.GetNGramHashes(cleanText2, nGramSize);

    //        double similarity = NGramShingleComparator.CalculateSimilarity(set1, set2);
    //        Console.WriteLine($"Схожесть на основе {nGramSize}-грамм: {similarity:F2}%");
    //    }
    //}
}
