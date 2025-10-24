using Microsoft.AspNetCore.Mvc;

namespace Exceptionless.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SecretsController : ControllerBase
    {
        private readonly HcpSecretsService _secretsService;

        public SecretsController(HcpSecretsService secretsService)
        {
            _secretsService = secretsService;
            _ = GetSecret();
        }

        [HttpGet]
        public async Task<IActionResult> GetSecret()
        {
            var secret = await _secretsService.GetSecretAsync();
            return Ok(secret);
        }
    }
}
