using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FullTextSearchMvc.Models
{
    public class Article
    {
        public int ArticleId { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public string Author { get; set; }
        
        public string Category { get; set; }
        
        public DateTime PublishedDate { get; set; }
        
        public DateTime LastModified { get; set; }
        
        // Navigation properties
        public List<ArticleTag> ArticleTags { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public class Tag
    {
        public int TagId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        // Navigation properties
        public List<ArticleTag> ArticleTags { get; set; }
    }

    public class ArticleTag
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; }
        
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
