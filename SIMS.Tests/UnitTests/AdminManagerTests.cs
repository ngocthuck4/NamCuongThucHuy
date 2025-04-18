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
    public class AdminManagerTests
    {
        private readonly Mock<ICsvRepository> _repositoryMock;
        private readonly AdminManager _manager;

        public AdminManagerTests()
        {
            _repositoryMock = new Mock<ICsvRepository>();
            _manager = new AdminManager(_repositoryMock.Object);
        }

        [Fact]
        public void AddClass_NewClass_AddsSuccessfully()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>();
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act
            _manager.AddClass(classId, 3, 1, "Mon 9AM");

            // Assert
            _repositoryMock.Verify(r => r.WriteClasses(It.Is<List<Class>>(list =>
                list.Count == 1 &&
                list[0].Id == classId &&
                list[0].SubjectId == 3 &&
                list[0].SemesterId == 1 &&
                list[0].Schedule == "Mon 9AM")), Times.Once());
        }

        [Fact]
        public void AddClass_ClassExists_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, Schedule = "Mon 9AM" }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);

            // Act & Assert
            Action act = () => _manager.AddClass(classId, 3, 1, "Tue 10AM");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Class already exists.");
            _repositoryMock.Verify(r => r.WriteClasses(It.IsAny<List<Class>>()), Times.Never());
        }

        [Fact]
        public void AssignTeacherToClass_ValidClassAndTeacher_AssignsSuccessfully()
        {
            // Arrange
            var classId = 1;
            var teacherUsername = "teacher1";
            var classes = new List<Class>
    {
        new Class { Id = classId, SubjectId = 3, SemesterId = 1, Schedule = "Mon 9AM" }
    };
            var teachers = new List<User>
    {
        new User { Username = teacherUsername, Role = UserRole.Teacher }
    };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadTeachers()).Returns(teachers);

            // Act
            _manager.AssignTeacherToClass(classId, teacherUsername);

            // Assert
            _repositoryMock.Verify(r => r.WriteClasses(It.Is<List<Class>>(list =>
                list.Count == 1 &&
                list[0].Id == classId &&
                list[0].TeacherUsername == teacherUsername)), Times.Once());
        }

        [Fact]
        public void AssignTeacherToClass_ClassNotFound_ThrowsException()
        {
            // Arrange
            var classId = 999;
            var teacherUsername = "teacher1";
            var classes = new List<Class>
    {
        new Class { Id = 1, SubjectId = 3, SemesterId = 1, Schedule = "Mon 9AM" }
    };
            var teachers = new List<User>
    {
        new User { Username = teacherUsername, Role = UserRole.Teacher }
    };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadTeachers()).Returns(teachers);

            // Act & Assert
            Action act = () => _manager.AssignTeacherToClass(classId, teacherUsername);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Class not found.");
            _repositoryMock.Verify(r => r.WriteClasses(It.IsAny<List<Class>>()), Times.Never());
        }

        [Fact]
        public void AssignTeacherToClass_TeacherNotFound_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var teacherUsername = "teacher999";
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, Schedule = "Mon 9AM" }
            };
            var teachers = new List<User>
            {
                new User { Username = "teacher1", Role = UserRole.Teacher }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadTeachers()).Returns(teachers);

            // Act & Assert
            Action act = () => _manager.AssignTeacherToClass(classId, teacherUsername);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Teacher not found.");
            _repositoryMock.Verify(r => r.WriteClasses(It.IsAny<List<Class>>()), Times.Never());
        }
    }
}