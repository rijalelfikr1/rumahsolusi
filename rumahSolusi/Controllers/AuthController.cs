

namespace rumahSolusi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        public static UserModel user = new UserModel();
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration, DataContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        private async Task<UserModel> GetUserById(int id)
        {
            return await _context.MstUsers.FirstOrDefaultAsync(c => c.Id == id);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var user = await _context.MstUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User Tidak ditemukan");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Password salah.");
            }

            if (user.VerifiedAt == null)
            {
                return BadRequest("Belum di verifikasi");
            }
            if (user.Status == 1)
            {
                return BadRequest("User di ban");
            }
            if (user.Status == 2)
            {
                return BadRequest("User di Non Aktifkan");
            }
            string token = CreateToken(user);

            var refreshToken = new RefreshToken
            {
                Email = request.Email,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                TokenExpires = DateTime.Now.AddDays(7),
                TokenCreated = DateTime.Now
            };

            SetRefreshToken(refreshToken);

            return Ok(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            if (_context.MstUsers.Any(u => u.Email == request.Email))
            {
                return BadRequest("User Sudah Ada");
            }

            CreatePasswordHash(request.Password,
                    out byte[] passwordHash,
                    out byte[] passwordSalt);

            var user = new UserModel
            {
                Email = request.Email,
                UserName = request.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                VerificationToken = CreateRandomToken()
            };

            _context.MstUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user.VerificationToken);
        }


        [HttpPost("verify")]
        public async Task<IActionResult> Verify(string token)
        {
            var user = await _context.MstUsers.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null)
            {
                return BadRequest("Token Salah");
            }

            user.VerifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok("User berhasil diverifikasi :)");
        }

        [HttpPut("Update-User")]
        public async Task<IActionResult> UpdateUser(UserUpdateModel request)
        {
            var user = _context.MstUsers.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User Tidak Ditemukan");
            }

            CreatePasswordHash(request.Password,
                            out byte[] passwordHash,
                            out byte[] passwordSalt);


            user.Email = request.Email;
            user.UserName = request.UserName;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.SaveChangesAsync();
            return Ok("User Berhasil diubah");
            
        }

        [HttpPut("Ban-User")]
        public async Task<IActionResult> BanUser(int Id)
        {

            var dbMk = await GetUserById(Id);
            if (dbMk == null)
            {
                return BadRequest("User Tidak Ditemukan");
            }

            dbMk.Status = 1;
            await _context.SaveChangesAsync();
            return Ok("User Berhasil di ban");

        }

        [HttpPut("Non-Active-User")]
        public async Task<IActionResult> NonUser(int Id)
        {

            var dbMk = await GetUserById(Id);
            if (dbMk == null)
            {
                return BadRequest("User Tidak Ditemukan");
            }

            dbMk.Status = 2;
            await _context.SaveChangesAsync();
            return Ok("User Berhasil di Non Aktifkan");

        }

        [HttpDelete("Delete-User")]
        public async Task<IActionResult> DeleteUser(int Id)
        {
            var user = await GetUserById(Id);
            if (user == null)
            {
                return BadRequest("Tidak ada user");
            }
            else
            {
                _context.MstUsers.Remove(user);
                await _context.SaveChangesAsync();
                return Ok("Berhasil Delete User");
            }
        }

        [HttpPut]
        private void SetRefreshToken(RefreshToken newRefreshToken)
        {
            var user = _context.MstUsers.FirstOrDefault(u => u.Email == newRefreshToken.Email);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.TokenExpires
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            user.RefreshToken = newRefreshToken.Token;
            user.TokenCreated = newRefreshToken.TokenCreated;
            user.TokenExpires = newRefreshToken.TokenExpires;
            _context.SaveChanges();
        }

        /*private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                TokenExpires = DateTime.Now.AddDays(7),
                TokenCreated = DateTime.Now
            };

            return refreshToken;
        }*/

        private string CreateToken(UserModel user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        private string CreateRandomToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        }
     }
}


