using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FullTextSearchMvc.Models;
using FullTextSearchMvc.Services;

namespace FullTextSearchMvc.Controllers
{
    public class ThaiArticleController : Controller
    {
        private readonly ThaiFullTextSearchService _thaiSearchService;
        private readonly ILogger<ThaiArticleController> _logger;

        public ThaiArticleController(ThaiFullTextSearchService thaiSearchService, ILogger<ThaiArticleController> logger)
        {
            _thaiSearchService = thaiSearchService;
            _logger = logger;
        }
        
        // GET: ThaiArticle/Create
        public IActionResult Create()
        {
            return View(new Article
            {
                PublishedDate = DateTime.Now,
                LastModified = DateTime.Now
            });
        }
        
        // POST: ThaiArticle/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Article article)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool success = await _thaiSearchService.CreateThaiArticleAsync(article);
                    
                    if (success)
                    {
                        _logger.LogInformation("New Thai article created successfully with title: {Title}", article.Title);
                        return RedirectToAction("Index", "SearchThai");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create new Thai article with title: {Title}", article.Title);
                        ModelState.AddModelError(string.Empty, "Failed to create the Thai article. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new Thai article: {Message}", ex.Message);
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the Thai article. Please try again.");
                }
            }
            
            return View(article);
        }

        // GET: ThaiArticle/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var article = await _thaiSearchService.GetThaiArticleByIdAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("Thai article with ID {Id} not found", id);
                    return NotFound();
                }

                return View(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Thai article with ID {Id} for editing: {Message}", id, ex.Message);
                return RedirectToAction("Index", "SearchThai");
            }
        }

        // POST: ThaiArticle/Edit/5
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
                    await _thaiSearchService.UpdateThaiArticleAsync(article);
                    _logger.LogInformation("Thai article with ID {Id} updated successfully", id);
                    return RedirectToAction("Index", "SearchThai");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating Thai article with ID {Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the Thai article. Please try again.");
                }
            }

            return View(article);
        }
    }
}
