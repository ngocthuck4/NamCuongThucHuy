using AuthCsvApp.Managers;
using AuthCsvApp.Repositories;
using Xunit;

namespace SIMS.Tests
{
    public class TestSetup
    {
        [Fact]
        public void CanAccessStudentClassManager()
        {
            var manager = new StudentClassManager(null, "student1"); // Cung cấp studentUsername
            Assert.NotNull(manager);
        }
    }
}