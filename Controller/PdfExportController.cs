using Microsoft.AspNetCore.Mvc;
using System.IO;
using pdfExxportDemo.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

public class PdfController : Controller
{
    private readonly IWebHostEnvironment _env;

    public PdfController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Content("mwfkkn");
    }

    [HttpPost]
    public async Task<IActionResult> Upload(PdfExport model)
    {
        if (model.PdfFile != null && model.PdfFile.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "pdfs");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, model.PdfFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.PdfFile.CopyToAsync(stream);

            ViewBag.FilePath = "/pdfs/" + model.PdfFile.FileName;
            return View("Index");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Annotate([FromBody] PdfExport model)
    {
        string fileName = Request.Form["fileName"].ToString();
        if (string.IsNullOrEmpty(fileName))
            return BadRequest("Missing filename.");

        string fullPath = Path.Combine(_env.WebRootPath, "pdfs", fileName);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".pdf")
        {
            // PDF Annotation
            string tempPath = fullPath + ".tmp";

            using var reader = new PdfReader(fullPath);
            using var writer = new PdfWriter(tempPath);
            using var pdfDoc = new PdfDocument(reader, writer);
            var page = pdfDoc.GetFirstPage();
            var canvas = new PdfCanvas(page);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            canvas.BeginText()
                  .SetFontAndSize(font, 14)
                  .MoveText(100, 700)
                  .ShowText("Hello from iText7!")
                  .EndText();

            canvas.SetStrokeColor(ColorConstants.BLUE)
                  .Rectangle(100, 600, 150, 75)
                  .Stroke();

            canvas.SetStrokeColor(ColorConstants.RED)
                  .Circle(200, 500, 40)
                  .Stroke();

            pdfDoc.Close();

            System.IO.File.Delete(fullPath);
            System.IO.File.Move(tempPath, fullPath);
        }
        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
        {
            // Image Annotation using System.Drawing.Common
            using var img = System.Drawing.Image.FromFile(fullPath);
            using var g = System.Drawing.Graphics.FromImage(img);

            using var font = new System.Drawing.Font("Arial", 14);
            var brush = System.Drawing.Brushes.Red;
            var pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2);

            // Text
            g.DrawString("Hello from C#!", font, brush, 100, 50);

            // Rectangle
            g.DrawRectangle(pen, 100, 100, 150, 75);

            // Circle (Ellipse)
            g.DrawEllipse(pen, 200, 200, 80, 80);

            img.Save(fullPath); // overwrite original
        }
        else
        {
            return BadRequest("Unsupported file type.");
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult SaveCanvasToPdf([FromBody] CanvasSaveRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ImageData))
        {
            return BadRequest("Missing filename or image data.");
        }

        try
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "pdfs", request.FileName);
            var tempPath = pdfPath + ".tmp";

            var base64 = request.ImageData.Split(',')[1]; // Remove "data:image/png;base64,"
            var imageBytes = Convert.FromBase64String(base64);

            using var reader = new PdfReader(pdfPath);
            using var writer = new PdfWriter(tempPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var page = pdfDoc.GetFirstPage();
            var canvas = new PdfCanvas(page);

            var imgData = iText.IO.Image.ImageDataFactory.Create(imageBytes);
            var image = new iText.Layout.Element.Image(imgData)
                .ScaleToFit(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight())
                .SetFixedPosition(0, 0); // Bottom-left corner

            new iText.Layout.Document(pdfDoc).Add(image);
            pdfDoc.Close();

            System.IO.File.Delete(pdfPath);
            System.IO.File.Move(tempPath, pdfPath);

            var stream = System.IO.File.OpenRead(pdfPath);
            return File(stream, "application/pdf", "Annotated.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error saving PDF: " + ex.Message);
        }
    }

}
