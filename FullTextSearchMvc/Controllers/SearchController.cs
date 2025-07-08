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
                    
                // Populate the CategoryList for the dropdown in the view
                model.CategoryList = model.AvailableCategories
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c,
                        Text = c
                    })
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
        public async Task<IActionResult> Search(string query, string categoryFilter, string authorFilter, string enableFilters)
        {
            bool filtersEnabled = enableFilters == "true";
            _logger.LogInformation("Starting search process with query: {Query}, filters enabled: {FiltersEnabled}", query, filtersEnabled);
            
            // If filters are not enabled, ignore the filter values
            if (!filtersEnabled)
            {
                categoryFilter = null;
                authorFilter = null;
                _logger.LogInformation("Filters disabled, ignoring filter values");
            }
            else
            {
                _logger.LogInformation("Filters enabled, using category filter: {CategoryFilter}, author filter: {AuthorFilter}", categoryFilter, authorFilter);
            }
            
            var model = new SearchModel
            {
                Query = query,
                CategoryFilter = categoryFilter,
                AuthorFilter = authorFilter
            };

            try
            {
                _logger.LogInformation("Getting all articles for the dropdown and filtered display");
                // Get all articles for the dropdown and filtered display
                var allArticles = await _searchService.GetAllArticlesAsync();
                
                // Store the original unfiltered articles for dropdown population
                var unfilteredArticles = allArticles.ToList();
                
                _logger.LogInformation("Getting all available categories for the filter dropdown");
                // Get all available categories for the filter dropdown
                model.AvailableCategories = unfilteredArticles
                    .Where(a => !string.IsNullOrEmpty(a.Category))
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                
                _logger.LogInformation("Populating the CategoryList for the dropdown in the view");
                // Populate the CategoryList for the dropdown in the view
                model.CategoryList = model.AvailableCategories
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c,
                        Text = c
                    })
                    .ToList();
                
                // Apply filters to AllArticles if enabled
                if (filtersEnabled)
                {
                    _logger.LogInformation("Applying filters to all articles");
                    
                    // Apply category filter if specified
                    if (!string.IsNullOrEmpty(model.CategoryFilter))
                    {
                        _logger.LogInformation("Filtering all articles by category: {CategoryFilter}", model.CategoryFilter);
                        _logger.LogInformation("Before filtering all articles by category: {Count} articles", allArticles.Count);
                        allArticles = allArticles.Where(a => a.Category == model.CategoryFilter).ToList();
                        _logger.LogInformation("After filtering all articles by category: {Count} articles", allArticles.Count);
                    }
                    
                    // Apply author filter if specified
                    if (!string.IsNullOrEmpty(model.AuthorFilter))
                    {
                        _logger.LogInformation("Filtering all articles by author: {AuthorFilter}", model.AuthorFilter);
                        _logger.LogInformation("Before filtering all articles by author: {Count} articles", allArticles.Count);
                        allArticles = allArticles.Where(a => a.Author == model.AuthorFilter).ToList();
                        _logger.LogInformation("After filtering all articles by author: {Count} articles", allArticles.Count);
                    }
                }
                
                // Assign the filtered articles to the model
                model.AllArticles = allArticles;
                
                List<SearchResult> results;
                
                // Implement the new conditions
                if (string.IsNullOrWhiteSpace(model.Query))
                {
                    _logger.LogInformation("Query is empty, showing all articles");
                    
                    // Convert filtered articles to search results format
                    results = allArticles.Select(a => new SearchResult
                    {
                        Id = a.ArticleId,
                        Title = a.Title,
                        Content = a.Content,
                        Author = a.Author,
                        Category = a.Category,
                        PublishedDate = a.PublishedDate,
                        // No excerpt or relevance for full listing
                        Excerpt = a.Content.Length > 200 ? a.Content.Substring(0, 200) + "..." : a.Content,
                        Relevance = 0
                    }).ToList();
                }
                else
                {
                    _logger.LogInformation("Using the PostgreSQL full-text search service");
                    // Use the PostgreSQL full-text search service
                    results = await _searchService.SearchAsync(model.Query);
                    
                    // Apply filters if enabled
                    if (filtersEnabled)
                    {
                        _logger.LogInformation("Applying filters to search results");
                        
                        // Apply category filter if specified
                        if (!string.IsNullOrEmpty(model.CategoryFilter))
                        {
                            _logger.LogInformation("Filtering by category: {CategoryFilter}", model.CategoryFilter);
                            _logger.LogInformation("Before filtering by category: {Count} results", results.Count);
                            results = results.Where(r => r.Category == model.CategoryFilter).ToList();
                            _logger.LogInformation("After filtering by category: {Count} results", results.Count);
                        }
                        
                        // Apply author filter if specified
                        if (!string.IsNullOrEmpty(model.AuthorFilter))
                        {
                            _logger.LogInformation("Filtering by author: {AuthorFilter}", model.AuthorFilter);
                            _logger.LogInformation("Before filtering by author: {Count} results", results.Count);
                            results = results.Where(r => r.Author == model.AuthorFilter).ToList();
                            _logger.LogInformation("After filtering by author: {Count} results", results.Count);
                        }
                    }
                }
                
                _logger.LogInformation("Assigning search results to the model");
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
