//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace JBP.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class VerificationController : ControllerBase
//    {
//        private readonly IConfiguration _config;

//        public VerificationController(IConfiguration config)
//        {
//            _config = config;
//        }

//        [HttpGet("digilocker-login")]
//        public IActionResult DigiLockerLogin()
//        {
//            var clientId =
//                _config["DigiLocker:ClientId"];

//            var redirectUri =
//                _config["DigiLocker:RedirectUri"];

//            var url =
//                $"https://api.digitallocker.gov.in/public/oauth2/1/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}";

//            return Redirect(url);
//        }
//    }
//}