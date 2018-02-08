using System.Runtime.Serialization;

namespace Newsblast.Server.Models
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public string SqlServerConnectionString { get; set; }
        [DataMember]
        public string DiscordBotToken { get; set; }
    }
}
