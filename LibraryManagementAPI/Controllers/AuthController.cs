using Microsoft.AspNetCore.Mvc;
using LibraryManagementAPI.Services;
using LibraryManagementAPI.Models.User;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Net.Mail;
using System.Net;
using MongoDB.Driver;


namespace LibraryManagementAPI.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBService<User> _userService;
        private readonly IConfiguration _configuration;

        public AuthController(MongoDBService<User> userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegister userRegister)
        {
            if (await _userService.GetAsync(x => x.Email == userRegister.Email) != null)
            {
                return BadRequest(new { message = $"Email {userRegister.Email} is taken" });
            }


            User newUser = new User
            {
                Email = userRegister.Email,
                Name = userRegister.Name,
                Password = ComputeSha256Hash(userRegister.Password),
                IsEmailConfirmed= true,
            };
            newUser.Roles.Add("user");
            await _userService.CreateAsync(newUser);

            //SendVerificationEmail(newUser, IssueToken(newUser));

            return Ok(new { message = "User account created" });
        }

        [HttpGet("verify/{token}")]
        public async Task<IActionResult> Verify(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            var tokenData = handler.ReadJwtToken(token);

            string id = tokenData.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

            var user = await _userService.GetAsync(x => x.Id == id);

            if (user == null)
                return BadRequest(new { message = "Account not found" });

            UpdateDefinition<User> updateDefinition = Builders<User>.Update.Set(x => x.IsEmailConfirmed, true);


            await _userService.UpdateAsync(x => x.Id == id, updateDefinition);

            return Ok(new { message = "Email verified" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLogin userLogin)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.GetAsync((x) => x.Email == userLogin.Email && x.Password == ComputeSha256Hash(userLogin.Password));

                if (user == null)
                {
                    return Unauthorized("Invalid Login credentials");
                }

                var token = IssueToken(user);

                return Ok(new { Token = token });
            }
            return BadRequest("Invalid Request Body");
        }


        private string ComputeSha256Hash(string plainText)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));

                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private string IssueToken(User user)
        {
            // Creates a new symmetric security key from the JWT key specified in the app configuration.
            var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]));
            // Sets up the signing credentials using the above security key and specifying the HMAC SHA256 algorithm.
            var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

            // Defines a set of claims to be included in the token.
            var claims = new List<Claim>
            {
                // Custom claim using the user's ID.
                new Claim("LM_User_Id", user.Id),
                // Standard claim for user identifier, using id.
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                // Standard claim for user's email.
                new Claim(ClaimTypes.Email, user.Email),
                // Standard JWT claim for subject, using user ID.
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
            };

            // Adds a role claim for each role associated with the user.
            user.Roles.ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));


            // Creates a new JWT token with specified parameters including issuer, audience, claims, expiration time, and signing credentials.
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            // Serializes the JWT token to a string and returns it.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private void SendEmail(User user, string body)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_configuration["Mail:Email"]);
            mailMessage.To.Add(user.Email);
            mailMessage.Subject = "Library Management Account Verification";
            mailMessage.Body = body;

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = _configuration["Mail:Host"];
            smtpClient.Port = int.Parse(_configuration["Mail:Port"]);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_configuration["Mail:Email"], _configuration["Mail:Password"]);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;

            smtpClient.Send(mailMessage);
        }

        private void SendVerificationEmail(User user, string token)
        {
            string url = $"https://localhost:7256/api/v1/Auth/verify/{token}";
            string body = $"""
                <html>
                  <body>
                    <p>Hello,</p>
                    <p>Click on this <a href="{url}">Verify Email</a> to verify your email.</p>
                    <p>Best regards,<br>
                    The Library Management Team</p>
                  </body>
                </html>
                """;

            SendEmail(user, body);
        }
    }
}
