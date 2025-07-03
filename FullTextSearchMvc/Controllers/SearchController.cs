using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using FullTextSearchMvc.Models;

namespace FullTextSearchMvc.Controllers
{
    public class SearchController : Controller
    {
        // Sample data for demonstration purposes
        private static readonly List<SearchResult> _sampleData = new List<SearchResult>
        {
            new SearchResult { Id = 1, Title = "Introduction to C#", Content = "C# is a modern, object-oriented programming language developed by Microsoft." },
            new SearchResult { Id = 2, Title = "ASP.NET Core MVC", Content = "ASP.NET Core MVC is a web framework for building web apps and APIs using the Model-View-Controller design pattern." },
            new SearchResult { Id = 3, Title = "Entity Framework Core", Content = "Entity Framework Core is an object-database mapper for .NET that enables developers to work with a database using .NET objects." },
            new SearchResult { Id = 4, Title = "JavaScript Basics", Content = "JavaScript is a lightweight, interpreted programming language with object-oriented capabilities." },
            new SearchResult { Id = 5, Title = "Introduction to HTML", Content = "HTML (HyperText Markup Language) is the standard markup language for documents designed to be displayed in a web browser." }
        };

        public IActionResult Index()
        {
            return View(new SearchModel());
        }

        [HttpPost]
        public IActionResult Search(SearchModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return View("Index", model);
            }

            // Simple full-text search implementation
            var query = model.Query.ToLower();
            var results = _sampleData
                .Where(item => 
                    item.Title.ToLower().Contains(query) || 
                    item.Content.ToLower().Contains(query))
                .Select(item => 
                {
                    // Calculate relevance score (simple implementation)
                    double relevance = 0;
                    if (item.Title.ToLower().Contains(query))
                        relevance += 2.0;
                    if (item.Content.ToLower().Contains(query))
                        relevance += 1.0;
                    
                    // Create excerpt with highlighted search term
                    string excerpt = item.Content;
                    if (item.Content.Length > 100)
                    {
                        int index = item.Content.ToLower().IndexOf(query);
                        int startIndex = Math.Max(0, index - 40);
                        int length = Math.Min(100, item.Content.Length - startIndex);
                        excerpt = (startIndex > 0 ? "..." : "") + 
                                 item.Content.Substring(startIndex, length) + 
                                 (startIndex + length < item.Content.Length ? "..." : "");
                    }

                    return new SearchResult
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Content = item.Content,
                        Excerpt = excerpt,
                        Relevance = relevance
                    };
                })
                .OrderByDescending(item => item.Relevance)
                .ToList();

            model.Results = results;
            return View("Index", model);
        }
    }
}
