using AEMS.Test.Helper;
using BusinessLogic.DTOs.Authentication.Register;
using BusinessLogic.Helper;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using UserEntity = DataAccess.Entities.User;

namespace AEMS.Test.UI.UnitTest.Register
{

	public class UserServiceTest
	{
		[Fact]
		public async Task RegisterStudentAsync_WithFirstJsonRecord_CreatesStudentSuccessfully()
		{
			var dto = TestDataLoader.LoadStudentRegisterRequest("users.json", 0);

			UserEntity? createdUser = null;
			StudentProfile? createdProfile = null;

			var userRepository = new Mock<IUserRepository>();
			userRepository.Setup(x => x.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
			userRepository.Setup(x => x.CreateAsync(It.IsAny<UserEntity>()))
				.Callback<UserEntity>(user => createdUser = user)
				.Returns(Task.CompletedTask);

			var roleRepository = new Mock<IGenericRepository<Role>>();
			roleRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>?>()))
				.ReturnsAsync(new Role
				{
					Id = "student-role-id",
					RoleName = RoleEnum.Student
				});

			var studentProfileRepository = new Mock<IGenericRepository<StudentProfile>>();
			studentProfileRepository.Setup(x => x.CreateAsync(It.IsAny<StudentProfile>()))
				.Callback<StudentProfile>(profile => createdProfile = profile)
				.Returns(Task.CompletedTask);

			var transaction = new Mock<IDbContextTransaction>();
			var unitOfWork = new Mock<IUnitOfWork>();
			unitOfWork.SetupGet(x => x.Users).Returns(userRepository.Object);
			unitOfWork.SetupGet(x => x.Roles).Returns(roleRepository.Object);
			unitOfWork.SetupGet(x => x.StudentProfiles).Returns(studentProfileRepository.Object);
			unitOfWork.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
			unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

			var authService = new AuthService(unitOfWork.Object, Mock.Of<IEmailService>());

			await authService.RegisterStudentAsync(dto);

			Assert.NotNull(createdUser);
			Assert.NotNull(createdProfile);
			Assert.Equal(dto.Email, createdUser!.Email);
			Assert.Equal(dto.FullName, createdUser.FullName);
			Assert.Equal(dto.Phone, createdUser.Phone);
			Assert.Equal("student-role-id", createdUser.RoleId);
			Assert.Equal(UserStatusEnum.Active, createdUser.Status);
			Assert.True(HashPasswordHelper.VerifyPassword(dto.Password, createdUser.PasswordHash!));
			Assert.Equal(dto.StudentCode, createdProfile!.StudentCode);
			Assert.Equal(createdUser.Id, createdProfile.UserId);
			transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task RegisterStudentAsync_WithSecondJsonRecord_ThrowsException_WhenEmailAlreadyExists()
		{
			var dto = TestDataLoader.LoadStudentRegisterRequest("users.json", 1);

			var userRepository = new Mock<IUserRepository>();
			userRepository.Setup(x => x.ExistsByEmailAsync(dto.Email)).ReturnsAsync(true);

			var unitOfWork = new Mock<IUnitOfWork>();
			unitOfWork.SetupGet(x => x.Users).Returns(userRepository.Object);

			var authService = new AuthService(unitOfWork.Object, Mock.Of<IEmailService>());

			var exception = await Assert.ThrowsAsync<Exception>(() => authService.RegisterStudentAsync(dto));

			Assert.Equal("Email đã tồn tại trong hệ thống.", exception.Message);
		}
	}
}
