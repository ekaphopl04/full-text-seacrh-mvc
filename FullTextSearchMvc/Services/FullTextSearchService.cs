using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using FullTextSearchMvc.Models;

namespace FullTextSearchMvc.Services
{
    public class FullTextSearchService
    {
        private readonly string _connectionString;
        private readonly ILogger<FullTextSearchService> _logger;

        public FullTextSearchService(IConfiguration configuration, ILogger<FullTextSearchService> logger)
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
                _logger.LogInformation("Database connection string is configured");
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
                _logger.LogInformation("Attempting to connect to database for search query: {Query}", query);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection established successfully for search operation");

                    // Execute the full-text search query
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT a.article_id, a.title, a.content, a.author, a.category, a.published_date,
                                   ts_rank(a.search_vector, to_tsquery('english', @query)) AS relevance,
                                   ts_headline('english', a.content, to_tsquery('english', @query),
                                              'StartSel = <mark>, StopSel = </mark>, MaxWords=35, MinWords=15') AS excerpt
                            FROM articles a
                            WHERE a.search_vector @@ to_tsquery('english', @query)
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
                _logger.LogError(ex, "Database connection failed during search operation: {ErrorMessage}", ex.Message);
                // Return empty results on error
                return new List<SearchResult>();
            }

            return results;
        }

        private string FormatSearchQuery(string query)
        {
            // Convert the query to a format suitable for PostgreSQL ts_query
            // This handles basic AND operations between words
            return string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
        
        public async Task<List<Article>> GetAllArticlesAsync()
        {
            var articles = new List<Article>();
            
            try
            {
                _logger.LogInformation("Attempting to connect to database to retrieve all articles");
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection established successfully for retrieving articles");
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT article_id, title, content, author, category, published_date, last_modified
                            FROM articles
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
                _logger.LogError(ex, "Database connection failed when retrieving all articles: {ErrorMessage}", ex.Message);
                // Return empty list on error
                return new List<Article>();
            }
            
            return articles;
        }
        
        public async Task<Article> GetArticleByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve article with ID {Id}", id);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            SELECT article_id, title, content, author, category, published_date, last_modified
                            FROM articles
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
                
                _logger.LogWarning("Article with ID {Id} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article with ID {Id}: {ErrorMessage}", id, ex.Message);
                return null;
            }
        }
        
        public async Task<bool> UpdateArticleAsync(Article article)
        {
            try
            {
                _logger.LogInformation("Attempting to update article with ID {Id}", article.ArticleId);
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = @"
                            UPDATE articles
                            SET title = @title,
                                content = @content,
                                author = @author,
                                category = @category,
                                last_modified = CURRENT_TIMESTAMP
                            WHERE article_id = @id;
                            
                            -- Update the search vector
                            UPDATE articles
                            SET search_vector = setweight(to_tsvector('english', title), 'A') ||
                                               setweight(to_tsvector('english', COALESCE(content, '')), 'B') ||
                                               setweight(to_tsvector('english', COALESCE(author, '')), 'C') ||
                                               setweight(to_tsvector('english', COALESCE(category, '')), 'D')
                            WHERE article_id = @id";
                            
                        cmd.Parameters.AddWithValue("id", article.ArticleId);
                        cmd.Parameters.AddWithValue("title", article.Title);
                        cmd.Parameters.AddWithValue("content", article.Content);
                        cmd.Parameters.AddWithValue("author", article.Author ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("category", article.Category ?? (object)DBNull.Value);
                        
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article with ID {Id}: {ErrorMessage}", article.ArticleId, ex.Message);
                return false;
            }
        }
    }
}
