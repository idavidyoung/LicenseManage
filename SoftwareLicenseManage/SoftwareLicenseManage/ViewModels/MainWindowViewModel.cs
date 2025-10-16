using Prism.Mvvm;
using SoftwareLicenseManage.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using LicenseManager = SoftwareLicenseManage.Models.LicenseManager;

namespace SoftwareLicenseManage.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
            // 1. 生成机器码（客户端操作）
            string machineCode = HardwareIdGenerator.GetHardwareId();
            Console.WriteLine($"生成的机器码: {machineCode}");
            Console.WriteLine("请将此机器码发送给软件提供商以获取授权文件\n");

            // 2. 模拟软件提供商生成授权文件（使用私钥）
            LicenseManager providerLicenseManager = new LicenseManager();

            LicenseInfo licenseInfo = new LicenseInfo
            {
                ProductName = "我的软件",
                Version = "2.0",
                LicenseType = LicenseType.Professional,
                ExpiryDate = DateTime.Now.AddYears(1), // 有效期1年
                MaxUsers = 1,
                CustomerName = "张三",
                CustomerEmail = "zhangsan@example.com",
                HardwareId = machineCode, // 绑定到特定机器
                Features = new string[] { "高级功能", "技术支持", "自动更新" },
                CustomData = new Dictionary<string, string>
                {
                    { "OrderId", "ORD20250001" },
                    { "SalesAgent", "李四" }
                }
            };

            string licenseXml = providerLicenseManager.GenerateLicense(licenseInfo);
            string publicKey = providerLicenseManager.ExportPublicKey();
            providerLicenseManager.SaveLicenseToFile(licenseXml, "授权文件.lic");

            Console.WriteLine("授权文件生成成功！");
            Console.WriteLine($"公钥（需嵌入客户端）:\n{publicKey}\n");
            Console.WriteLine($"授权文件内容:\n{licenseXml}\n");

            // 3. 模拟客户端验证授权（使用公钥）
            LicenseManager clientLicenseManager = new LicenseManager();
            clientLicenseManager.ImportPublicKey(publicKey); // 导入公钥

            string currentMachineCode = HardwareIdGenerator.GetHardwareId(); // 重新获取当前机器码

            if (clientLicenseManager.ValidateLicense(licenseXml, currentMachineCode, out LicenseInfo validatedLicense))
            {
                Console.WriteLine("✅ 授权验证成功！");
                Console.WriteLine($"产品名称: {validatedLicense.ProductName}");
                Console.WriteLine($"版本: {validatedLicense.Version}");
                Console.WriteLine($"授权类型: {validatedLicense.LicenseType}");
                Console.WriteLine($"客户姓名: {validatedLicense.CustomerName}");
                Console.WriteLine($"客户邮箱: {validatedLicense.CustomerEmail}");
                Console.WriteLine($"过期时间: {validatedLicense.ExpiryDate:yyyy-MM-dd}");
                Console.WriteLine($"功能特性: {string.Join(", ", validatedLicense.Features)}");

                // 显示自定义数据
                foreach (var item in validatedLicense.CustomData)
                {
                    Console.WriteLine($"{item.Key}: {item.Value}");
                }
            }
            else
            {
                Console.WriteLine("❌ 授权验证失败！软件无法运行。");
            }

        }
    }
}
