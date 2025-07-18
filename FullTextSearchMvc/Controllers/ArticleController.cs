using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FullTextSearchMvc.Models;
using FullTextSearchMvc.Services;

namespace FullTextSearchMvc.Controllers
{
    public class ArticleController : Controller
    {
        private readonly FullTextSearchService _searchService;
        private readonly ILogger<ArticleController> _logger;

        public ArticleController(FullTextSearchService searchService, ILogger<ArticleController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }
        
        // GET: Article/Create
        public IActionResult Create()
        {
            return View(new Article
            {
                PublishedDate = DateTime.Now,
                LastModified = DateTime.Now
            });
        }
        
        // POST: Article/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Article article)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool success = await _searchService.CreateArticleAsync(article);
                    
                    if (success)
                    {
                        _logger.LogInformation("New article created successfully with title: {Title}", article.Title);
                        return RedirectToAction("Index", "Search");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create new article with title: {Title}", article.Title);
                        ModelState.AddModelError(string.Empty, "Failed to create the article. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new article: {Message}", ex.Message);
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the article. Please try again.");
                }
            }
            
            return View(article);
        }

        // GET: Article/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var article = await _searchService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("Article with ID {Id} not found", id);
                    return NotFound();
                }

                return View(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article with ID {Id} for editing: {Message}", id, ex.Message);
                return RedirectToAction("Index", "Search");
            }
        }

        // POST: Article/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Article article)
        {
            if (id != article.ArticleId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _searchService.UpdateArticleAsync(article);
                    _logger.LogInformation("Article with ID {Id} updated successfully", id);
                    return RedirectToAction("Index", "Search");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating article with ID {Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the article. Please try again.");
                }
            }

            return View(article);
        }
    }
}
