using System;

namespace llcom.Model
{
    /// <summary>
    /// MQTT 测试页相关设置（Settings 分部类）。
    /// </summary>
    internal partial class Settings
    {
        private string _mqttServer = "broker.emqx.io";
        private int _mqttPort = 1883;
        private string _mqttClientID = Guid.NewGuid().ToString();
        private bool _mqttTLS = false;
        private bool _mqttTLSCert = false;
        private string _mqttTLSCertCaPath = "";
        private string _mqttTLSCertClientPath = "";
        private string _mqttTLSCertClientPassword = "";
        private bool _mqttWs = false;
        private string _mqttWsPath = "/mqtt";
        private string _mqttUser = "user";
        private string _mqttPassword = "password";
        private int _mqttKeepAlive = 120;
        private bool _mqttCleanSession = false;
        private string _mqttPublishTopic = "your/publish/topic";
        private string _mqttSubscribeTopic = "your/subcribe/topic";

        public string mqttServer { get => _mqttServer; set { if (SetProperty(ref _mqttServer, value)) Save(); } }
        public int mqttPort { get => _mqttPort; set { if (SetProperty(ref _mqttPort, value)) Save(); } }
        public string mqttClientID { get => _mqttClientID; set { if (SetProperty(ref _mqttClientID, value)) Save(); } }
        public bool mqttTLS { get => _mqttTLS; set { if (SetProperty(ref _mqttTLS, value)) Save(); } }
        public bool mqttTLSCert { get => _mqttTLSCert; set { if (SetProperty(ref _mqttTLSCert, value)) Save(); } }
        public string mqttTLSCertCaPath { get => _mqttTLSCertCaPath; set { if (SetProperty(ref _mqttTLSCertCaPath, value)) Save(); } }
        public string mqttTLSCertClientPath { get => _mqttTLSCertClientPath; set { if (SetProperty(ref _mqttTLSCertClientPath, value)) Save(); } }
        public string mqttTLSCertClientPassword { get => _mqttTLSCertClientPassword; set { if (SetProperty(ref _mqttTLSCertClientPassword, value)) Save(); } }
        public bool mqttWs { get => _mqttWs; set { if (SetProperty(ref _mqttWs, value)) Save(); } }
        public string mqttWsPath { get => _mqttWsPath; set { if (SetProperty(ref _mqttWsPath, value)) Save(); } }
        public string mqttUser { get => _mqttUser; set { if (SetProperty(ref _mqttUser, value)) Save(); } }
        public string mqttPassword { get => _mqttPassword; set { if (SetProperty(ref _mqttPassword, value)) Save(); } }
        public int mqttKeepAlive { get => _mqttKeepAlive; set { if (SetProperty(ref _mqttKeepAlive, value)) Save(); } }
        public bool mqttCleanSession { get => _mqttCleanSession; set { if (SetProperty(ref _mqttCleanSession, value)) Save(); } }
        public string mqttPublishTopic { get => _mqttPublishTopic; set { if (SetProperty(ref _mqttPublishTopic, value)) Save(); } }
        public string mqttSubscribeTopic { get => _mqttSubscribeTopic; set { if (SetProperty(ref _mqttSubscribeTopic, value)) Save(); } }
    }
}
