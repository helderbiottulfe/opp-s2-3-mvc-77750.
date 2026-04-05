using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public FacultyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<FacultyProfile?> GetMyProfileAsync()
        {
            var userId = _userManager.GetUserId(User);
            return await _db.FacultyProfiles
                .Include(f => f.CourseAssignments).ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(f => f.IdentityUserId == userId);
        }

        private async Task<List<int>> GetMyCourseIdsAsync()
        {
            var profile = await GetMyProfileAsync();
            return profile?.CourseAssignments.Select(a => a.CourseId).ToList() ?? new List<int>();
        }

        // ── Dashboard ──────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return View("NoProfile");
            var courseIds = profile.CourseAssignments.Select(a => a.CourseId).ToList();
            ViewBag.Courses = await _db.Courses.Where(c => courseIds.Contains(c.Id)).Include(c => c.Branch).ToListAsync();
            ViewBag.StudentCount = await _db.CourseEnrolments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentProfileId).Distinct().CountAsync();
            ViewBag.Profile = profile;
            return View();
        }

        // ── My students ────────────────────────────────────────────────────
        public async Task<IActionResult> MyStudents(int? courseId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var query = _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course)
                .Where(e => courseIds.Contains(e.CourseId));
            if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId.Value);
            ViewBag.Courses = new SelectList(
                await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name", courseId);
            ViewBag.SelectedCourse = courseId;
            return View(await query.ToListAsync());
        }

        // ── Student contact details (tutor only) ───────────────────────────
        public async Task<IActionResult> StudentContact(int id)
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return Forbid();

            var isTutor = profile.CourseAssignments.Any(a => a.IsTutor);
            if (!isTutor) return Forbid();

            var courseIds = profile.CourseAssignments.Select(a => a.CourseId).ToList();
            var student = await _db.StudentProfiles
                .FirstOrDefaultAsync(s => s.Id == id &&
                    _db.CourseEnrolments.Any(e => e.StudentProfileId == s.Id && courseIds.Contains(e.CourseId)));
            if (student == null) return NotFound();
            return View(student);
        }

        // ── Gradebook ─────────────────────────────────────────────────────
        public async Task<IActionResult> Gradebook(int? courseId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var query = _db.Assignments
                .Include(a => a.Course)
                .Include(a => a.Results).ThenInclude(r => r.StudentProfile)
                .Where(a => courseIds.Contains(a.CourseId));
            if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId.Value);
            ViewBag.Courses = new SelectList(
                await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name", courseId);
            return View(await query.ToListAsync());
        }

        // ── Add assignment result ──────────────────────────────────────────
        public async Task<IActionResult> AddResult(int assignmentId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var assignment = await _db.Assignments.Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && courseIds.Contains(a.CourseId));
            if (assignment == null) return Forbid();

            var enrolled = await _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Where(e => e.CourseId == assignment.CourseId)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            ViewBag.Students = new SelectList(enrolled.Select(e => e.StudentProfile), "Id", "Name");
            return View(new AssignmentResult { AssignmentId = assignmentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResult(AssignmentResult model)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var assignment = await _db.Assignments
                .FirstOrDefaultAsync(a => a.Id == model.AssignmentId && courseIds.Contains(a.CourseId));
            if (assignment == null) return Forbid();

            if (model.Score > assignment.MaxScore)
                ModelState.AddModelError("Score", $"Score cannot exceed max score of {assignment.MaxScore}.");

            if (!ModelState.IsValid)
            {
                ViewBag.Assignment = assignment;
                var enrolled = await _db.CourseEnrolments.Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == assignment.CourseId).ToListAsync();
                ViewBag.Students = new SelectList(enrolled.Select(e => e.StudentProfile), "Id", "Name");
                return View(model);
            }

            var existing = await _db.AssignmentResults
                .FirstOrDefaultAsync(r => r.AssignmentId == model.AssignmentId && r.StudentProfileId == model.StudentProfileId);
            if (existing != null) { existing.Score = model.Score; existing.Feedback = model.Feedback; }
            else _db.AssignmentResults.Add(model);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Result saved.";
            return RedirectToAction(nameof(Gradebook));
        }

        // ── Create assignment ─────────────────────────────────────────────
        public async Task<IActionResult> CreateAssignment()
        {
            var courseIds = await GetMyCourseIdsAsync();
            ViewBag.Courses = new SelectList(
                await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name");
            return View(new Assignment { DueDate = DateTime.Today.AddDays(30) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(Assignment model)
        {
            var courseIds = await GetMyCourseIdsAsync();
            if (!courseIds.Contains(model.CourseId))
                ModelState.AddModelError("CourseId", "You are not assigned to this course.");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(
                    await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Assignments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment created.";
            return RedirectToAction(nameof(Gradebook));
        }

        public async Task<IActionResult> EditAssignment(int id)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var assignment = await _db.Assignments
                .FirstOrDefaultAsync(a => a.Id == id && courseIds.Contains(a.CourseId));
            if (assignment == null) return Forbid();
            ViewBag.Courses = new SelectList(
                await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name", assignment.CourseId);
            return View(assignment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(Assignment model)
        {
            var courseIds = await GetMyCourseIdsAsync();
            if (!courseIds.Contains(model.CourseId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(
                    await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Assignments.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment updated.";
            return RedirectToAction(nameof(Gradebook));
        }

        // ── Exams list ────────────────────────────────────────────────────
        public async Task<IActionResult> Exams(int? courseId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var query = _db.Exams
                .Include(e => e.Course)
                .Include(e => e.Results).ThenInclude(r => r.StudentProfile)
                .Where(e => courseIds.Contains(e.CourseId));
            if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId.Value);
            ViewBag.Courses = new SelectList(
                await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync(), "Id", "Name", courseId);
            return View(await query.ToListAsync());
        }

        // ── Add exam result ───────────────────────────────────────────────
        public async Task<IActionResult> AddExamResult(int examId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var exam = await _db.Exams.Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId && courseIds.Contains(e.CourseId));
            if (exam == null) return Forbid();

            var enrolled = await _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Where(e => e.CourseId == exam.CourseId)
                .ToListAsync();

            ViewBag.Exam = exam;
            ViewBag.Students = new SelectList(enrolled.Select(e => e.StudentProfile), "Id", "Name");
            return View(new ExamResult { ExamId = examId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExamResult(ExamResult model)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var exam = await _db.Exams
                .FirstOrDefaultAsync(e => e.Id == model.ExamId && courseIds.Contains(e.CourseId));
            if (exam == null) return Forbid();

            if (model.Score > exam.MaxScore)
                ModelState.AddModelError("Score", $"Score cannot exceed max score of {exam.MaxScore}.");
            if (model.Score < 0)
                ModelState.AddModelError("Score", "Score cannot be negative.");

            if (!ModelState.IsValid)
            {
                ViewBag.Exam = exam;
                var enrolled = await _db.CourseEnrolments.Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == exam.CourseId).ToListAsync();
                ViewBag.Students = new SelectList(enrolled.Select(e => e.StudentProfile), "Id", "Name");
                return View(model);
            }

            var existing = await _db.ExamResults
                .FirstOrDefaultAsync(r => r.ExamId == model.ExamId && r.StudentProfileId == model.StudentProfileId);
            if (existing != null)
            {
                existing.Score = model.Score;
                existing.Grade = model.Grade;
            }
            else _db.ExamResults.Add(model);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Exam result saved.";
            return RedirectToAction(nameof(Exams));
        }

        // ── Edit exam result ──────────────────────────────────────────────
        public async Task<IActionResult> EditExamResult(int id)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var result = await _db.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e.Course)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id && courseIds.Contains(r.Exam.CourseId));
            if (result == null) return NotFound();
            ViewBag.Exam = result.Exam;
            return View(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExamResult(ExamResult model)
        {
            var courseIds = await GetMyCourseIdsAsync();
            var existing = await _db.ExamResults
                .Include(r => r.Exam)
                .FirstOrDefaultAsync(r => r.Id == model.Id && courseIds.Contains(r.Exam.CourseId));
            if (existing == null) return Forbid();

            if (model.Score > existing.Exam.MaxScore)
                ModelState.AddModelError("Score", $"Score cannot exceed {existing.Exam.MaxScore}.");
            if (model.Score < 0)
                ModelState.AddModelError("Score", "Score cannot be negative.");

            if (!ModelState.IsValid)
            {
                ViewBag.Exam = existing.Exam;
                model.StudentProfile = existing.StudentProfile;
                return View(model);
            }

            existing.Score = model.Score;
            existing.Grade = model.Grade;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Exam result updated.";
            return RedirectToAction(nameof(Exams));
        }

        // ── Attendance view ────────────────────────────────────────────────
        public async Task<IActionResult> Attendance(int courseId)
        {
            var courseIds = await GetMyCourseIdsAsync();
            if (!courseIds.Contains(courseId)) return Forbid();

            var enrolments = await _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.AttendanceRecords)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
            ViewBag.Course = await _db.Courses.FindAsync(courseId);
            return View(enrolments);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordAttendance(int enrolmentId, int weekNumber, bool present)
        {
            var enrolment = await _db.CourseEnrolments.FindAsync(enrolmentId);
            if (enrolment == null) return NotFound();

            var courseIds = await GetMyCourseIdsAsync();
            if (!courseIds.Contains(enrolment.CourseId)) return Forbid();

            var existing = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.CourseEnrolmentId == enrolmentId && a.WeekNumber == weekNumber);
            if (existing == null)
                _db.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enrolmentId, WeekNumber = weekNumber, Date = DateTime.Today, Present = present });
            else existing.Present = present;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Attendance), new { courseId = enrolment.CourseId });
        }
    }
}
