using Microsoft.EntityFrameworkCore;

namespace PlagiarismCheckerMVC.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentCheckResult> DocumentCheckResults { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка уникальности email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            
            // Связь один-ко-многим между пользователем и документами
            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Связь между Document и DocumentCheckResult (один к одному)
            modelBuilder.Entity<Document>()
                .HasOne<DocumentCheckResult>(d => d.DocumentCheckResult)
                .WithOne(cr => cr.Document)
                .HasForeignKey<DocumentCheckResult>(cr => cr.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 