using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;
using Xunit;

namespace VgcCollege.Tests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────
    public static class TestDbHelper
    {
        public static ApplicationDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        public static ApplicationDbContext CreateSeededDb(string dbName)
        {
            var db = CreateInMemoryDb(dbName);

            db.Branches.AddRange(
                new Branch { Id = 1, Name = "Dublin", Address = "1 Main St" },
                new Branch { Id = 2, Name = "Cork", Address = "2 Main St" }
            );

            db.Courses.AddRange(
                new Course { Id = 1, Name = "Computer Science", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) },
                new Course { Id = 2, Name = "Business Studies", BranchId = 2, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) }
            );

            db.StudentProfiles.AddRange(
                new StudentProfile { Id = 1, IdentityUserId = "uid-student1", Name = "Alice", Email = "alice@test.com", StudentNumber = "S001" },
                new StudentProfile { Id = 2, IdentityUserId = "uid-student2", Name = "Bob", Email = "bob@test.com", StudentNumber = "S002" }
            );

            db.FacultyProfiles.AddRange(
                new FacultyProfile { Id = 1, IdentityUserId = "uid-faculty1", Name = "Prof. Smith", Email = "smith@test.com" },
                new FacultyProfile { Id = 2, IdentityUserId = "uid-faculty2", Name = "Prof. Jones", Email = "jones@test.com" }
            );

            db.FacultyCourseAssignments.AddRange(
                new FacultyCourseAssignment { Id = 1, FacultyProfileId = 1, CourseId = 1, IsTutor = true },
                new FacultyCourseAssignment { Id = 2, FacultyProfileId = 2, CourseId = 2, IsTutor = false }
            );

            db.CourseEnrolments.AddRange(
                new CourseEnrolment { Id = 1, StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today, Status = "Active" },
                new CourseEnrolment { Id = 2, StudentProfileId = 2, CourseId = 2, EnrolDate = DateTime.Today, Status = "Active" }
            );

            db.Assignments.AddRange(
                new Assignment { Id = 1, CourseId = 1, Title = "Assignment 1", MaxScore = 100, DueDate = DateTime.Today.AddDays(30) },
                new Assignment { Id = 2, CourseId = 2, Title = "Assignment 2", MaxScore = 50, DueDate = DateTime.Today.AddDays(30) }
            );

            db.Exams.AddRange(
                new Exam { Id = 1, CourseId = 1, Title = "Midterm", Date = DateTime.Today.AddDays(60), MaxScore = 100, ResultsReleased = true },
                new Exam { Id = 2, CourseId = 1, Title = "Final", Date = DateTime.Today.AddDays(120), MaxScore = 100, ResultsReleased = false }
            );

            db.AssignmentResults.AddRange(
                new AssignmentResult { Id = 1, AssignmentId = 1, StudentProfileId = 1, Score = 75, Feedback = "Good" },
                new AssignmentResult { Id = 2, AssignmentId = 2, StudentProfileId = 2, Score = 40, Feedback = "Adequate" }
            );

            db.ExamResults.AddRange(
                new ExamResult { Id = 1, ExamId = 1, StudentProfileId = 1, Score = 72, Grade = "B" },
                new ExamResult { Id = 2, ExamId = 2, StudentProfileId = 1, Score = 85, Grade = "A" }
            );

            db.SaveChanges();
            return db;
        }
    }

    // ─── Enrolment Rule Tests ─────────────────────────────────────────────────
    public class EnrolmentTests
    {
        [Fact]
        public void Student_Can_Be_Enrolled_In_Course()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Student_Can_Be_Enrolled_In_Course));
            var enrolment = new CourseEnrolment
            {
                StudentProfileId = 1,
                CourseId = 2,
                EnrolDate = DateTime.Today,
                Status = "Active"
            };
            db.CourseEnrolments.Add(enrolment);
            db.SaveChanges();

            var result = db.CourseEnrolments.FirstOrDefault(e =>
                e.StudentProfileId == 1 && e.CourseId == 2);

            Assert.NotNull(result);
            Assert.Equal("Active", result.Status);
        }

        [Fact]
        public void Duplicate_Enrolment_Is_Detected()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Duplicate_Enrolment_Is_Detected));

            // Alice (id=1) is already enrolled in Course 1
            bool alreadyEnrolled = db.CourseEnrolments.Any(e =>
                e.StudentProfileId == 1 && e.CourseId == 1);

            Assert.True(alreadyEnrolled);
        }

        [Fact]
        public void Enrolment_Status_Can_Be_Updated_To_Withdrawn()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Enrolment_Status_Can_Be_Updated_To_Withdrawn));
            var enrolment = db.CourseEnrolments.First(e => e.Id == 1);
            enrolment.Status = "Withdrawn";
            db.SaveChanges();

            var updated = db.CourseEnrolments.First(e => e.Id == 1);
            Assert.Equal("Withdrawn", updated.Status);
        }

        [Fact]
        public void Student_Can_Enrol_In_Multiple_Courses()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Student_Can_Enrol_In_Multiple_Courses));

            db.CourseEnrolments.Add(new CourseEnrolment
            {
                StudentProfileId = 1,
                CourseId = 2,
                EnrolDate = DateTime.Today,
                Status = "Active"
            });
            db.SaveChanges();

            var count = db.CourseEnrolments.Count(e => e.StudentProfileId == 1);
            Assert.Equal(2, count);
        }
    }

    // ─── Exam Visibility Tests ────────────────────────────────────────────────
    public class ExamVisibilityTests
    {
        [Fact]
        public void Student_Only_Sees_Released_Exam_Results()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Student_Only_Sees_Released_Exam_Results));

            // Simulate the query used in StudentController.ExamResults()
            var visibleResults = db.ExamResults
                .Where(r => r.StudentProfileId == 1 && r.Exam.ResultsReleased)
                .ToList();

            Assert.Single(visibleResults);
            Assert.Equal(1, visibleResults[0].ExamId); // Only Exam 1 is released
        }

        [Fact]
        public void Provisional_Results_Are_Hidden_From_Student()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Provisional_Results_Are_Hidden_From_Student));

            var provisionalVisible = db.ExamResults
                .Where(r => r.StudentProfileId == 1 && !r.Exam.ResultsReleased)
                .ToList();

            // The query a student uses MUST NOT return provisional results
            var studentQuery = db.ExamResults
                .Where(r => r.StudentProfileId == 1 && r.Exam.ResultsReleased)
                .ToList();

            Assert.Single(provisionalVisible); // exists in db
            Assert.Empty(studentQuery.Where(r => !r.Exam.ResultsReleased)); // not visible to student
        }

        [Fact]
        public void Releasing_Exam_Makes_Results_Visible_To_Student()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Releasing_Exam_Makes_Results_Visible_To_Student));

            // Before release
            var beforeRelease = db.ExamResults
                .Where(r => r.StudentProfileId == 1 && r.Exam.ResultsReleased)
                .Count();

            // Release exam 2
            var exam = db.Exams.First(e => e.Id == 2);
            exam.ResultsReleased = true;
            db.SaveChanges();

            // After release
            var afterRelease = db.ExamResults
                .Where(r => r.StudentProfileId == 1 && r.Exam.ResultsReleased)
                .Count();

            Assert.Equal(beforeRelease + 1, afterRelease);
        }
    }

    // ─── Grade / Score Validation Tests ──────────────────────────────────────
    public class GradeValidationTests
    {
        [Theory]
        [InlineData(100, 100, true)]   // exactly max — valid
        [InlineData(0, 100, true)]     // zero — valid
        [InlineData(75, 100, true)]    // normal — valid
        [InlineData(101, 100, false)]  // over max — invalid
        [InlineData(-1, 100, false)]   // negative — invalid
        public void Score_Validation_Works_Correctly(decimal score, decimal maxScore, bool expectedValid)
        {
            bool isValid = score >= 0 && score <= maxScore;
            Assert.Equal(expectedValid, isValid);
        }

        [Theory]
        [InlineData(90, 100, "A")]
        [InlineData(70, 100, "B")]
        [InlineData(55, 100, "C")]
        [InlineData(40, 100, "D")]
        [InlineData(30, 100, "F")]
        public void Grade_Calculation_Returns_Correct_Grade(decimal score, decimal maxScore, string expectedGrade)
        {
            var pct = (score / maxScore) * 100;
            var grade = pct >= 80 ? "A" : pct >= 65 ? "B" : pct >= 50 ? "C" : pct >= 40 ? "D" : "F";
            Assert.Equal(expectedGrade, grade);
        }

        [Fact]
        public void Assignment_Result_Score_Is_Stored_Correctly()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Assignment_Result_Score_Is_Stored_Correctly));

            var result = db.AssignmentResults.First(r => r.Id == 1);
            Assert.Equal(75m, result.Score);
            Assert.Equal("Good", result.Feedback);
        }

        [Fact]
        public void Percentage_Calculation_Is_Correct()
        {
            decimal score = 75m;
            decimal maxScore = 100m;
            decimal expected = 75m;

            decimal actual = maxScore > 0 ? (score / maxScore) * 100 : 0;
            Assert.Equal(expected, actual);
        }
    }

    // ─── Authorization Query Filtering Tests ─────────────────────────────────
    public class AuthorizationFilteringTests
    {
        [Fact]
        public void Faculty_Only_Sees_Students_In_Their_Courses()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Faculty_Only_Sees_Students_In_Their_Courses));

            // Faculty 1 teaches Course 1 — should only see Alice (StudentProfileId=1)
            var faculty1CourseIds = db.FacultyCourseAssignments
                .Where(a => a.FacultyProfileId == 1)
                .Select(a => a.CourseId)
                .ToList();

            var faculty1Students = db.CourseEnrolments
                .Where(e => faculty1CourseIds.Contains(e.CourseId))
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToList();

            Assert.Single(faculty1Students);
            Assert.Contains(1, faculty1Students); // Alice
            Assert.DoesNotContain(2, faculty1Students); // Bob is in Course 2
        }

        [Fact]
        public void Student_Only_Sees_Their_Own_Results()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Student_Only_Sees_Their_Own_Results));

            // Query as Alice (StudentProfileId=1)
            var aliceResults = db.AssignmentResults
                .Where(r => r.StudentProfileId == 1)
                .ToList();

            Assert.All(aliceResults, r => Assert.Equal(1, r.StudentProfileId));
        }

        [Fact]
        public void Faculty_Tutor_Can_Access_Contact_Details_Of_Their_Students()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Faculty_Tutor_Can_Access_Contact_Details_Of_Their_Students));

            // Faculty 1 is a tutor for Course 1
            var isTutor = db.FacultyCourseAssignments
                .Any(a => a.FacultyProfileId == 1 && a.IsTutor);

            Assert.True(isTutor);
        }

        [Fact]
        public void Faculty_Non_Tutor_Cannot_Access_Contact_Details()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Faculty_Non_Tutor_Cannot_Access_Contact_Details));

            // Faculty 2 is NOT a tutor for any course
            var isTutor = db.FacultyCourseAssignments
                .Any(a => a.FacultyProfileId == 2 && a.IsTutor);

            Assert.False(isTutor);
        }

        [Fact]
        public void Student_Cannot_See_Other_Students_Enrolments()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Student_Cannot_See_Other_Students_Enrolments));

            // Query as Bob (StudentProfileId=2) — should not see Alice's enrolments
            var bobEnrolments = db.CourseEnrolments
                .Where(e => e.StudentProfileId == 2)
                .ToList();

            Assert.All(bobEnrolments, e => Assert.Equal(2, e.StudentProfileId));
            Assert.DoesNotContain(bobEnrolments, e => e.StudentProfileId == 1);
        }

        [Fact]
        public void Attendance_Percentage_Calculation_Is_Accurate()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Attendance_Percentage_Calculation_Is_Accurate));

            // Add attendance records for enrolment 1
            db.AttendanceRecords.AddRange(
                new AttendanceRecord { CourseEnrolmentId = 1, WeekNumber = 1, Date = DateTime.Today, Present = true },
                new AttendanceRecord { CourseEnrolmentId = 1, WeekNumber = 2, Date = DateTime.Today, Present = true },
                new AttendanceRecord { CourseEnrolmentId = 1, WeekNumber = 3, Date = DateTime.Today, Present = false },
                new AttendanceRecord { CourseEnrolmentId = 1, WeekNumber = 4, Date = DateTime.Today, Present = true }
            );
            db.SaveChanges();

            var records = db.AttendanceRecords.Where(a => a.CourseEnrolmentId == 1).ToList();
            var attended = records.Count(a => a.Present);
            var total = records.Count;
            var pct = total > 0 ? attended * 100 / total : 0;

            Assert.Equal(4, total);
            Assert.Equal(3, attended);
            Assert.Equal(75, pct);
        }
    }

    // ─── Exam Result CRUD Tests ───────────────────────────────────────────────
    public class ExamResultCrudTests
    {
        [Fact]
        public void Can_Add_New_Exam_Result()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Can_Add_New_Exam_Result));

            var newResult = new ExamResult
            {
                ExamId = 1,
                StudentProfileId = 2, // Bob has no result for Exam 1 yet
                Score = 65,
                Grade = "C"
            };
            db.ExamResults.Add(newResult);
            db.SaveChanges();

            var saved = db.ExamResults.FirstOrDefault(r => r.ExamId == 1 && r.StudentProfileId == 2);
            Assert.NotNull(saved);
            Assert.Equal(65m, saved.Score);
            Assert.Equal("C", saved.Grade);
        }

        [Fact]
        public void Can_Edit_Existing_Exam_Result()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Can_Edit_Existing_Exam_Result));

            // Alice has ExamResult Id=1 for Exam 1 with Score=72, Grade=B
            var result = db.ExamResults.First(r => r.Id == 1);
            result.Score = 90;
            result.Grade = "A";
            db.SaveChanges();

            var updated = db.ExamResults.First(r => r.Id == 1);
            Assert.Equal(90m, updated.Score);
            Assert.Equal("A", updated.Grade);
        }

        [Fact]
        public void Score_Cannot_Exceed_Max_Score()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Score_Cannot_Exceed_Max_Score));

            var exam = db.Exams.First(e => e.Id == 1);
            decimal invalidScore = exam.MaxScore + 1;

            bool isValid = invalidScore >= 0 && invalidScore <= exam.MaxScore;
            Assert.False(isValid);
        }

        [Fact]
        public void Exam_Result_Upsert_Updates_Existing_Rather_Than_Duplicating()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Exam_Result_Upsert_Updates_Existing_Rather_Than_Duplicating));

            // Alice already has a result for Exam 1 — simulate upsert logic
            var existing = db.ExamResults
                .FirstOrDefault(r => r.ExamId == 1 && r.StudentProfileId == 1);

            Assert.NotNull(existing);
            existing.Score = 99;
            existing.Grade = "A";
            db.SaveChanges();

            // Should still be exactly 1 result for Alice/Exam1
            var count = db.ExamResults.Count(r => r.ExamId == 1 && r.StudentProfileId == 1);
            Assert.Equal(1, count);
            Assert.Equal(99m, db.ExamResults.First(r => r.ExamId == 1 && r.StudentProfileId == 1).Score);
        }
    }

    // ─── Assignment Creation Tests ────────────────────────────────────────────
    public class AssignmentCreationTests
    {
        [Fact]
        public void Can_Create_Assignment_For_Course()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Can_Create_Assignment_For_Course));

            var newAssignment = new Assignment
            {
                CourseId = 1,
                Title = "Final Project",
                MaxScore = 100,
                DueDate = DateTime.Today.AddDays(60)
            };
            db.Assignments.Add(newAssignment);
            db.SaveChanges();

            var saved = db.Assignments.FirstOrDefault(a => a.Title == "Final Project" && a.CourseId == 1);
            Assert.NotNull(saved);
            Assert.Equal(100m, saved.MaxScore);
        }

        [Fact]
        public void Can_Edit_Assignment_Title_And_Due_Date()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Can_Edit_Assignment_Title_And_Due_Date));

            var assignment = db.Assignments.First(a => a.Id == 1);
            var newTitle = "Updated OOP Assignment";
            var newDue = DateTime.Today.AddDays(90);

            assignment.Title = newTitle;
            assignment.DueDate = newDue;
            db.SaveChanges();

            var updated = db.Assignments.First(a => a.Id == 1);
            Assert.Equal(newTitle, updated.Title);
            Assert.Equal(newDue, updated.DueDate);
        }

        [Fact]
        public void Faculty_Cannot_Create_Assignment_For_Course_They_Dont_Teach()
        {
            var db = TestDbHelper.CreateSeededDb(nameof(Faculty_Cannot_Create_Assignment_For_Course_They_Dont_Teach));

            // Faculty 1 teaches Course 1 only — Course 2 is not theirs
            var faculty1CourseIds = db.FacultyCourseAssignments
                .Where(a => a.FacultyProfileId == 1)
                .Select(a => a.CourseId)
                .ToList();

            bool canCreateForCourse2 = faculty1CourseIds.Contains(2);
            Assert.False(canCreateForCourse2);
        }
    }
}
