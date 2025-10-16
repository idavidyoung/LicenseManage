using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SoftwareLicenseManage.Models
{
    // 授权类型枚举
    public enum LicenseType
    {
        Trial,
        Standard,
        Professional,
        Enterprise
    }

    // 授权信息类
    public class LicenseInfo
    {
        public string ProductName { get; set; }
        public string Version { get; set; }
        public LicenseType LicenseType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int MaxUsers { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string HardwareId { get; set; } // 机器码
        public string[] Features { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
    }

    // 机器码生成器（基于硬件信息）
    public static class HardwareIdGenerator
    {
        public static string GetHardwareId()
        {
            StringBuilder sb = new StringBuilder();

            // 1. 获取CPU序列号
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        sb.Append(mo["ProcessorId"]?.ToString());
                        break;
                    }
                }
            }
            catch { /* 忽略错误 */ }

            // 2. 获取主硬盘序列号
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia WHERE Tag LIKE '%0'"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        sb.Append(mo["SerialNumber"]?.ToString().Trim());
                        break;
                    }
                }
            }
            catch { /* 忽略错误 */ }

            // 3. 使用SHA256生成哈希作为机器码
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16); // 取前16位
            }
        }
    }

    // 授权管理器（核心类）
    public class LicenseManager
    {
        private RSAParameters _privateKey;
        private RSAParameters _publicKey;

        public LicenseManager()
        {
            // 生成RSA密钥对（2048位）
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                _privateKey = rsa.ExportParameters(true);
                _publicKey = rsa.ExportParameters(false);
            }
        }

        // 生成授权文件（使用私钥签名）
        public string GenerateLicense(LicenseInfo licenseInfo)
        {
            var xmlDoc = new XmlDocument();
            XmlNode licenseNode = xmlDoc.CreateElement("License");
            xmlDoc.AppendChild(licenseNode);

            // 添加授权信息到XML
            AddNode(xmlDoc, licenseNode, "Product", licenseInfo.ProductName);
            AddNode(xmlDoc, licenseNode, "Version", licenseInfo.Version);
            AddNode(xmlDoc, licenseNode, "LicenseType", licenseInfo.LicenseType.ToString());
            AddNode(xmlDoc, licenseNode, "ExpiryDate", licenseInfo.ExpiryDate.ToString("yyyy-MM-dd"));
            AddNode(xmlDoc, licenseNode, "MaxUsers", licenseInfo.MaxUsers.ToString());
            AddNode(xmlDoc, licenseNode, "CustomerName", licenseInfo.CustomerName);
            AddNode(xmlDoc, licenseNode, "CustomerEmail", licenseInfo.CustomerEmail);
            AddNode(xmlDoc, licenseNode, "HardwareId", licenseInfo.HardwareId);
            AddNode(xmlDoc, licenseNode, "Features", string.Join(",", licenseInfo.Features));

            // 添加自定义数据
            if (licenseInfo.CustomData != null)
            {
                XmlNode customNode = xmlDoc.CreateElement("CustomData");
                licenseNode.AppendChild(customNode);
                foreach (var item in licenseInfo.CustomData)
                {
                    AddNode(xmlDoc, customNode, item.Key, item.Value);
                }
            }

            // 对XML内容进行数字签名
            string licenseXml = xmlDoc.OuterXml;
            string signature = SignData(licenseXml);

            // 将签名添加到XML
            AddNode(xmlDoc, licenseNode, "Signature", signature);

            return xmlDoc.OuterXml;
        }
        public bool SaveLicenseToFile(string licenseXml,string path)
        {
            try
            {
                System.IO.File.WriteAllText(path, licenseXml);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool LoadLicenseFromFile(string path, out string licenseXml)
        {
            try
            {
                licenseXml = System.IO.File.ReadAllText(path);
                return true;
            }
            catch
            {
                licenseXml = null;
                return false;
            }
        }
        // 验证授权文件（使用公钥验证签名）
        public bool ValidateLicense(string licenseXml, string currentMachineCode, out LicenseInfo licenseInfo)
        {
            licenseInfo = null;
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(licenseXml);

                // 提取签名并移除（验证时需要原始数据）
                XmlNode signatureNode = xmlDoc.SelectSingleNode("//Signature");
                if (signatureNode == null) return false;
                string signature = signatureNode.InnerText;
                signatureNode.ParentNode.RemoveChild(signatureNode);

                // 验证数字签名
                string dataToVerify = xmlDoc.OuterXml;
                if (!VerifyData(dataToVerify, signature))
                    return false;

                // 解析授权信息
                licenseInfo = new LicenseInfo
                {
                    ProductName = GetNodeValue(xmlDoc, "Product"),
                    Version = GetNodeValue(xmlDoc, "Version"),
                    LicenseType = Enum.Parse<LicenseType>(GetNodeValue(xmlDoc, "LicenseType")),
                    ExpiryDate = DateTime.Parse(GetNodeValue(xmlDoc, "ExpiryDate")),
                    MaxUsers = int.Parse(GetNodeValue(xmlDoc, "MaxUsers")),
                    CustomerName = GetNodeValue(xmlDoc, "CustomerName"),
                    CustomerEmail = GetNodeValue(xmlDoc, "CustomerEmail"),
                    HardwareId = GetNodeValue(xmlDoc, "HardwareId"),
                    Features = GetNodeValue(xmlDoc, "Features").Split(','),
                    CustomData = new Dictionary<string, string>()
                };

                // 解析自定义数据
                XmlNode customNode = xmlDoc.SelectSingleNode("//CustomData");
                if (customNode != null)
                {
                    foreach (XmlNode node in customNode.ChildNodes)
                    {
                        licenseInfo.CustomData.Add(node.Name, node.InnerText);
                    }
                }

                // 检查授权是否过期
                if (licenseInfo.ExpiryDate < DateTime.Now)
                    return false;

                // 检查机器码是否匹配（关键绑定机制）
                if (licenseInfo.HardwareId != currentMachineCode)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        // 导出公钥（用于客户端验证）
        public string ExportPublicKey()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(_publicKey);
                return rsa.ToXmlString(false);
            }
        }

        // 导入公钥（客户端使用）
        public void ImportPublicKey(string publicKeyXml)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKeyXml);
                _publicKey = rsa.ExportParameters(false);
            }
        }

        // 辅助方法：添加XML节点
        private void AddNode(XmlDocument doc, XmlNode parent, string name, string value)
        {
            XmlNode node = doc.CreateElement(name);
            node.InnerText = value;
            parent.AppendChild(node);
        }

        // 辅助方法：获取XML节点值
        private string GetNodeValue(XmlDocument doc, string nodeName)
        {
            XmlNode node = doc.SelectSingleNode($"//{nodeName}");
            return node?.InnerText ?? string.Empty;
        }

        // 使用私钥签名数据
        private string SignData(string data)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(_privateKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signatureBytes);
            }
        }

        // 使用公钥验证签名
        private bool VerifyData(string data, string signature)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(_publicKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }
}
