using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace NcmToMp3.Services
{
    /// <summary>
    /// NCM转MP3核心服务（在线接口版，无本地依赖）
    /// 适配VS2026 + .NET 10
    /// </summary>
    public class NcmConvertService
    {
        // 稳定的在线转换接口（无需注册/密钥）
        private readonly HttpClient _httpClient;
        private const string ConvertApiUrl = "https://tool.liumingye.cn/music/api/convert";

        public NcmConvertService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // 延长超时，适配大文件
            };
            // 解决HTTPS证书问题
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// 在线转换NCM到MP3
        /// </summary>
        /// <param name="ncmFilePath">本地NCM文件路径</param>
        /// <param name="mp3SavePath">MP3保存路径</param>
        /// <returns>是否转换成功</returns>
        public async Task<bool> ConvertNcmToMp3Async(string ncmFilePath, string mp3SavePath)
        {
            try
            {
                // Windows权限处理（仅Windows生效）
                if (OperatingSystem.IsWindows())
                {
                    SetFileAccessPermissions(ncmFilePath);
                }

                // 校验文件
                if (!File.Exists(ncmFilePath))
                {
                    Console.WriteLine("NCM文件不存在");
                    return false;
                }

                var fileInfo = new FileInfo(ncmFilePath);
                if (fileInfo.Length > 1024 * 1024 * 100) // 限制100MB内（接口限制）
                {
                    Console.WriteLine("文件大小超过100MB，无法转换");
                    return false;
                }

                // 构建表单上传请求
                using var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(ncmFilePath);
                content.Add(new StreamContent(fileStream), "file", fileInfo.Name);
                content.Add(new StringContent("ncm"), "type");
                content.Add(new StringContent("mp3"), "format");

                // 调用在线转换接口
                Console.WriteLine("开始调用在线接口转换...");
                var response = await _httpClient.PostAsync(ConvertApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"接口调用失败：{response.StatusCode}");
                    return false;
                }

                // 保存MP3文件
                using var mp3Stream = await response.Content.ReadAsStreamAsync();
                using var fs = new FileStream(mp3SavePath, FileMode.Create);
                await mp3Stream.CopyToAsync(fs);

                // 清理临时NCM文件
                if (File.Exists(ncmFilePath))
                {
                    File.Delete(ncmFilePath);
                }

                Console.WriteLine($"转换成功！MP3保存路径：{mp3SavePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Windows文件权限设置（消除CA1416警告）
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private void SetFileAccessPermissions(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    Environment.UserName,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                fileInfo.SetAccessControl(fileSecurity);
            }
            catch (Exception permEx)
            {
                Console.WriteLine($"权限设置失败（不影响转换）：{permEx.Message}");
            }
        }
    }
}