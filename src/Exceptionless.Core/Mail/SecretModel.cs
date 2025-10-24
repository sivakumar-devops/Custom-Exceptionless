using System.Text.Json;
using System.Text.Json.Serialization;

namespace Exceptionless.Core.Mail
{
    public class SecretModel
    {
        [JsonPropertyName("secret")]
        public SecretResponse? Secret { get; set; }


        public class SecretResponse
        {
            [JsonPropertyName("dynamic_instance")]
            public DynamicInstanceData? DynamicInstance { get; set; }

            public class DynamicInstanceData
            {
                [JsonPropertyName("values")]
                public AwsCredentialValues? Values { get; set; }
            }

            public class AwsCredentialValues
            {
                [JsonPropertyName("access_key_id")]
                public string? AccessKeyId { get; set; }

                [JsonPropertyName("secret_access_key")]
                public string? SecretAccessKey { get; set; }

                [JsonPropertyName("session_token")]
                public string? SessionToken { get; set; }
            }
        }
    }
}
