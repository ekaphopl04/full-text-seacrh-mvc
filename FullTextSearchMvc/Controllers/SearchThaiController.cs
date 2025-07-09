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
    public class SearchThaiController : Controller
    {
        private readonly ThaiFullTextSearchService _thaiSearchService;
        private readonly ILogger<SearchThaiController> _logger;

        public SearchThaiController(ThaiFullTextSearchService thaiSearchService, ILogger<SearchThaiController> logger)
        {
            _thaiSearchService = thaiSearchService;
            _logger = logger;
            _logger.LogInformation("SearchThaiController initialized");
        }

        public async Task<IActionResult> Index(string categoryFilter = null)
        {
            _logger.LogInformation("Loading Thai Index page and retrieving all Thai articles");
            var model = new SearchModel
            {
                CategoryFilter = categoryFilter
            };
            
            try
            {
                // Get all Thai articles
                model.AllArticles = await _thaiSearchService.GetAllThaiArticlesAsync();
                _logger.LogInformation("Successfully retrieved {Count} Thai articles", model.AllArticles?.Count ?? 0);
                
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
                _logger.LogError(ex, "Error retrieving Thai articles for Index page: {Message}", ex.Message);
                model.AllArticles = new List<Article>();
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string query, string categoryFilter, string authorFilter, string enableFilters)
        {
            bool filtersEnabled = enableFilters == "true";
            _logger.LogInformation("Starting Thai search process with query: {Query}, filters enabled: {FiltersEnabled}", query, filtersEnabled);
            
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
                _logger.LogInformation("Getting all Thai articles for the dropdown and filtered display");
                // Get all articles for the dropdown and filtered display
                var allArticles = await _thaiSearchService.GetAllThaiArticlesAsync();
                
                // Store the original unfiltered articles for dropdown population
                var unfilteredArticles = allArticles.ToList();
                
                _logger.LogInformation("Getting all available Thai categories for the filter dropdown");
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
                    _logger.LogInformation("Applying filters to all Thai articles");
                    
                    // Apply category filter if specified
                    if (!string.IsNullOrEmpty(model.CategoryFilter))
                    {
                        _logger.LogInformation("Filtering all Thai articles by category: {CategoryFilter}", model.CategoryFilter);
                        _logger.LogInformation("Before filtering all Thai articles by category: {Count} articles", allArticles.Count);
                        allArticles = allArticles.Where(a => a.Category == model.CategoryFilter).ToList();
                        _logger.LogInformation("After filtering all Thai articles by category: {Count} articles", allArticles.Count);
                    }
                    
                    // Apply author filter if specified
                    if (!string.IsNullOrEmpty(model.AuthorFilter))
                    {
                        _logger.LogInformation("Filtering all Thai articles by author: {AuthorFilter}", model.AuthorFilter);
                        _logger.LogInformation("Before filtering all Thai articles by author: {Count} articles", allArticles.Count);
                        allArticles = allArticles.Where(a => a.Author == model.AuthorFilter).ToList();
                        _logger.LogInformation("After filtering all Thai articles by author: {Count} articles", allArticles.Count);
                    }
                }
                
                // Assign the filtered articles to the model
                model.AllArticles = allArticles;
                
                List<SearchResult> results;
                
                // Implement the new conditions
                if (string.IsNullOrWhiteSpace(model.Query))
                {
                    _logger.LogInformation("Query is empty, showing all Thai articles");
                    
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
                    _logger.LogInformation("Using the PostgreSQL full-text search service for Thai articles");
                    // Use the PostgreSQL full-text search service
                    results = await _thaiSearchService.SearchAsync(model.Query);
                    
                    // Apply filters if enabled
                    if (filtersEnabled)
                    {
                        _logger.LogInformation("Applying filters to Thai search results");
                        
                        // Apply category filter if specified
                        if (!string.IsNullOrEmpty(model.CategoryFilter))
                        {
                            _logger.LogInformation("Filtering Thai results by category: {CategoryFilter}", model.CategoryFilter);
                            _logger.LogInformation("Before filtering by category: {Count} results", results.Count);
                            results = results.Where(r => r.Category == model.CategoryFilter).ToList();
                            _logger.LogInformation("After filtering by category: {Count} results", results.Count);
                        }
                        
                        // Apply author filter if specified
                        if (!string.IsNullOrEmpty(model.AuthorFilter))
                        {
                            _logger.LogInformation("Filtering Thai results by author: {AuthorFilter}", model.AuthorFilter);
                            _logger.LogInformation("Before filtering by author: {Count} results", results.Count);
                            results = results.Where(r => r.Author == model.AuthorFilter).ToList();
                            _logger.LogInformation("After filtering by author: {Count} results", results.Count);
                        }
                    }
                }
                
                _logger.LogInformation("Assigning Thai search results to the model");
                model.Results = results;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Thai search error: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while performing the Thai search. Please try again.");
                
                _logger.LogInformation("Using fallback search method due to database error");
                // Fallback to simple search if database search fails
                model.Results = FallbackThaiSearch(model.Query);
            }

            return View("Index", model);
        }

        // Fallback search method in case the database search fails
        private List<SearchResult> FallbackThaiSearch(string query)
        {
            // Sample data for Thai search demonstration purposes
            var sampleData = new List<SearchResult>
            {
                new SearchResult { Id = 1, Title = "ข้าวมันไก่ สูตรต้นตำรับ", Content = "ข้าวมันไก่เป็นอาหารจานเดียวที่ได้รับความนิยมทั่วไทย เสิร์ฟพร้อมน้ำจิ้มรสจัดและน้ำซุปไก่ใสร้อนๆ ถือเป็นเมนูง่ายๆ แต่อร่อยและอิ่มท้อง", Author = "สมศักดิ์ ชิมดี", Category = "อาหารจานด่วน" },
                new SearchResult { Id = 2, Title = "ต้มยำกุ้ง รสชาติไทยแท้", Content = "ต้มยำกุ้งเป็นเมนูขึ้นชื่อของไทย มีรสเผ็ด เปรี้ยว หอมสมุนไพร เช่น ตะไคร้ ใบมะกรูด และพริกสด นิยมใส่กุ้งตัวใหญ่เพื่อความกลมกล่อม", Author = "สมหญิง รักกิน", Category = "อาหารทะเล" },
                new SearchResult { Id = 3, Title = "แกงเขียวหวานไก่ หอมกะทิกลมกล่อม", Content = "แกงเขียวหวานเป็นแกงไทยที่มีรสเผ็ดหวานเล็กน้อย มักใส่ไก่ มะเขือเปราะ และใบโหระพา นิยมกินคู่กับข้าวสวยหรือขนมจีน", Author = "อนันต์ อร่อยดี", Category = "อาหารภาคกลาง" },
                new SearchResult { Id = 4, Title = "ผัดไทย รสชาติระดับโลก", Content = "ผัดไทยเป็นอาหารจานเส้นที่มีชื่อเสียงไปทั่วโลก ทำจากเส้นจันท์ผัดกับไข่ เต้าหู้ กุ้งแห้ง และซอสเปรี้ยวหวาน โรยถั่วลิสงและมะนาว", Author = "วราภรณ์ ชวนชิม", Category = "อาหารจานด่วน" },
                new SearchResult { Id = 5, Title = "ข้าวซอย อาหารเหนือหอมเครื่องแกง", Content = "ข้าวซอยเป็นเมนูเส้นของภาคเหนือ ใช้เส้นบะหมี่ไข่ในน้ำแกงกะทิรสจัดจ้าน ใส่ไก่หรือลูกชิ้น โรยหอมเจียวและเส้นกรอบ", Author = "ศุภชัย ลิ้มลอง", Category = "อาหารภาคเหนือ" }
            };

            if (string.IsNullOrWhiteSpace(query)) 
                return sampleData;
                
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
                        Author = item.Author,
                        Category = item.Category,
                        Excerpt = excerpt,
                        Relevance = relevance
                    };
                })
                .OrderByDescending(item => item.Relevance)
                .ToList();
        }
    }
}
