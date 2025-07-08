using Microsoft.AspNetCore.Razor.TagHelpers;
using FullTextSearchMvc.Services;

namespace FullTextSearchMvc.TagHelpers
{
    [HtmlTargetElement("translate", TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("translate", Attributes = "key")]
    public class TranslateTagHelper : TagHelper
    {
        private readonly LanguageService _languageService;

        public TranslateTagHelper(LanguageService languageService)
        {
            _languageService = languageService;
        }

        [HtmlAttributeName("key")]
        public string Key { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content.SetHtmlContent(_languageService.GetTranslation(Key));
        }
    }
}
