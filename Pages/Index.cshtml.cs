using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public IndexModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    [BindProperty]
    public IFormFile PdfFile { get; set; }

    public string UploadedFilePath { get; set; }

    public async Task OnPostAsync()
    {
        if (PdfFile != null && PdfFile.Length > 0)
        {
            var uploads = Path.Combine(_env.WebRootPath, "pdfs");
            Directory.CreateDirectory(uploads);

            var filePath = Path.Combine(uploads, PdfFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await PdfFile.CopyToAsync(stream);

            UploadedFilePath = "/pdfs/" + PdfFile.FileName;
        }
    }
   

}
