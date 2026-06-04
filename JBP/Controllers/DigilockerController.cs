using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace JobPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DigilockerController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public DigilockerController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("generate-link")]
        public async Task<IActionResult> GenerateLink()
        {
            var requestBody = new
            {
                data = new
                {
                    expiry_minutes = 10,
                    send_sms = false,
                    send_email = false,
                    verify_phone = false,
                    verify_email = false,

                    redirect_url =
                    "http://127.0.0.1:5500/index.html",

                    skip_main_screen = false,
                    signup_flow = true
                }
            };

            var json =
                JsonSerializer.Serialize(requestBody);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmcmVzaCI6ZmFsc2UsImlhdCI6MTc3OTk1NDMxNCwianRpIjoiZWNmNjVkNmUtNDJmZi00OWRiLWE3M2MtYjJhNDZkOTkxYzJjIiwidHlwZSI6ImFjY2VzcyIsImlkZW50aXR5IjoiZGV2LmthcnRoaWtsdWNreTY1X3Zpc3Rhd2F5c3RlY2hfbGxwQHN1cmVwYXNzLmlvIiwibmJmIjoxNzc5OTU0MzE0LCJleHAiOjE3ODI1NDYzMTQsImVtYWlsIjoia2FydGhpa2x1Y2t5NjVfdmlzdGF3YXlzdGVjaF9sbHBAc3VyZXBhc3MuaW8iLCJ0ZW5hbnRfaWQiOiJtYWluIiwidXNlcl9jbGFpbXMiOnsic2NvcGVzIjpbInVzZXIiXX19.j6MMib450mhoC1J5k1J8BJKp6PcXm6szBdaTEWVej4s");

            var response =
                await _httpClient.PostAsync(
    "https://sandbox.surepass.app/api/v1/digilocker/initialize",
    content);

            var result =
                await response.Content.ReadAsStringAsync();

            return Content(
                result,
                "application/json");
        }
    }
}