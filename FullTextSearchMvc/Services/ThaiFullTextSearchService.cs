using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using FullTextSearchMvc.Models;

namespace FullTextSearchMvc.Services
{
    public class ThaiFullTextSearchService
    {
        private readonly string _connectionString;
        private readonly ILogger<ThaiFullTextSearchService> _logger;

        public ThaiFullTextSearchService(IConfiguration configuration, ILogger<ThaiFullTextSearchService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            
            // Log connection string availability
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Database connection string is missing or empty");
            }
            else
            {
                _logger.LogInformation("Thai database connection string is configured");
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();
            string formattedQuery = FormatSearchQuery(query);

            try
            {
                _logger.LogInformation("Attempting to connect to database for Thai search query: {Query}", query);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection established successfully for Thai search operation");

                    // Execute the full-text search query for Thai articles
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT a.article_id, a.title, a.content, a.author, a.category, a.published_date,
                                   ts_rank(a.search_vector, to_tsquery('simple', @query)) AS relevance,
                                   ts_headline('simple', a.content, to_tsquery('simple', @query),
                                              'StartSel = <mark>, StopSel = </mark>, MaxWords=35, MinWords=15') AS excerpt
                            FROM thai_articles a
                            WHERE a.search_vector @@ to_tsquery('simple', @query)
                            ORDER BY relevance DESC
                            LIMIT 20";
                        cmd.Parameters.AddWithValue("query", formattedQuery);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new SearchResult
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    Excerpt = reader.GetString(7),  // Highlighted excerpt
                                    Relevance = reader.GetDouble(6),
                                    Author = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                    Category = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                    PublishedDate = !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.MinValue
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed during Thai search operation: {ErrorMessage}", ex.Message);
                // Return empty results on error
                return new List<SearchResult>();
            }

            return results;
        }

        private string FormatSearchQuery(string query)
        {
            // Convert the query to a format suitable for PostgreSQL ts_query
            // This handles basic AND operations between words for Thai language
            return string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
        
        public async Task<List<Article>> GetAllThaiArticlesAsync()
        {
            var articles = new List<Article>();
            
            try
            {
                _logger.LogInformation("Attempting to connect to database to retrieve all Thai articles");
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection established successfully for retrieving Thai articles");
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT article_id, title, content, author, category, published_date, last_modified
                            FROM thai_articles
                            ORDER BY published_date DESC";
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                articles.Add(new Article
                                {
                                    ArticleId = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    Author = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                    Category = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                    PublishedDate = !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.MinValue,
                                    LastModified = !reader.IsDBNull(6) ? reader.GetDateTime(6) : DateTime.MinValue
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed when retrieving all Thai articles: {ErrorMessage}", ex.Message);
                // Return empty list on error
                return new List<Article>();
            }
            
            return articles;
        }
        
        public async Task<Article> GetThaiArticleByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve Thai article with ID {Id}", id);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT article_id, title, content, author, category, published_date, last_modified
                            FROM thai_articles
                            WHERE article_id = @id";
                        cmd.Parameters.AddWithValue("id", id);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Article
                                {
                                    ArticleId = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    Author = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                    Category = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                    PublishedDate = !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.MinValue,
                                    LastModified = !reader.IsDBNull(6) ? reader.GetDateTime(6) : DateTime.MinValue
                                };
                            }
                        }
                    }
                }
                
                _logger.LogWarning("Thai article with ID {Id} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Thai article with ID {Id}: {ErrorMessage}", id, ex.Message);
                return null;
            }
        }
        
        public async Task<List<string>> GetThaiCategoriesAsync()
        {
            var categories = new List<string>();
            
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT DISTINCT category
                            FROM thai_articles
                            WHERE category IS NOT NULL
                            ORDER BY category";
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categories.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Thai categories: {ErrorMessage}", ex.Message);
            }
            
            return categories;
        }
        
        public async Task<bool> CreateThaiArticleAsync(Article article)
        {
            try
            {
                _logger.LogInformation("Attempting to create a new Thai article with title: {Title}", article.Title);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            INSERT INTO thai_articles (title, content, author, category, published_date, last_modified)
                            VALUES (@title, @content, @author, @category, @published_date, @last_modified)
                            RETURNING article_id";
                        
                        cmd.Parameters.AddWithValue("title", article.Title);
                        cmd.Parameters.AddWithValue("content", article.Content);
                        cmd.Parameters.AddWithValue("author", (object)article.Author ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("category", (object)article.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("published_date", article.PublishedDate);
                        cmd.Parameters.AddWithValue("last_modified", article.LastModified);
                        
                        // Execute the command and get the new ID
                        int newId = (int)await cmd.ExecuteScalarAsync();
                        article.ArticleId = newId;
                        
                        _logger.LogInformation("Thai article created successfully with ID: {ArticleId}", newId);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Thai article: {ErrorMessage}", ex.Message);
                return false;
            }
        }
        
        public async Task<bool> UpdateThaiArticleAsync(Article article)
        {
            try
            {
                _logger.LogInformation("Attempting to update Thai article with ID: {ArticleId}", article.ArticleId);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            UPDATE thai_articles
                            SET title = @title, content = @content, author = @author, 
                                category = @category, last_modified = @last_modified
                            WHERE article_id = @article_id";
                        
                        cmd.Parameters.AddWithValue("article_id", article.ArticleId);
                        cmd.Parameters.AddWithValue("title", article.Title);
                        cmd.Parameters.AddWithValue("content", article.Content);
                        cmd.Parameters.AddWithValue("author", (object)article.Author ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("category", (object)article.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("last_modified", DateTime.Now);
                        
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        bool success = rowsAffected > 0;
                        
                        if (success)
                        {
                            _logger.LogInformation("Thai article with ID {ArticleId} updated successfully", article.ArticleId);
                        }
                        else
                        {
                            _logger.LogWarning("Thai article with ID {ArticleId} was not updated, no matching record found", article.ArticleId);
                        }
                        
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Thai article with ID {ArticleId}: {ErrorMessage}", article.ArticleId, ex.Message);
                return false;
            }
        }
    }
}
