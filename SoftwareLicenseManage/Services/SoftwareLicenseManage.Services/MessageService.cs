using SoftwareLicenseManage.Services.Interfaces;

namespace SoftwareLicenseManage.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage()
        {
            return "Hello from the Message Service";
        }
    }
}
