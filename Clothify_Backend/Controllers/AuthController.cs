using BCrypt.Net;
using Clothify_Backend.DTO.Auth;
using Clothify_Backend.Models;
using Clothify_Backend.Services;
using Microsoft.AspNetCore.Mvc;
namespace Clothify_Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        //  Register
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            var userExists = _context.Users.Any(x => x.Email == dto.Email);
            if (userExists)
                return BadRequest("User already exists");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role // default role
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        // Login
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == dto.Email);
            if (user == null)
                return Unauthorized("Invalid email");

            var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isValid)
                return Unauthorized("Invalid password");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                Token = token,
                Role = user.Role,
                UserId = user.Id
            });
        }
    }
}