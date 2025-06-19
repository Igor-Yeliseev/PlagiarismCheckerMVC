namespace PlagiarismCheckerMVC.Models
{
    public class JwtSettings
    {
        public string SecurityKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; }
    }

    public class CloudStorageSettings
    {
        public string BucketName { get; set; } = string.Empty;
    }

    public class SearchEngineSettings
    {
        public SearchApiSettings Google { get; set; } = new SearchApiSettings();
        public SearchApiSettings Yandex { get; set; } = new SearchApiSettings();
    }

    public class SearchApiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SearchEngineId { get; set; } = string.Empty;
        public int ResultsCount { get; set; } = 10;
    }

    public class ParagraphSettings
    {
        /// <summary> Минимальное количество предложений в параграфе </summary>
        public int SentMinCount { get; set; }

        /// <summary> Минимальное количество символов в параграфе </summary>
        public int ParaMinChars { get; set; }

        /// <summary> Минимальное количество символов в предложении </summary>
        public int SentMinChars { get; set; }
    }
}