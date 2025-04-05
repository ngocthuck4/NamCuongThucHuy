
using AuthCsvApp.Repositories;
using AuthCsvApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthCsvApp.Services
{
    public class AdminAccountService
    {
        private readonly CsvRepository _repository;

        public AdminAccountService(CsvRepository repository)
        {
            _repository = repository;
        }

        public List<User> GetUsers(string search, string role)
        {
            var users = _repository.ReadUsers();

            // Tìm kiếm theo Username hoặc FullName
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                             .ToList();
            }

            // Lọc theo Role
            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role.ToString() == role).ToList();
            }

            return users;
        }

        public bool AddUser(User model, out string errorMessage)
        {
            var users = _repository.ReadUsers();

            // Kiểm tra xem username đã tồn tại chưa
            if (users.Any(u => u.Username == model.Username))
            {
                errorMessage = "Username already exists!";
                return false;
            }

            model.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
            users.Add(model);
            _repository.WriteUsers(users);

            errorMessage = null;
            return true;
        }

        public User GetUserById(int id)
        {
            var users = _repository.ReadUsers();
            return users.FirstOrDefault(u => u.Id == id);
        }

        public bool UpdateUser(int id, User model, out string errorMessage)
        {
            var users = _repository.ReadUsers();
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                errorMessage = "Account not found.";
                return false;
            }

            // Kiểm tra xem username mới có bị trùng với tài khoản khác không
            if (users.Any(u => u.Username == model.Username && u.Id != id))
            {
                errorMessage = "Username already exists!";
                return false;
            }

            // Cập nhật thông tin người dùng
            user.FullName = model.FullName;
            user.Address = model.Address;
            user.Username = model.Username;
            user.Password = model.Password;
            user.Role = model.Role;
            _repository.WriteUsers(users);

            errorMessage = null;
            return true;
        }

        public bool DeleteUser(int id, string adminUsername, out string errorMessage)
        {
            var users = _repository.ReadUsers();
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                errorMessage = "Account not found.";
                return false;
            }

            // Ngăn admin xóa chính tài khoản của mình
            if (user.Username == adminUsername)
            {
                errorMessage = "You cannot delete your own account!";
                return false;
            }

            // Xóa các dữ liệu liên quan
            var classes = _repository.ReadClasses();
            classes.RemoveAll(c => c.TeacherUsername == user.Username);
            _repository.WriteClasses(classes);

            var registrations = _repository.ReadSubjectRegistrations();
            registrations.RemoveAll(r => r.StudentUsername == user.Username);
            _repository.WriteSubjectRegistrations(registrations);

            var grades = _repository.ReadGrades();
            grades.RemoveAll(g => g.StudentUsername == user.Username);
            _repository.WriteGrades(grades);

            var notifications = _repository.ReadNotifications();
            notifications.RemoveAll(n => n.StudentUsername == user.Username);
            _repository.WriteNotifications(notifications);

            // Xóa người dùng
            users.Remove(user);
            _repository.WriteUsers(users);

            errorMessage = null;
            return true;
        }
    }
}