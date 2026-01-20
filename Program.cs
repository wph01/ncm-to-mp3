var builder = WebApplication.CreateBuilder(args);

// 添加MVC支持
builder.Services.AddControllersWithViews();
// 注册转换服务
builder.Services.AddSingleton<NcmToMp3.Services.NcmConvertService>();
// 注册HttpClient（在线接口依赖）
builder.Services.AddHttpClient();

// 大文件上传配置（适配.NET 10）
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824; // 1GB
    // .NET 10已移除 UnknownRequestBodyLengthLimit，直接删除即可
});

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();