using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using FullTextSearchMvc.Models;

namespace FullTextSearchMvc.Services
{
    public class FullTextSearchService
    {
        private readonly string _connectionString;

        public FullTextSearchService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

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
                Console.WriteLine($"Database error: {ex.Message}");
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
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
                Console.WriteLine($"Database error when retrieving all articles: {ex.Message}");
                // Return empty list on error
                return new List<Article>();
            }
            
            return articles;
        }
    }
}
