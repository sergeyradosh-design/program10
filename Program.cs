using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<GalleryService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public class ImageModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class GalleryService
{
    public List<ImageModel> Images { get; } = new()
    {
        new ImageModel { Title = "Одеса", Description = "Изглед към морето", FilePath = "https://picsum.photos/300/200?random=1" },
        new ImageModel { Title = "Залез", Description = "Красива вечер", FilePath = "https://picsum.photos/300/200?random=2" },
        new ImageModel { Title = "Град", Description = "Нощни светлини", FilePath = "https://picsum.photos/300/200?random=3" }
    };
}

public class ImageGalleryViewComponent : ViewComponent
{
    private readonly GalleryService _service;

    public ImageGalleryViewComponent(GalleryService service)
    {
        _service = service;
    }

    public IViewComponentResult Invoke()
    {
        var html = new StringBuilder("<div class='row'>");
        foreach (var img in _service.Images)
        {
            html.Append($@"
                <div class='col-md-4 mb-3'>
                    <div class='card shadow-sm'>
                        <img src='{img.FilePath}' class='card-img-top' alt='{img.Title}' style='height:200px; object-fit:cover;' />
                        <div class='card-body'>
                            <h5 class='card-title'>{img.Title}</h5>
                            <p class='card-text'>{img.Description}</p>
                        </div>
                    </div>
                </div>");
        }
        html.Append("</div>");

        return ViewComponentContent(html.ToString());
    }

    private IViewComponentResult ViewComponentContent(string htmlContent)
    {
        return new ContentViewComponentResult(htmlContent);
    }
}

public class ContentViewComponentResult : IViewComponentResult
{
    private readonly string _content;
    public ContentViewComponentResult(string content) => _content = content;

    public void Execute(ViewContext context)
    {
        context.Writer.Write(_content);
    }

    public Task ExecuteAsync(ViewContext context)
    {
        Execute(context);
        return Task.CompletedTask;
    }
}

public class HomeController : Controller
{
    private readonly GalleryService _galleryService;
    private readonly IWebHostEnvironment _env;

    public HomeController(GalleryService galleryService, IWebHostEnvironment env)
    {
        _galleryService = galleryService;
        _env = env;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var html = @"
            <!DOCTYPE html>
            <html lang='bg'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <title>Интерактивна Галерия</title>
                <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <body class='bg-light'>
                <div class='container my-4'>
                    <h1 class='mb-4 text-center'>Интерактивна Галерия с Изображения</h1>
                    
                    <div class='card p-3 mb-4 shadow-sm'>
                        <h4>Качване на ново изображение</h4>
                        <form action='/Home/Upload' method='post' enctype='multipart/form-data' class='row g-3'>
                            <div class='col-md-4'>
                                <input type='text' name='title' class='form-control' placeholder='Заглавие' required />
                            </div>
                            <div class='col-md-4'>
                                <input type='text' name='description' class='form-control' placeholder='Описание' required />
                            </div>
                            <div class='col-md-4'>
                                <input type='file' name='file' class='form-control' accept='image/*' required />
                            </div>
                            <div class='col-12'>
                                <button type='submit' class='btn btn-primary'>Качи</button>
                            </div>
                        </form>
                    </div>

                    <h3>Галерия (чрез ViewComponent):</h3>
                    " + RenderGalleryComponent() + @"
                </div>
            </body>
            </html>";

        return Content(html, "text/html", Encoding.UTF8);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(string title, string description, IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var wwwroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploads = Path.Combine(wwwroot, "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _galleryService.Images.Add(new ImageModel
            {
                Title = title,
                Description = description,
                FilePath = $"/uploads/{fileName}"
            });
        }

        return RedirectToAction("Index");
    }

    private string RenderGalleryComponent()
    {
        var component = new ImageGalleryViewComponent(_galleryService);
        var result = component.Invoke() as ContentViewComponentResult;

        var writer = new StringWriter();
        var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext
        {
            Writer = writer
        };
        result?.Execute(viewContext);
        return writer.ToString();
    }
}
