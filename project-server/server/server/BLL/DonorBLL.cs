using AutoMapper;
using System.Collections.Generic;
using WebApplication1.BLL.Interfaces;
using WebApplication1.DAL;
using WebApplication1.DAL.Interfaces;
using WebApplication1.DTOs;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System; // נחוץ עבור Exception

namespace WebApplication1.BLL
{
    public class DonorBLL : IdonorBLL
    {
        private readonly IdonorDAL donorDAL;
        private readonly IMapper _mapper;
        private readonly ILogger<DonorBLL> _logger;

        public DonorBLL(IdonorDAL donorDAL, IMapper mapper, ILogger<DonorBLL> logger)
        {
            this.donorDAL = donorDAL;
            this._mapper = mapper;
            this._logger = logger;
        }

        public async Task<List<DonorDto>> Get()
        {
            try
            {
                _logger.LogInformation("Attempting to fetch all donors.");
                var donorFromDb = await donorDAL.Get();

                if (donorFromDb == null) return new List<DonorDto>();

                _logger.LogInformation("Fetched {Count} donors from database. Mapping to DTOs.", donorFromDb.Count);
                return _mapper.Map<List<DonorDto>>(donorFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all donors");
                throw new Exception("שגיאה בשליפת רשימת התורמים.");
            }
        }

        public async Task<DonorDto> GetById(int id)
        {
            try
            {
                var donorFromDb = await donorDAL.GetById(id);
                return _mapper.Map<DonorDto>(donorFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching donor by ID: {Id}", id);
                throw new Exception("שגיאה בשליפת פרטי התורם.");
            }
        }

        public async Task Post(DonorDto donorDto)
        {
            try
            {
                _logger.LogInformation("Mapping and posting new donor: {DonorName}", donorDto.FirstName + " " + donorDto.LastName);

                // בדיקת אורך בסיסית לפני השליחה ל-DAL כדי למנוע את שגיאת ה-Truncated
                if (donorDto.FirstName?.Length > 50 || donorDto.LastName?.Length > 50)
                    throw new ArgumentException("שם התורם ארוך מדי.");

                var donorModel = _mapper.Map<DonorModel>(donorDto);
                await donorDAL.Post(donorModel);
                _logger.LogInformation("Donor posted successfully.");
            }
            catch (ArgumentException ex)
            {
                throw new Exception(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting new donor");
                throw new Exception("שגיאה בהוספת תורם חדש. וודא שכל השדות תקינים ואינם ארוכים מדי.");
            }
        }

        public async Task Update(int id, DonorDto donorDto)
        {
            try
            {
                _logger.LogInformation("Attempting to update donor with ID: {Id}", id);
                var donorModel = _mapper.Map<DonorModel>(donorDto);
                await donorDAL.Update(id, donorModel);
                _logger.LogInformation("Update operation completed for donor ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating donor with ID: {Id}", id);
                throw new Exception("עדכון פרטי התורם נכשל.");
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete donor with ID: {Id}", id);
                await donorDAL.Delete(id);
                _logger.LogInformation("Delete operation completed for donor ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting donor with ID: {Id}", id);
                throw new Exception("מחיקת התורם נכשלה.");
            }
        }

        public async Task<DonorDto> GetByEmail(string email)
        {
            try
            {
                _logger.LogInformation("Searching for donor with email: {Email}", email);
                var donorModel = await donorDAL.GetByGmail(email);

                if (donorModel == null)
                {
                    _logger.LogWarning("No donor found with email: {Email}", email);
                    return null;
                }

                return _mapper.Map<DonorDto>(donorModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for donor by email: {Email}", email);
                throw new Exception("שגיאה בחיפוש תורם לפי אימייל.");
            }
        }

        public async Task<DonorDto> GetByName(string name)
        {
            try
            {
                _logger.LogInformation("Searching for donor with name: {Name}", name);
                var donorModel = await donorDAL.GetByName(name);

                if (donorModel == null) return null;

                return _mapper.Map<DonorDto>(donorModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for donor by name: {Name}", name);
                throw new Exception("שגיאה בחיפוש תורם לפי שם.");
            }
        }

        public async Task<List<DonorDto>> GetByGift(string name)
        {
            try
            {
                _logger.LogInformation("Searching for donor associated with gift: {GiftName}", name);
                var donorModel = await donorDAL.GetByGift(name);

                if (donorModel == null) return new List<DonorDto>();

                return _mapper.Map<List<DonorDto>>(donorModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for donor by gift: {GiftName}", name);
                throw new Exception("שגיאה בשליפת תורמים לפי מתנה.");
            }
        }
    }
}