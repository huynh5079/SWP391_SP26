using BusinessLogic.DTOs.Event.Semester;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Event.Sub_Service.Semester;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Event.Semester
{
	public class SemesterService : ISemesterService
	{
		private readonly IUnitOfWork _uow;

		public SemesterService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<SemesterDTO> CreateSemesterAsync(SemesterDTO dto)
		{
			if (dto == null)
				throw new InvalidOperationException("Dữ liệu học kỳ không hợp lệ.");

			if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
				throw new InvalidOperationException("Tên và mã học kỳ không được để trống.");

          if (!TryParseSemesterName(dto.Name, out var semesterName))
				throw new InvalidOperationException("Tên học kỳ không hợp lệ. Chỉ chấp nhận Spring/Summer/Fall.");

			if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
				throw new InvalidOperationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

			var semesterYear = dto.year > 0
				? dto.year
				: dto.StartDate?.Year ?? DateTimeHelper.GetVietnamTime().Year;
			var semesters = await _uow.Semesters.GetAllAsync();

			var overlap = semesters.Any(s =>
				s.StartDate.HasValue && s.EndDate.HasValue &&
				dto.StartDate < s.EndDate &&
				dto.EndDate > s.StartDate
			);

			if (overlap)
				throw new InvalidOperationException("Semester bị trùng thời gian với semester khác."); 

			var normalizedCode = dto.Code.Trim();

          var duplicate = await _uow.Semesters.GetAsync(x =>
				(x.Name == semesterName && x.Year == semesterYear) ||
				(x.Code != null && x.Code == normalizedCode));

			if (duplicate != null)
				throw new InvalidOperationException("Tên hoặc mã học kỳ đã tồn tại.");

			var entity = new DataAccess.Entities.Semester
			{
              Name = semesterName,
				Code = normalizedCode,
              Year = semesterYear,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate,
				Status = ResolveStatus(dto.StartDate, dto.EndDate, dto.Status)
			};

			await _uow.Semesters.CreateAsync(entity);
			await _uow.SaveChangesAsync();

			return MapSemester(entity);
		}

		public async Task<SemesterDTO> AutoCreateSemesterAsync()
		{
			var now = DateTimeHelper.GetVietnamTime();

			var semesters = (await _uow.Semesters.GetAllAsync()).ToList();

			// Nếu đã có semester kế tiếp (StartDate > now) thì không tạo thêm
			if (semesters.Any(s => s.StartDate.HasValue && s.StartDate.Value > now))
				throw new InvalidOperationException("Đã có semester kế tiếp. Hãy chờ đến khi semester đó bắt đầu.");

			var latest = semesters
				.Where(s => s.EndDate.HasValue)
				.OrderByDescending(s => s.EndDate)
				.FirstOrDefault();

			SemesterNameEnum nextSemester;
			int nextYear;

			if (latest == null)
			{
				nextSemester = SemesterNameEnum.Spring;
				nextYear = now.Year;
			}
			else
			{
				var latestSemester = ResolveSemesterName(latest);
				var latestYear = ResolveYear(latest, now.Year);

				(nextSemester, nextYear) = latestSemester switch
				{
					SemesterNameEnum.Spring => (SemesterNameEnum.Summer, latestYear),
					SemesterNameEnum.Summer => (SemesterNameEnum.Fall, latestYear),
					_ => (SemesterNameEnum.Spring, latestYear + 1)
				};
			}

			var (startDate, endDate) = GetDefaultDateRange(nextSemester, nextYear);

			// Nếu dữ liệu cũ có kỳ trước kết thúc trễ hơn mốc chuẩn, dời kỳ mới sang sau ngày kết thúc kỳ trước
			if (latest?.EndDate.HasValue == true && startDate <= latest.EndDate.Value)
			{
				startDate = latest.EndDate.Value.Date.AddDays(1);
				if (endDate < startDate)
				{
					endDate = startDate.AddMonths(4).AddDays(-1);
				}
			}

			var overlap = semesters.Any(s =>
				s.StartDate.HasValue && s.EndDate.HasValue
				&& s.StartDate.Value < endDate
				&& s.EndDate.Value > startDate);

			if (overlap)
				throw new InvalidOperationException("Khoảng thời gian semester mới bị trùng với semester hiện có.");

            var nextCode = BuildCode(nextSemester, nextYear);
          var duplicate = semesters.Any(s =>
				(s.Name == nextSemester && s.Year == nextYear)
				|| string.Equals(s.Code, nextCode, StringComparison.OrdinalIgnoreCase));
			if (duplicate)
                throw new InvalidOperationException($"Semester {nextSemester} {nextYear} đã tồn tại.");

			var entity = new DataAccess.Entities.Semester
			{
                Name = nextSemester,
				Code = nextCode,
              Year = nextYear,
				StartDate = startDate,
				EndDate = endDate,
				Status = ResolveStatus(startDate, endDate, SemesterStatusEnum.Upcoming)
			};

			using var tx = await _uow.BeginTransactionAsync();
			try
			{
				await _uow.Semesters.CreateAsync(entity);
				await _uow.SaveChangesAsync();
				await tx.CommitAsync();
			}
			catch
			{
				await tx.RollbackAsync();
				throw;
			}

			return MapSemester(entity);
		}


		public async Task<bool> DeleteSemesterAsync(string semesterId)
		{
			if (string.IsNullOrWhiteSpace(semesterId))
				throw new InvalidOperationException("SemesterId không hợp lệ.");

			var semester = await _uow.Semesters.GetByIdAsync(semesterId);
			if (semester == null)
				throw new InvalidOperationException("Học kỳ không tồn tại.");

			var inUse = await _uow.Events.GetAsync(x => x.SemesterId == semesterId);
			if (inUse != null)
				throw new InvalidOperationException("Không thể xóa học kỳ đang được sử dụng bởi sự kiện.");

			semester.DeletedAt = DateTimeHelper.GetVietnamTime();
			await _uow.Semesters.UpdateAsync(semester);
			await _uow.SaveChangesAsync();
			return true;
		}

		public async Task<List<SemesterDTO>> GetAllSemestersAsync()
		{
			var semesters = await _uow.Semesters.GetAllAsync();
			return semesters
				.OrderByDescending(x => x.StartDate)
				.Select(MapSemester)
				.ToList();
		}

		public async Task<SemesterDTO?> GetSemesterByIdAsync(string semesterId)
		{
			if (string.IsNullOrWhiteSpace(semesterId))
				return null;

			var semester = await _uow.Semesters.GetByIdAsync(semesterId);
			return semester == null ? null : MapSemester(semester);
		}

		public async Task<bool> UpdateSemesterAsync(string semesterId, SemesterDTO dto)
		{
			if (string.IsNullOrWhiteSpace(semesterId))
				throw new InvalidOperationException("SemesterId không hợp lệ.");

			if (dto == null)
				throw new InvalidOperationException("Dữ liệu học kỳ không hợp lệ.");
			var now = DateTimeHelper.GetVietnamTime();
			if (now >= dto.StartDate && now <= dto.EndDate)
			{
				throw new InvalidOperationException("Không được cập nhật thời gian khi học kì đang diễn ra");
			}
			var semester = await _uow.Semesters.GetByIdAsync(semesterId);
			if (semester == null)
				throw new InvalidOperationException("Học kỳ không tồn tại.");

			if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
				throw new InvalidOperationException("Tên và mã học kỳ không được để trống.");

          if (!TryParseSemesterName(dto.Name, out var semesterName))
				throw new InvalidOperationException("Tên học kỳ không hợp lệ. Chỉ chấp nhận Spring/Summer/Fall.");

			if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
				throw new InvalidOperationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

			var semesterYear = dto.year > 0
				? dto.year
				: dto.StartDate?.Year ?? semester.Year;

			var normalizedCode = dto.Code.Trim();

         var duplicate = await _uow.Semesters.GetAsync(x => x.Id != semesterId &&
				((x.Name == semesterName && x.Year == semesterYear)
				|| (x.Code != null && x.Code == normalizedCode)));

			if (duplicate != null)
				throw new InvalidOperationException("Tên hoặc mã học kỳ đã tồn tại.");

         semester.Name = semesterName;
			semester.Code = normalizedCode;
         semester.Year = semesterYear;
			semester.StartDate = dto.StartDate;
			semester.EndDate = dto.EndDate;
			semester.Status = ResolveStatus(dto.StartDate, dto.EndDate, dto.Status);

			await _uow.Semesters.UpdateAsync(semester);
			await _uow.SaveChangesAsync();
			return true;
		}

		private static SemesterDTO MapSemester(DataAccess.Entities.Semester semester)
		{
			return new SemesterDTO
			{
              SemesterId = semester.Id,
             Name = semester.Year > 0 ? $"{semester.Name} {semester.Year}" : semester.Name.ToString(),
				Code = semester.Code,
             year = semester.Year,
				StartDate = semester.StartDate,
				EndDate = semester.EndDate,
				Status = semester.Status,
				EventList = new List<EventListDto>()
			};
		}

		private static SemesterStatusEnum ResolveStatus(DateTime? startDate, DateTime? endDate, SemesterStatusEnum fallback)
		{
			if (!startDate.HasValue || !endDate.HasValue)
				return fallback;

			var now = DateTimeHelper.GetVietnamTime().Date;
			if (now < startDate.Value.Date) return SemesterStatusEnum.Upcoming;
			if (now > endDate.Value.Date) return SemesterStatusEnum.Finished;
			return SemesterStatusEnum.Active;
		}

		private static SemesterNameEnum ResolveSemesterName(DataAccess.Entities.Semester semester)
		{
          return semester.Name;
		}

		private static int ResolveYear(DataAccess.Entities.Semester semester, int fallbackYear)
		{
          if (semester.Year > 0) return semester.Year;
			if (semester.StartDate.HasValue) return semester.StartDate.Value.Year;
			if (semester.EndDate.HasValue) return semester.EndDate.Value.Year;

          if (!string.IsNullOrWhiteSpace(semester.Code))
			{
               var digits = new string(semester.Code.Where(char.IsDigit).ToArray());
				if (digits.Length >= 4 && int.TryParse(digits[^4..], out var parsedYear))
					return parsedYear;
			}

			return fallbackYear;
		}

		private static (DateTime StartDate, DateTime EndDate) GetDefaultDateRange(SemesterNameEnum semester, int year)
		{
			return semester switch
			{
				SemesterNameEnum.Spring => (new DateTime(year, 1, 1), new DateTime(year, 4, 30, 23, 59, 59)),
				SemesterNameEnum.Summer => (new DateTime(year, 5, 1), new DateTime(year, 8, 31, 23, 59, 59)),
				_ => (new DateTime(year, 9, 1), new DateTime(year, 12, 31, 23, 59, 59))
			};
		}

		private static string BuildCode(SemesterNameEnum semester, int year)
		{
           var prefix = semester switch
			{
				SemesterNameEnum.Spring => "SP",
				SemesterNameEnum.Summer => "SU",
				_ => "FA"
			};

           var yy = (year % 100).ToString("00");
			return $"{prefix}{yy}";
		}

		private static bool TryParseSemesterName(string? rawName, out SemesterNameEnum semester)
		{
			semester = SemesterNameEnum.Spring;
			if (string.IsNullOrWhiteSpace(rawName)) return false;

			var token = rawName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
			if (string.IsNullOrWhiteSpace(token)) return false;

			return Enum.TryParse(token, true, out semester);
		}

		
	}
}
