using Microsoft.EntityFrameworkCore;
using FullTextSearchMvc.Models;

namespace FullTextSearchMvc.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match PostgreSQL naming convention
            modelBuilder.Entity<Article>().ToTable("articles");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Tag>().ToTable("tags");
            modelBuilder.Entity<ArticleTag>().ToTable("article_tags");

            // Configure primary keys
            modelBuilder.Entity<Article>().HasKey(a => a.ArticleId);
            modelBuilder.Entity<Category>().HasKey(c => c.CategoryId);
            modelBuilder.Entity<Tag>().HasKey(t => t.TagId);
            
            // Configure composite key for ArticleTag junction table
            modelBuilder.Entity<ArticleTag>()
                .HasKey(at => new { at.ArticleId, at.TagId });

            // Configure column names to match PostgreSQL naming convention
            modelBuilder.Entity<Article>().Property(a => a.ArticleId).HasColumnName("article_id");
            modelBuilder.Entity<Article>().Property(a => a.Title).HasColumnName("title");
            modelBuilder.Entity<Article>().Property(a => a.Content).HasColumnName("content");
            modelBuilder.Entity<Article>().Property(a => a.Author).HasColumnName("author");
            modelBuilder.Entity<Article>().Property(a => a.Category).HasColumnName("category");
            modelBuilder.Entity<Article>().Property(a => a.PublishedDate).HasColumnName("published_date");
            modelBuilder.Entity<Article>().Property(a => a.LastModified).HasColumnName("last_modified");

            modelBuilder.Entity<Category>().Property(c => c.CategoryId).HasColumnName("category_id");
            modelBuilder.Entity<Category>().Property(c => c.Name).HasColumnName("name");
            modelBuilder.Entity<Category>().Property(c => c.Description).HasColumnName("description");

            modelBuilder.Entity<Tag>().Property(t => t.TagId).HasColumnName("tag_id");
            modelBuilder.Entity<Tag>().Property(t => t.Name).HasColumnName("name");

            modelBuilder.Entity<ArticleTag>().Property(at => at.ArticleId).HasColumnName("article_id");
            modelBuilder.Entity<ArticleTag>().Property(at => at.TagId).HasColumnName("tag_id");

            // Configure relationships
            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId);

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId);
        }
    }
}
