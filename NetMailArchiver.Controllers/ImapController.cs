using MailKit.Net.Imap;
using NetMailArchiver.Models;

namespace NetMailArchiver.Controllers
{
    public class ImapController(ImapInformation imapInfomation)
    {
        private ImapClient _client = new ImapClient();

        public void ConnectAndAuthenticate()
        {
            if (_client.IsConnected && _client.IsAuthenticated) return;
            
            try
            {
                _client.Connect(imapInfomation.Host, imapInfomation.Port, imapInfomation.UseSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);
                _client.Authenticate(imapInfomation.Username, imapInfomation.Password);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsConnectedAndAuthenticated()
        {
            if (_client.IsConnected && _client.IsAuthenticated) return true;
            return false;
        }
    }
}
