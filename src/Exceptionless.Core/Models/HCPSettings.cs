using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Exceptionless.Core.Models
{
    public class HcpSettings
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string OrgId { get; set; }
        public required string ProjectId { get; set; }
        public required string AppName { get; set; }
        public required string SecretName { get; set; }
    }

    public class HcpTokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }
    }
}
