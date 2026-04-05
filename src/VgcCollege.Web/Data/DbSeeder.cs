using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();

            // ── Roles ──────────────────────────────────────────────────────
            foreach (var role in new[] { "Admin", "Faculty", "Student" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin user ─────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("admin@vgc.ie") == null)
            {
                var admin = new ApplicationUser { UserName = "admin@vgc.ie", Email = "admin@vgc.ie", FullName = "Site Administrator", EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin@123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Faculty users ──────────────────────────────────────────────
            var faculty1Id = string.Empty;
            var faculty2Id = string.Empty;
            if (await userManager.FindByEmailAsync("mary.smith@vgc.ie") == null)
            {
                var f1 = new ApplicationUser { UserName = "mary.smith@vgc.ie", Email = "mary.smith@vgc.ie", FullName = "Mary Smith", EmailConfirmed = true };
                await userManager.CreateAsync(f1, "Faculty@123!");
                await userManager.AddToRoleAsync(f1, "Faculty");
                faculty1Id = f1.Id;
            }
            else faculty1Id = (await userManager.FindByEmailAsync("mary.smith@vgc.ie"))!.Id;

            if (await userManager.FindByEmailAsync("john.doe@vgc.ie") == null)
            {
                var f2 = new ApplicationUser { UserName = "john.doe@vgc.ie", Email = "john.doe@vgc.ie", FullName = "John Doe", EmailConfirmed = true };
                await userManager.CreateAsync(f2, "Faculty@123!");
                await userManager.AddToRoleAsync(f2, "Faculty");
                faculty2Id = f2.Id;
            }
            else faculty2Id = (await userManager.FindByEmailAsync("john.doe@vgc.ie"))!.Id;

            // ── Student users ──────────────────────────────────────────────
            var student1Id = string.Empty; var student2Id = string.Empty;
            var student3Id = string.Empty; var student4Id = string.Empty;

            async Task<string> EnsureStudent(string email, string name)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
                    await userManager.CreateAsync(u, "Student@123!");
                    await userManager.AddToRoleAsync(u, "Student");
                    return u.Id;
                }
                return (await userManager.FindByEmailAsync(email))!.Id;
            }

            student1Id = await EnsureStudent("alice.jones@student.vgc.ie", "Alice Jones");
            student2Id = await EnsureStudent("bob.murphy@student.vgc.ie", "Bob Murphy");
            student3Id = await EnsureStudent("claire.ryan@student.vgc.ie", "Claire Ryan");
            student4Id = await EnsureStudent("david.kelly@student.vgc.ie", "David Kelly");

            // ── Branches ──────────────────────────────────────────────────
            if (!await db.Branches.AnyAsync())
            {
                db.Branches.AddRange(
                    new Branch { Id = 1, Name = "VGC Dublin City", Address = "10 O'Connell Street, Dublin 1" },
                    new Branch { Id = 2, Name = "VGC Cork", Address = "5 Patrick Street, Cork" },
                    new Branch { Id = 3, Name = "VGC Galway", Address = "22 Shop Street, Galway" }
                );
                await db.SaveChangesAsync();
            }

            // ── Courses ────────────────────────────────────────────────────
            if (!await db.Courses.AnyAsync())
            {
                db.Courses.AddRange(
                    new Course { Id = 1, Name = "BSc Computer Science Year 1", BranchId = 1, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2025, 5, 31) },
                    new Course { Id = 2, Name = "BSc Computer Science Year 2", BranchId = 1, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2025, 5, 31) },
                    new Course { Id = 3, Name = "HND Business Studies", BranchId = 2, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2025, 5, 31) },
                    new Course { Id = 4, Name = "Diploma in Data Analytics", BranchId = 3, StartDate = new DateTime(2025, 1, 6), EndDate = new DateTime(2025, 12, 19) }
                );
                await db.SaveChangesAsync();
            }

            // ── Faculty profiles ───────────────────────────────────────────
            if (!await db.FacultyProfiles.AnyAsync())
            {
                db.FacultyProfiles.AddRange(
                    new FacultyProfile { Id = 1, IdentityUserId = faculty1Id, Name = "Mary Smith", Email = "mary.smith@vgc.ie", Phone = "01-2345678" },
                    new FacultyProfile { Id = 2, IdentityUserId = faculty2Id, Name = "John Doe", Email = "john.doe@vgc.ie", Phone = "021-9876543" }
                );
                await db.SaveChangesAsync();
            }

            // ── Faculty course assignments ─────────────────────────────────
            if (!await db.FacultyCourseAssignments.AnyAsync())
            {
                db.FacultyCourseAssignments.AddRange(
                    new FacultyCourseAssignment { Id = 1, FacultyProfileId = 1, CourseId = 1, IsTutor = true },
                    new FacultyCourseAssignment { Id = 2, FacultyProfileId = 1, CourseId = 2, IsTutor = false },
                    new FacultyCourseAssignment { Id = 3, FacultyProfileId = 2, CourseId = 3, IsTutor = true },
                    new FacultyCourseAssignment { Id = 4, FacultyProfileId = 2, CourseId = 4, IsTutor = true }
                );
                await db.SaveChangesAsync();
            }

            // ── Student profiles ───────────────────────────────────────────
            if (!await db.StudentProfiles.AnyAsync())
            {
                db.StudentProfiles.AddRange(
                    new StudentProfile { Id = 1, IdentityUserId = student1Id, Name = "Alice Jones", Email = "alice.jones@student.vgc.ie", Phone = "087-1112222", StudentNumber = "STU001", DateOfBirth = new DateTime(2003, 4, 12) },
                    new StudentProfile { Id = 2, IdentityUserId = student2Id, Name = "Bob Murphy", Email = "bob.murphy@student.vgc.ie", Phone = "086-3334444", StudentNumber = "STU002", DateOfBirth = new DateTime(2002, 11, 8) },
                    new StudentProfile { Id = 3, IdentityUserId = student3Id, Name = "Claire Ryan", Email = "claire.ryan@student.vgc.ie", Phone = "085-5556666", StudentNumber = "STU003", DateOfBirth = new DateTime(2004, 2, 20) },
                    new StudentProfile { Id = 4, IdentityUserId = student4Id, Name = "David Kelly", Email = "david.kelly@student.vgc.ie", Phone = "083-7778888", StudentNumber = "STU004", DateOfBirth = new DateTime(2003, 7, 15) }
                );
                await db.SaveChangesAsync();
            }

            // ── Enrolments ────────────────────────────────────────────────
            if (!await db.CourseEnrolments.AnyAsync())
            {
                db.CourseEnrolments.AddRange(
                    new CourseEnrolment { Id = 1, StudentProfileId = 1, CourseId = 1, EnrolDate = new DateTime(2024, 8, 20), Status = "Active" },
                    new CourseEnrolment { Id = 2, StudentProfileId = 2, CourseId = 1, EnrolDate = new DateTime(2024, 8, 20), Status = "Active" },
                    new CourseEnrolment { Id = 3, StudentProfileId = 3, CourseId = 2, EnrolDate = new DateTime(2024, 8, 20), Status = "Active" },
                    new CourseEnrolment { Id = 4, StudentProfileId = 4, CourseId = 3, EnrolDate = new DateTime(2024, 8, 20), Status = "Active" }
                );
                await db.SaveChangesAsync();
            }

            // ── Attendance records ─────────────────────────────────────────
            if (!await db.AttendanceRecords.AnyAsync())
            {
                var records = new List<AttendanceRecord>();
                var enrolIds = new[] { 1, 2 };
                var rng = new Random(42);
                int id = 1;
                foreach (var eid in enrolIds)
                {
                    for (int w = 1; w <= 8; w++)
                    {
                        records.Add(new AttendanceRecord
                        {
                            Id = id++,
                            CourseEnrolmentId = eid,
                            WeekNumber = w,
                            Date = new DateTime(2024, 9, 1).AddDays((w - 1) * 7),
                            Present = rng.NextDouble() > 0.2
                        });
                    }
                }
                db.AttendanceRecords.AddRange(records);
                await db.SaveChangesAsync();
            }

            // ── Assignments ───────────────────────────────────────────────
            if (!await db.Assignments.AnyAsync())
            {
                db.Assignments.AddRange(
                    new Assignment { Id = 1, CourseId = 1, Title = "OOP Assignment 1 – Basics", MaxScore = 100, DueDate = new DateTime(2024, 10, 15) },
                    new Assignment { Id = 2, CourseId = 1, Title = "OOP Assignment 2 – Design Patterns", MaxScore = 100, DueDate = new DateTime(2024, 11, 20) },
                    new Assignment { Id = 3, CourseId = 1, Title = "Web Dev Project", MaxScore = 100, DueDate = new DateTime(2025, 1, 31) },
                    new Assignment { Id = 4, CourseId = 2, Title = "Algorithms Assignment", MaxScore = 100, DueDate = new DateTime(2024, 10, 20) },
                    new Assignment { Id = 5, CourseId = 3, Title = "Business Report", MaxScore = 100, DueDate = new DateTime(2024, 11, 1) }
                );
                await db.SaveChangesAsync();
            }

            // ── Assignment results ─────────────────────────────────────────
            if (!await db.AssignmentResults.AnyAsync())
            {
                db.AssignmentResults.AddRange(
                    new AssignmentResult { Id = 1, AssignmentId = 1, StudentProfileId = 1, Score = 78, Feedback = "Good understanding of classes and interfaces." },
                    new AssignmentResult { Id = 2, AssignmentId = 1, StudentProfileId = 2, Score = 65, Feedback = "Needs more attention to encapsulation." },
                    new AssignmentResult { Id = 3, AssignmentId = 2, StudentProfileId = 1, Score = 85, Feedback = "Excellent use of Factory pattern." },
                    new AssignmentResult { Id = 4, AssignmentId = 2, StudentProfileId = 2, Score = 70, Feedback = "Adequate but missed Singleton explanation." },
                    new AssignmentResult { Id = 5, AssignmentId = 4, StudentProfileId = 3, Score = 88, Feedback = "Well-structured algorithms." },
                    new AssignmentResult { Id = 6, AssignmentId = 5, StudentProfileId = 4, Score = 74, Feedback = "Good analysis but weak conclusion." }
                );
                await db.SaveChangesAsync();
            }

            // ── Exams ─────────────────────────────────────────────────────
            if (!await db.Exams.AnyAsync())
            {
                db.Exams.AddRange(
                    new Exam { Id = 1, CourseId = 1, Title = "Semester 1 OOP Exam", Date = new DateTime(2025, 1, 20), MaxScore = 100, ResultsReleased = true },
                    new Exam { Id = 2, CourseId = 1, Title = "Semester 2 Final Exam", Date = new DateTime(2025, 5, 10), MaxScore = 100, ResultsReleased = false },
                    new Exam { Id = 3, CourseId = 2, Title = "Algorithms Final Exam", Date = new DateTime(2025, 5, 12), MaxScore = 100, ResultsReleased = false },
                    new Exam { Id = 4, CourseId = 3, Title = "Business Studies Exam", Date = new DateTime(2025, 5, 8), MaxScore = 100, ResultsReleased = true }
                );
                await db.SaveChangesAsync();
            }

            // ── Exam results ──────────────────────────────────────────────
            if (!await db.ExamResults.AnyAsync())
            {
                db.ExamResults.AddRange(
                    new ExamResult { Id = 1, ExamId = 1, StudentProfileId = 1, Score = 72, Grade = "B" },
                    new ExamResult { Id = 2, ExamId = 1, StudentProfileId = 2, Score = 58, Grade = "C" },
                    new ExamResult { Id = 3, ExamId = 2, StudentProfileId = 1, Score = 81, Grade = "A" },
                    new ExamResult { Id = 4, ExamId = 2, StudentProfileId = 2, Score = 63, Grade = "C" },
                    new ExamResult { Id = 5, ExamId = 4, StudentProfileId = 4, Score = 77, Grade = "B" }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
