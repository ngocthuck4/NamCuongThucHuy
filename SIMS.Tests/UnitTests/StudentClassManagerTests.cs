using FluentAssertions;
using Moq;
using AuthCsvApp.Models;
using AuthCsvApp.Managers;
using AuthCsvApp.Interfaces; // Thêm namespace cho ICsvRepository
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SIMS.Tests.UnitTests
{
    public class StudentClassManagerTests
    {
        private readonly Mock<ICsvRepository> _repositoryMock; // Sửa từ CsvRepository thành ICsvRepository
        private readonly StudentClassManager _manager;
        private readonly string _studentUsername = "student1";

        public StudentClassManagerTests()
        {
            _repositoryMock = new Mock<ICsvRepository>();
            _manager = new StudentClassManager(_repositoryMock.Object, _studentUsername);
        }

        [Fact]
        public void GetClasses_RegisteredClasses_ReturnsCorrectClasses()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher1", Schedule = "Mon 9AM" },
                new Class { Id = 2, SubjectId = 4, SemesterId = 1, TeacherUsername = "teacher2", Schedule = "Tue 10AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, SubjectId = 3, StudentUsername = _studentUsername, ClassId = classId, RegistrationDate = DateTime.Now }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act
            var result = _manager.GetClasses();

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainSingle(c => c.Id == classId && c.SubjectId == 3);
        }

        [Fact]
        public void GetClasses_NoRegistrations_ReturnsEmptyList()
        {
            // Arrange
            var classes = new List<Class>
            {
                new Class { Id = 1, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher1", Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>(); // No registrations
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act
            var result = _manager.GetClasses();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void RegisterClass_ValidClass_AddsRegistration()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
    {
        new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher1", Schedule = "Mon 9AM" }
    };
            var registrations = new List<SubjectRegistration>();
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act
            _manager.RegisterClass(classId, _studentUsername);

            // Assert
            _repositoryMock.Verify(r => r.WriteSubjectRegistrations(It.Is<List<SubjectRegistration>>(list =>
                list.Count == 1 &&
                list[0].ClassId == classId &&
                list[0].StudentUsername == _studentUsername)), Times.Once());
        }

        [Fact]
        public void RegisterClass_ClassNotFound_ThrowsException()
        {
            // Arrange
            var classId = 999;
            var classes = new List<Class>
            {
                new Class { Id = 1, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher1", Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>();
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act & Assert
            Action act = () => _manager.RegisterClass(classId, _studentUsername);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Class not found.");
            _repositoryMock.Verify(r => r.WriteSubjectRegistrations(It.IsAny<List<SubjectRegistration>>()), Times.Never());
        }

        [Fact]
        public void RegisterClass_AlreadyRegistered_ThrowsException()
        {
            // Arrange
            var classId = 1;
            var classes = new List<Class>
            {
                new Class { Id = classId, SubjectId = 3, SemesterId = 1, TeacherUsername = "teacher1", Schedule = "Mon 9AM" }
            };
            var registrations = new List<SubjectRegistration>
            {
                new SubjectRegistration { Id = 1, SubjectId = 3, StudentUsername = _studentUsername, ClassId = classId, RegistrationDate = DateTime.Now }
            };
            _repositoryMock.Setup(r => r.ReadClasses()).Returns(classes);
            _repositoryMock.Setup(r => r.ReadSubjectRegistrations()).Returns(registrations);

            // Act & Assert
            Action act = () => _manager.RegisterClass(classId, _studentUsername);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You have already registered for this class.");
            _repositoryMock.Verify(r => r.WriteSubjectRegistrations(It.IsAny<List<SubjectRegistration>>()), Times.Never());
        }
    }
}