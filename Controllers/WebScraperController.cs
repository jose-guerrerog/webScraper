using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using AngleSharp;
using System.IO;
using website_scraper.Models;
using CsvHelper;

namespace website_scraper.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WebScraperController : ControllerBase
    {
        /**
         * The website I'm scraping has data where the paths are relative
         * so I need a base url set somewhere to build full url's
         */

        private readonly String websiteUrl = "https://townshiptale.gamepedia.com/Category:Items";
        private readonly ILogger<WebScraperController> _logger;

        // Constructor
        public WebScraperController(ILogger<WebScraperController> logger)
        {
            _logger = logger;
        }

        private IActionResult ExportCSV(List<Category> data) {
            
            using (var memoryStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(memoryStream))
                {
                    var writer = new CsvWriter(sw, System.Globalization.CultureInfo.CurrentCulture);
 
                    writer.WriteField("Name");
                    writer.WriteField("Description");
                    writer.WriteField("Image Path");
                    writer.NextRecord();

                    foreach (Category dataField in data)
                    { 
                       writer.WriteField(dataField.Title);
                       writer.WriteField(dataField.Description);
                       writer.WriteField(dataField.ImgSrc);
                       writer.NextRecord();
                    }

                    sw.Flush();
                    return File( memoryStream.ToArray(), "text/csv", "myScraper.csv" ); 
                }
            }
        }

        private async Task<IActionResult>  GetPageData(String url) {
            
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            var categoriesByLetter = document.QuerySelectorAll("div.mw-category li a");

            var records = new List<Category>();
            
            foreach (var item in categoriesByLetter)
            {
                String originItem = item.BaseUrl.Origin;
                String hrefItem = item.GetAttribute("Href");
                String urlItem = originItem + hrefItem;
                var documentItem = await context.OpenAsync(urlItem);

                var title = documentItem.GetElementById("section_0").TextContent;
                var description = documentItem.QuerySelectorAll("div.mf-section-0 p")[0].TextContent;
                var img = documentItem.QuerySelectorAll("div.mf-section-0 img");
                var imgSrc = ""; 
                
                if (img.Length > 0) {
                    imgSrc = documentItem.QuerySelectorAll("div.mf-section-0 img")[0].GetAttribute("src");
                }
                
                records.Add(new Category {
                    Title = title,
                    Description = description,
                    ImgSrc = imgSrc,
                });
            }

            return ExportCSV(records);
            
        }

        [HttpGet]
        public Task<IActionResult> Get()
        {
            return GetPageData(websiteUrl);
            
        }
    }
}
