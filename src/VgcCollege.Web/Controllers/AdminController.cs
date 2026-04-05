using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── Dashboard ──────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.BranchCount = await _db.Branches.CountAsync();
            ViewBag.CourseCount = await _db.Courses.CountAsync();
            ViewBag.StudentCount = await _db.StudentProfiles.CountAsync();
            ViewBag.FacultyCount = await _db.FacultyProfiles.CountAsync();
            ViewBag.EnrolmentCount = await _db.CourseEnrolments.CountAsync();
            return View();
        }

        // ── Branches ──────────────────────────────────────────────────────
        public async Task<IActionResult> Branches() =>
            View(await _db.Branches.ToListAsync());

        public IActionResult CreateBranch() => View(new Branch());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch(Branch model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Branches.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Branch created.";
            return RedirectToAction(nameof(Branches));
        }

        public async Task<IActionResult> EditBranch(int id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranch(Branch model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Branches.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Branch updated.";
            return RedirectToAction(nameof(Branches));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var b = await _db.Branches.FindAsync(id);
            if (b != null) { _db.Branches.Remove(b); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Branch deleted.";
            return RedirectToAction(nameof(Branches));
        }

        // ── Courses ───────────────────────────────────────────────────────
        public async Task<IActionResult> Courses() =>
            View(await _db.Courses.Include(c => c.Branch).ToListAsync());

        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Branches = new SelectList(await _db.Branches.ToListAsync(), "Id", "Name");
            return View(new Course());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = new SelectList(await _db.Branches.ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Courses.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Course created.";
            return RedirectToAction(nameof(Courses));
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            ViewBag.Branches = new SelectList(await _db.Branches.ToListAsync(), "Id", "Name", course.BranchId);
            return View(course);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = new SelectList(await _db.Branches.ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Courses.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Course updated.";
            return RedirectToAction(nameof(Courses));
        }

        // ── Students ──────────────────────────────────────────────────────
        public async Task<IActionResult> Students() =>
            View(await _db.StudentProfiles.ToListAsync());

        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await _db.StudentProfiles
                .Include(s => s.Enrolments).ThenInclude(e => e.Course)
                .Include(s => s.AssignmentResults).ThenInclude(ar => ar.Assignment).ThenInclude(a => a.Course)
                .Include(s => s.ExamResults).ThenInclude(er => er.Exam).ThenInclude(ex => ex.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound();
            return View(student);
        }

        public IActionResult CreateStudent() => View(new StudentProfile());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(StudentProfile model, string password)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.Name, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password ?? "Student@123!");
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }
            await _userManager.AddToRoleAsync(user, "Student");
            model.IdentityUserId = user.Id;
            _db.StudentProfiles.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Student created.";
            return RedirectToAction(nameof(Students));
        }

        public async Task<IActionResult> EditStudent(int id)
        {
            var s = await _db.StudentProfiles.FindAsync(id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(StudentProfile model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.StudentProfiles.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Student updated.";
            return RedirectToAction(nameof(Students));
        }

        // ── Enrolments ────────────────────────────────────────────────────
        public async Task<IActionResult> Enrolments() =>
            View(await _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course).ThenInclude(c => c.Branch)
                .ToListAsync());

        public async Task<IActionResult> CreateEnrolment()
        {
            ViewBag.Students = new SelectList(await _db.StudentProfiles.ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
            return View(new CourseEnrolment { EnrolDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEnrolment(CourseEnrolment model)
        {
            bool dup = await _db.CourseEnrolments.AnyAsync(e =>
                e.StudentProfileId == model.StudentProfileId && e.CourseId == model.CourseId);
            if (dup) ModelState.AddModelError("", "This student is already enrolled in that course.");

            if (!ModelState.IsValid)
            {
                ViewBag.Students = new SelectList(await _db.StudentProfiles.ToListAsync(), "Id", "Name");
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.CourseEnrolments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Student enrolled.";
            return RedirectToAction(nameof(Enrolments));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var e = await _db.CourseEnrolments.FindAsync(id);
            if (e != null) { _db.CourseEnrolments.Remove(e); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Enrolment removed.";
            return RedirectToAction(nameof(Enrolments));
        }

        // ── Faculty ───────────────────────────────────────────────────────
        public async Task<IActionResult> Faculty() =>
            View(await _db.FacultyProfiles
                .Include(f => f.CourseAssignments).ThenInclude(a => a.Course)
                .ToListAsync());

        public async Task<IActionResult> AssignFaculty()
        {
            ViewBag.Faculty = new SelectList(await _db.FacultyProfiles.ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
            return View(new FacultyCourseAssignment());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFaculty(FacultyCourseAssignment model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Faculty = new SelectList(await _db.FacultyProfiles.ToListAsync(), "Id", "Name");
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.FacultyCourseAssignments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Faculty assigned.";
            return RedirectToAction(nameof(Faculty));
        }

        // ── Exam result release + management ─────────────────────────────
        public async Task<IActionResult> ManageExams() =>
            View(await _db.Exams
                .Include(e => e.Course)
                .Include(e => e.Results).ThenInclude(r => r.StudentProfile)
                .ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleExamRelease(int id)
        {
            var exam = await _db.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            exam.ResultsReleased = !exam.ResultsReleased;
            await _db.SaveChangesAsync();
            TempData["Success"] = exam.ResultsReleased ? "Results released." : "Results hidden.";
            return RedirectToAction(nameof(ManageExams));
        }

        // ── Add exam result (Admin) ───────────────────────────────────────
        public async Task<IActionResult> AddExamResult(int examId)
        {
            var exam = await _db.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return NotFound();

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
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == model.ExamId);
            if (exam == null) return NotFound();

            if (model.Score > exam.MaxScore)
                ModelState.AddModelError("Score", $"Score cannot exceed {exam.MaxScore}.");
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
            if (existing != null) { existing.Score = model.Score; existing.Grade = model.Grade; }
            else _db.ExamResults.Add(model);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Exam result saved.";
            return RedirectToAction(nameof(ManageExams));
        }

        // ── Edit exam result (Admin) ──────────────────────────────────────
        public async Task<IActionResult> EditExamResult(int id)
        {
            var result = await _db.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e.Course)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            ViewBag.Exam = result.Exam;
            return View(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExamResult(ExamResult model)
        {
            var existing = await _db.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == model.Id);
            if (existing == null) return NotFound();

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
            return RedirectToAction(nameof(ManageExams));
        }

        // ── Assignments (Admin) ───────────────────────────────────────────
        public async Task<IActionResult> Assignments(int? courseId)
        {
            var query = _db.Assignments.Include(a => a.Course).ThenInclude(c => c.Branch).AsQueryable();
            if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId);
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name", courseId);
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> CreateAssignment()
        {
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
            return View(new Assignment { DueDate = DateTime.Today.AddDays(30) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(Assignment model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Assignments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment created.";
            return RedirectToAction(nameof(Assignments));
        }

        public async Task<IActionResult> EditAssignment(int id)
        {
            var a = await _db.Assignments.FindAsync(id);
            if (a == null) return NotFound();
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name", a.CourseId);
            return View(a);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(Assignment model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Assignments.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment updated.";
            return RedirectToAction(nameof(Assignments));
        }

        // ── Exams (Admin create/edit) ─────────────────────────────────────
        public async Task<IActionResult> CreateExam()
        {
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
            return View(new Exam { Date = DateTime.Today.AddDays(60), MaxScore = 100 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExam(Exam model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Exams.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Exam created.";
            return RedirectToAction(nameof(ManageExams));
        }

        public async Task<IActionResult> EditExam(int id)
        {
            var exam = await _db.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name", exam.CourseId);
            return View(exam);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExam(Exam model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _db.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
                return View(model);
            }
            _db.Exams.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Exam updated.";
            return RedirectToAction(nameof(ManageExams));
        }

        // ── Attendance ────────────────────────────────────────────────────
        public async Task<IActionResult> Attendance(int? courseId)
        {
            var query = _db.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course)
                .Include(e => e.AttendanceRecords)
                .AsQueryable();
            if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId);
            ViewBag.Courses = new SelectList(await _db.Courses.ToListAsync(), "Id", "Name", courseId);
            ViewBag.SelectedCourse = courseId;
            return View(await query.ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordAttendance(int enrolmentId, int weekNumber, bool present)
        {
            var existing = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.CourseEnrolmentId == enrolmentId && a.WeekNumber == weekNumber);
            if (existing == null)
            {
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    CourseEnrolmentId = enrolmentId,
                    WeekNumber = weekNumber,
                    Date = DateTime.Today,
                    Present = present
                });
            }
            else existing.Present = present;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
