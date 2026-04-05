using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<StudentProfile?> GetMyProfileAsync()
        {
            var userId = _userManager.GetUserId(User);
            return await _db.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        }

        // ── Dashboard ──────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return View("NoProfile");

            var enrolments = await _db.CourseEnrolments
                .Include(e => e.Course).ThenInclude(c => c.Branch)
                .Where(e => e.StudentProfileId == profile.Id)
                .ToListAsync();

            ViewBag.Profile = profile;
            ViewBag.Enrolments = enrolments;
            return View();
        }

        // ── My profile ─────────────────────────────────────────────────────
        public async Task<IActionResult> Profile()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return NotFound();
            return View(profile);
        }

        // ── My courses / enrolments ────────────────────────────────────────
        public async Task<IActionResult> MyCourses()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return NotFound();

            var enrolments = await _db.CourseEnrolments
                .Include(e => e.Course).ThenInclude(c => c.Branch)
                .Include(e => e.AttendanceRecords)
                .Where(e => e.StudentProfileId == profile.Id)
                .ToListAsync();
            return View(enrolments);
        }

        // ── My grades (assignments) ────────────────────────────────────────
        public async Task<IActionResult> Grades()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return NotFound();

            var results = await _db.AssignmentResults
                .Include(r => r.Assignment).ThenInclude(a => a.Course)
                .Where(r => r.StudentProfileId == profile.Id)
                .ToListAsync();
            return View(results);
        }

        // ── My exam results (released only) ────────────────────────────────
        public async Task<IActionResult> ExamResults()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return NotFound();

            // IMPORTANT: Only show results where ResultsReleased == true
            var results = await _db.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e.Course)
                .Where(r => r.StudentProfileId == profile.Id && r.Exam.ResultsReleased)
                .ToListAsync();
            return View(results);
        }

        // ── My attendance ─────────────────────────────────────────────────
        public async Task<IActionResult> Attendance()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return NotFound();

            var enrolments = await _db.CourseEnrolments
                .Include(e => e.Course)
                .Include(e => e.AttendanceRecords)
                .Where(e => e.StudentProfileId == profile.Id)
                .ToListAsync();
            return View(enrolments);
        }
    }
}
