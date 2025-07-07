using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FullTextSearchMvc.Models
{
    public class SearchModel
    {
        public string Query { get; set; }
        public string CategoryFilter { get; set; }
        public string AuthorFilter { get; set; }
        public string SearchType { get; set; } = "regular"; // Default to regular search
        public List<string> AvailableCategories { get; set; } = new List<string>();
        public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();
        public List<Article> AllArticles { get; set; } = new List<Article>();
    }

    public class SearchResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public double Relevance { get; set; }
        
        // Additional fields from the database
        public string Author { get; set; }
        public string Category { get; set; }
        public DateTime PublishedDate { get; set; }
    }
}
