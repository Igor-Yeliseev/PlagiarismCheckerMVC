using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlagiarismCheckerMVC.Models
{
    [Table("DocumentCheckResults")]
    public class DocumentCheckResult
    {
        [Key, ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Значение должно быть от 0 до 1")]
        public decimal Originality { get; set; }

        [Required]
        public DateTime CheckedAt { get; set; }

        public virtual Document? Document { get; set; }
    }
} 