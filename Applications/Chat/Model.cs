using Newtonsoft.Json;

namespace WebSocketServer.Applications.Chat
{
    class Model
    {
        [JsonProperty("username")]
        public string Username { get; private set; }
        [JsonProperty("message")]
        public string Message { get; private set; }
        [JsonProperty("date")]
        public long Date { get; private set; }
    }
}