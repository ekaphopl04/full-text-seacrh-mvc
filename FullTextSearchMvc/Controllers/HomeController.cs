using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FullTextSearchMvc.Models;
using Microsoft.AspNetCore.Http;

namespace FullTextSearchMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // ถ้าไม่มีการตั้งค่าภาษา ให้ใช้ภาษาอังกฤษเป็นค่าเริ่มต้น
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Language")))
        {
            HttpContext.Session.SetString("Language", "en");
        }
        return View();
    }

    [HttpPost]
    public IActionResult SetLanguage(string language)
    {
        HttpContext.Session.SetString("Language", language);
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
