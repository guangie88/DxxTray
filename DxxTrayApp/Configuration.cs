using System;

namespace DxxTray
{
    public class Configuration
    {
        public string StoreDirPath { get; set; }
        public TimeSpan? ExpiryTime { get; set; }
    }
}
