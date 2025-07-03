using System.Collections.Generic;

namespace FullTextSearchMvc.Models
{
    public class SearchModel
    {
        public string Query { get; set; }
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();
    }

    public class SearchResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public double Relevance { get; set; }
    }
}
