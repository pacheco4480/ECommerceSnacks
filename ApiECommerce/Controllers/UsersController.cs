using Microsoft.AspNetCore.Mvc;
using ApiECommerce.Entities;
using ApiECommerce.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _config;

        public UsersController(AppDbContext appDbContext, IConfiguration config)
        {
            _appDbContext = appDbContext;
            _config = config;
        }


        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var checkUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkUser != null)
            {
                return BadRequest("Já existe um utilizador com este email.");
            }

            _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var currentUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email && u.Password == user.Password);

            if (currentUser == null)
            { 
                return NotFound("O utilizador não existe"); 
            }

            var key = _config["JWT:Key"] ?? throw new ArgumentNullException("JWT:Key", "JWT:Key cannot be null.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email!)
            };

            var token = new JwtSecurityToken(
                issuer: _config["JWT:Issuer"],
                audience: _config["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(10),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new ObjectResult(new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = currentUser.Id,
                user_name = currentUser.Name
            });
        }
    }
}
