namespace HangScheduler.Api.Configuration
{
    public class Database
    {
        public string DatabaseName { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string SslMode { get; set; }
        public string SslKeyPath { get; set;}
        public string SslCertPath { get; set;}
        public string SslRootCertPath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }
}
