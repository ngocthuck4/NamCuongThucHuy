using FluentAssertions;
using Moq;
using AuthCsvApp.Models;
using AuthCsvApp.Managers;
using AuthCsvApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SIMS.Tests.UnitTests
{
    public class TeacherManagerTests
    {
        private readonly Mock<ICsvRepository> _repositoryMock;
        private readonly string _teacherUsername = "teacher1";
        private readonly TeacherManager _manager;

        public TeacherManagerTests()
        {
            _repositoryMock = new Mock<ICsvRepository>();
            _manager = new TeacherManager(_repositoryMock.Object, _teacherUsername);
        }

        [Fact]
        public void GetStudentsInClass_ValidClass_ReturnsStudentList()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, ClassId = classId, StudentUsername = "student1", SubjectId = 3, RegistrationDate = DateTime.Now },
                new SubjectRegistration { Id = 2, ClassId = classId, StudentUsername = "student2", SubjectId = 3, RegistrationDate = DateTime.Now }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act
            var result = _manager.GetStudentsInClass(classId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(new List<string> { "student1", "student2" });
        }

        [Fact]
        public void GetStudentsInClass_ClassNotFound_ThrowsException()
        {
            // Arrange
            var classId = 999;
            var classes = new List<Class>
            {
                new Class { Id = 1, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act & Assert
            Action act = () => _manager.GetStudentsInClass(classId);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Class not found.");
        }

        [Fact]
        public void GetStudentsInClass_UnauthorizedTeacher_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher2", Schedule = "Mon 9AM" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act & Assert
            Action act = () => _manager.GetStudentsInClass(classId);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You are not authorized to view students in this class.");
        }

        [Fact]
        public void EnterGrades_ValidInput_AddsGradeSuccessfully()
        {
            // Arrange
            var classId = 1;
            var studentUsername = "student1";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, ClassId = classId, StudentUsername = studentUsername, SubjectId = 3, RegistrationDate = DateTime.Now }
            };
            var grades = new List<Grade>();
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);
            _repositoryMock.Setup(r => r.ReadGrades()).Returns(grades);

            // Act
            _manager.EnterGrades(classId, studentUsername, 7.0, 8.0);

            // Assert
            _repositoryMock.Verify(r => r.WriteGrades(It.Is<List<Grade>>(list =>
                list.Count == 1 &&
                list[0].ClassId == classId &&
                list[0].StudentUsername == studentUsername &&
                list[0].MidtermScore == 7.0 &&
                list[0].FinalScore == 8.0 &&
                Math.Abs(list[0].TotalScore - (7.0 * 0.4 + 8.0 * 0.6)) < 0.01 &&
                list[0].Classification == "Pass")), Times.Once());
        }

        [Fact]
        public void EnterGrades_ExistingGrade_UpdatesSuccessfully()
        {
            // Arrange
            var classId = 1;
            var studentUsername = "student1";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, ClassId = classId, StudentUsername = studentUsername, SubjectId = 3, RegistrationDate = DateTime.Now }
            };
            var grades = new List<Grade>
            {
                new Grade { Id = 1, ClassId = classId, StudentUsername = studentUsername, MidtermScore = 5.0, FinalScore = 6.0, TotalScore = 5.6, Classification = "Pass" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);
            _repositoryMock.Setup(r => r.ReadGrades()).Returns(grades);

            // Act
            _manager.EnterGrades(classId, studentUsername, 8.0, 9.0);

            // Assert
            _repositoryMock.Verify(r => r.WriteGrades(It.Is<List<Grade>>(list =>
                list.Count == 1 &&
                list[0].ClassId == classId &&
                list[0].StudentUsername == studentUsername &&
                list[0].MidtermScore == 8.0 &&
                list[0].FinalScore == 9.0 &&
                Math.Abs(list[0].TotalScore - (8.0 * 0.4 + 9.0 * 0.6)) < 0.01 &&
                list[0].Classification == "Pass")), Times.Once());
        }

        [Fact]
        public void EnterGrades_ClassNotFound_ThrowsException()
        {
            // Arrange
            var classId = 999;
            var studentUsername = "student1";
            var classes = new List<Class>
            {
                new Class { Id = 1, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act & Assert
            Action act = () => _manager.EnterGrades(classId, studentUsername, 7.0, 8.0);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Class not found.");
            _repositoryMock.Verify(r => r.WriteGrades(It.IsAny<List<Grade>>()), Times.Never());
        }

        [Fact]
        public void EnterGrades_UnauthorizedTeacher_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var studentUsername = "student1";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher2", Schedule = "Mon 9AM" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act & Assert
            Action act = () => _manager.EnterGrades(classId, studentUsername, 7.0, 8.0);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You are not authorized to enter grades for this class.");
            _repositoryMock.Verify(r => r.WriteGrades(It.IsAny<List<Grade>>()), Times.Never());
        }

        [Fact]
        public void EnterGrades_StudentNotRegistered_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var studentUsername = "student999";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, ClassId = classId, StudentUsername = "student1", SubjectId = 3, RegistrationDate = DateTime.Now }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act & Assert
            Action act = () => _manager.EnterGrades(classId, studentUsername, 7.0, 8.0);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Student is not registered in this class.");
            _repositoryMock.Verify(r => r.WriteGrades(It.IsAny<List<Grade>>()), Times.Never());
        }

        [Fact]
        public void EnterGrades_InvalidScores_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var studentUsername = "student1";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = _teacherUsername, Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, ClassId = classId, StudentUsername = studentUsername, SubjectId = 3, RegistrationDate = DateTime.Now }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act & Assert
            Action act = () => _manager.EnterGrades(classId, studentUsername, -1.0, 8.0);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Scores must be between 0 and 10.");
            _repositoryMock.Verify(r => r.WriteGrades(It.IsAny<List<Grade>>()), Times.Never());
        }
    }
}