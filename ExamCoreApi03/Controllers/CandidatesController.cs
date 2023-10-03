using ExamCoreApi03.Models;
using ExamCoreApi03.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ExamCoreApi03.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    
    public class CandidatesController : ControllerBase
    {

        private readonly CandidateDbContext _context;
        private readonly IWebHostEnvironment _env;
        public IConfiguration _configuration;
        public CandidatesController(CandidateDbContext _context, IWebHostEnvironment _env, IConfiguration configuration)
        {
            this._context = _context;
            this._env = _env;
            _configuration = configuration;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetSkills")]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
        {
            return await _context.Skills.ToListAsync();
        }
       
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CandidateVM>>> GetCandidateSkills()
        {
            List<CandidateVM> candidateSkills = new List<CandidateVM>();
            var allCandidates = _context.Candidates.ToList();
            foreach (var candidate in allCandidates)
            {
                var skillList = _context.CandidateSkills.Where(x => x.CandidateId == candidate.CandidateId).Select(x => new Skill { SkillId = x.SkillId, SkillName = x.Skill.SkillName }).ToList();

                candidateSkills.Add(new CandidateVM
                {
                    CandidateId = candidate.CandidateId,
                    CandidateName = candidate.CandidateName,
                    BirthDate = candidate.BirthDate,
                    Email = candidate.Email,
                    Password = candidate.Password,
                    Fresher = candidate.Fresher,
                    Picture = candidate.Picture,
                    SkillList = skillList.ToList()
                });
            }
            return candidateSkills;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("PostCandidate")]
        public async Task<ActionResult<CandidateSkill>> PostCandidateSkills([FromForm] CandidateVM VM)
        {
            var skillItems = JsonConvert.DeserializeObject<Skill[]>(VM.SkillStringify);

            Candidate candidate = new Candidate
            {
                CandidateName = VM.CandidateName,
                BirthDate = VM.BirthDate,
                Email = VM.Email,
                Password = VM.Password,
                Fresher = VM.Fresher
            };

            if (VM.PictureFile != null)
            {
                var webroot = _env.WebRootPath;
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(VM.PictureFile.FileName);
                var filePath = Path.Combine(webroot, "Images", fileName);

                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                await VM.PictureFile.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close();
                candidate.Picture = fileName;
            }

            foreach (var item in skillItems)
            {
                var candidateskill = new CandidateSkill
                {
                    Candidate = candidate,
                    CandidateId = candidate.CandidateId,
                    SkillId = item.SkillId,
                    //if
                    SkillName = item.SkillName


                };
                _context.Add(candidateskill);
            }

            await _context.SaveChangesAsync();
            return Ok(candidate);
        }
        [Route("Update/{id}")]
        [HttpPut]
        public async Task<ActionResult<CandidateSkill>> UpdateBookingEntry(int id, [FromForm] CandidateVM vm)
        {
            var skillItems = JsonConvert.DeserializeObject<Skill[]>(vm.SkillStringify);

            Candidate candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            candidate.CandidateName = vm.CandidateName;
            candidate.BirthDate = vm.BirthDate;
            candidate.Email = vm.Email;
            candidate.Password = vm.Password;
            candidate.Fresher = vm.Fresher;

            if (vm.PictureFile != null)
            {
                var webroot = _env.WebRootPath;
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.PictureFile.FileName);
                var filePath = Path.Combine(webroot, "Images", fileName);

                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                await vm.PictureFile.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close();
                candidate.Picture = fileName;
            }


            var existingSkills = _context.CandidateSkills.Where(x => x.CandidateId == candidate.CandidateId).ToList();
            foreach (var item in existingSkills)
            {
                _context.CandidateSkills.Remove(item);
            }


            foreach (var item in skillItems)
            {
                var candidateSkill = new CandidateSkill
                {
                    CandidateId = candidate.CandidateId,
                    SkillId = item.SkillId,
                    //if
                    SkillName = item.SkillName
                };
                _context.Add(candidateSkill);
            }

            _context.Entry(candidate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(candidate);
        }
        [Route("Delete/{id}")]
        [HttpDelete]
        public async Task<ActionResult<CandidateSkill>> DeleteCandidateSkill(int id)
        {
            Candidate candidate = _context.Candidates.Find(id);

            var existingSkills = _context.CandidateSkills.Where(x => x.CandidateId == candidate.CandidateId).ToList();
            foreach (var item in existingSkills)
            {
                _context.CandidateSkills.Remove(item);
            }
            _context.Entry(candidate).State = EntityState.Deleted;

            await _context.SaveChangesAsync();


            return Ok(candidate);
        }





        [AllowAnonymous]
        [Route("PostLoginDetails")]
        [HttpPost]
        public async Task<IActionResult> PostLoginDetails(UserModel _userData)
        {
            if (_userData != null)
            {
                var resultLoginCheck = _context.Candidates
                    .Where(e => e.Email == _userData.Email && e.Password == _userData.Password)
                    .FirstOrDefault();
                if (resultLoginCheck == null)
                {
                    return BadRequest("Invalid Credentials");
                }
                else
                {


                    var claims = new[] {
                        new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        new Claim("UserId", _userData.ID.ToString()),
                        new Claim("DisplayName", _userData.FullName),
                        new Claim("Email", _userData.Email)
                    };


                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        _configuration["Jwt:Issuer"],
                        _configuration["Jwt:Audience"],
                        claims,
                        expires: DateTime.UtcNow.AddMinutes(10),
                        signingCredentials: signIn);


                    _userData.AccessToken = new JwtSecurityTokenHandler().WriteToken(token);

                    return Ok(_userData);
                }
            }
            else
            {
                return BadRequest("No Data Posted");
            }
        }





    }


}
