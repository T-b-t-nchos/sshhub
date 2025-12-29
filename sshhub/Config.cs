namespace sshhub
{
    public class ConfigRoot
    {
        public string Exec { get; set; } = "ssh {$Username}@{$IP} -p {$Port}";
        public TargetConfig[] Targets { get; set; } = [];
    }


    public class TargetConfig
    {
        public int id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 22;
        public string Username { get; set; } = string.Empty;
    }
}
