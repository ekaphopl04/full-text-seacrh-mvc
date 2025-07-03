using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FullTextSearchMvc.Models;
using FullTextSearchMvc.Services;

namespace FullTextSearchMvc.Controllers
{
    public class SearchController : Controller
    {
        private readonly FullTextSearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(FullTextSearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
            _logger.LogInformation("SearchController initialized");
        }

        public async Task<IActionResult> Index(string categoryFilter = null)
        {
            _logger.LogInformation("Loading Index page and retrieving all articles");
            var model = new SearchModel
            {
                CategoryFilter = categoryFilter
            };
            
            try
            {
                // Get all articles
                model.AllArticles = await _searchService.GetAllArticlesAsync();
                _logger.LogInformation("Successfully retrieved {Count} articles", model.AllArticles?.Count ?? 0);
                
                // Get all available categories for the filter dropdown
                model.AvailableCategories = model.AllArticles
                    .Where(a => !string.IsNullOrEmpty(a.Category))
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving articles for Index page: {Message}", ex.Message);
                model.AllArticles = new List<Article>();
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string query, string categoryFilter)
        {
            var model = new SearchModel
            {
                Query = query,
                CategoryFilter = categoryFilter
            };
            
            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return RedirectToAction("Index", new { categoryFilter = model.CategoryFilter });
            }

            try
            {
                // Get all articles for the dropdown and filtered display
                model.AllArticles = await _searchService.GetAllArticlesAsync();
                
                // Get all available categories for the filter dropdown
                model.AvailableCategories = model.AllArticles
                    .Where(a => !string.IsNullOrEmpty(a.Category))
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                
                // Use the PostgreSQL full-text search service
                var results = await _searchService.SearchAsync(model.Query);
                
                // Apply category filter if selected
                if (!string.IsNullOrEmpty(model.CategoryFilter))
                {
                    results = results.Where(r => r.Category == model.CategoryFilter).ToList();
                }
                
                model.Results = results;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Search error: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while performing the search. Please try again.");
                
                _logger.LogInformation("Using fallback search method due to database error");
                // Fallback to simple search if database search fails
                model.Results = FallbackSearch(model.Query);
            }

            return View("Index", model);
        }

        // Fallback search method in case the database search fails
        private List<SearchResult> FallbackSearch(string query)
        {
            // Sample data for demonstration purposes
            var sampleData = new List<SearchResult>
            {
                new SearchResult { Id = 1, Title = "Introduction to C#", Content = "C# is a modern, object-oriented programming language developed by Microsoft." },
                new SearchResult { Id = 2, Title = "ASP.NET Core MVC", Content = "ASP.NET Core MVC is a web framework for building web apps and APIs using the Model-View-Controller design pattern." },
                new SearchResult { Id = 3, Title = "Entity Framework Core", Content = "Entity Framework Core is an object-database mapper for .NET that enables developers to work with a database using .NET objects." },
                new SearchResult { Id = 4, Title = "JavaScript Basics", Content = "JavaScript is a lightweight, interpreted programming language with object-oriented capabilities." },
                new SearchResult { Id = 5, Title = "Introduction to HTML", Content = "HTML (HyperText Markup Language) is the standard markup language for documents designed to be displayed in a web browser." }
            };

            query = query.ToLower();
            return sampleData
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
        }
    }
}
