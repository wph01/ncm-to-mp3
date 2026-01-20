using Microsoft.AspNetCore.Mvc;
using NcmToMp3.Services;
using System.IO;
using System;

namespace NcmToMp3.Controllers
{
    public class HomeController : Controller
    {
        private readonly NcmConvertService _convertService;
        private readonly IWebHostEnvironment _webHostEnv;

        public HomeController(NcmConvertService convertService, IWebHostEnvironment webHostEnv)
        {
            _convertService = convertService;
            _webHostEnv = webHostEnv;
        }

        // 首页（上传界面）
        public IActionResult Index() => View();

        // 异步处理文件上传和转换
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1073741824)] // 仅保留大文件配置
        public async Task<IActionResult> Convert(IFormFile file)
        {
            // 校验文件：必须是NCM文件，且不为空
            if (file == null || file.Length == 0 || !file.FileName.EndsWith(".ncm", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "请上传有效的NCM格式音频文件（大小≤100MB）！";
                return View("Index");
            }

            try
            {
                // 1. 创建uploads文件夹（存放上传/转换后的文件）
                var uploadsFolder = Path.Combine(_webHostEnv.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                // 2. 保存用户上传的NCM文件到本地（临时文件）
                var ncmFileName = $"{Guid.NewGuid()}.ncm"; // 生成唯一文件名，避免重复
                var ncmFilePath = Path.Combine(uploadsFolder, ncmFileName);

                await using (var stream = System.IO.File.Create(ncmFilePath)) // 用完整命名空间避免冲突
                {
                    await file.CopyToAsync(stream); // 异步保存文件，不卡界面
                }

                // 3. 定义MP3文件的保存路径
                var mp3FileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.mp3";
                var mp3FilePath = Path.Combine(uploadsFolder, mp3FileName);

                // 4. 调用在线转换服务（核心：异步调用）
                var isSuccess = await _convertService.ConvertNcmToMp3Async(ncmFilePath, mp3FilePath);

                // 5. 根据转换结果给用户反馈
                if (isSuccess)
                {
                    ViewBag.Success = true; // 告诉前端转换成功
                    ViewBag.DownloadUrl = $"/uploads/{mp3FileName}"; // MP3下载链接
                    ViewBag.FileName = mp3FileName; // 显示给用户的文件名
                }
                else
                {
                    ViewBag.Error = "转换失败！可能是文件加密/接口暂时不可用，请稍后重试。";
                }
            }
            catch (Exception ex)
            {
                // 捕获所有异常，给用户友好提示
                ViewBag.Error = $"系统错误：{ex.Message}";
            }

            // 回到首页，显示结果
            return View("Index");
        }
    }
}