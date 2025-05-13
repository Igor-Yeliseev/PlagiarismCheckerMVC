using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlagiarismCheckerMVC.Models
{
    [Table("DocumentCheckResults")]
    public class DocumentCheckResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid DocumentId { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Значение должно быть от 0 до 1")]
        public decimal Originality { get; set; }

        [Required]
        public DateTime CheckedAt { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document? Document { get; set; }
    }
} 