using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace rumahSolusi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly DataContext _context;
        public AuthController(DataContext context)
        {
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

            return Ok(user.VerificationToken);
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

            return Ok("User Sukses dibuat");
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

            return Ok("User diverifikasi :)");
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


