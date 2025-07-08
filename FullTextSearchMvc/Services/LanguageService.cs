using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FullTextSearchMvc.Services
{
    public class LanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        private Dictionary<string, Dictionary<string, string>> _resources;

        public LanguageService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _resources = new Dictionary<string, Dictionary<string, string>>();
            LoadResources();
        }

        private void LoadResources()
        {
            var resourcePath = Path.Combine(_environment.ContentRootPath, "Resources");
            
            // Load English resources
            var enPath = Path.Combine(resourcePath, "SharedResource.en.json");
            if (File.Exists(enPath))
            {
                var jsonString = File.ReadAllText(enPath);
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                _resources["en"] = resources;
            }
            
            // Load Thai resources
            var thPath = Path.Combine(resourcePath, "SharedResource.th.json");
            if (File.Exists(thPath))
            {
                var jsonString = File.ReadAllText(thPath);
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                _resources["th"] = resources;
            }
        }

        public string GetCurrentLanguage()
        {
            var language = _httpContextAccessor.HttpContext?.Session.GetString("Language");
            return string.IsNullOrEmpty(language) ? "en" : language;
        }

        public string GetTranslation(string key)
        {
            var language = GetCurrentLanguage();
            
            if (_resources.ContainsKey(language) && _resources[language].ContainsKey(key))
            {
                return _resources[language][key];
            }
            
            // Fallback to English
            if (_resources.ContainsKey("en") && _resources["en"].ContainsKey(key))
            {
                return _resources["en"][key];
            }
            
            // Return the key if no translation is found
            return key;
        }
    }
}
