using AutoMapper;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using server.DAL;
using server.DAL.Interfaces;
using server.DTO;
using System.Net;
using MailKit.Net.Smtp;
using WebApplication1.BLL;
using WebApplication1.BLL.Interfaces;
using WebApplication1.DAL;
using WebApplication1.DAL.Interfaces;
using WebApplication1.DTOs;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace WebApplication1.BLL
{
    public class GiftBLL : IgiftBLL
    {
        private readonly IgiftDAL giftDAL;
        private readonly IMapper _mapper;
        private readonly ICustomerDatailsDAL _customerDetailsDAL;
        private readonly ILogger<GiftBLL> _logger;

        public GiftBLL(IgiftDAL giftDAL, IMapper mapper, ICustomerDatailsDAL customerDetailsDAL, ILogger<GiftBLL> logger)
        {
            this.giftDAL = giftDAL;
            this._mapper = mapper;
            this._customerDetailsDAL = customerDetailsDAL;
            this._logger = logger;
        }

        public async Task<List<GiftDto>> Get()
        {
            try
            {
                var giftFromDb = await giftDAL.Get();
                if (giftFromDb == null) return new List<GiftDto>();

                var giftDtos = _mapper.Map<List<GiftDto>>(giftFromDb);

                for (int i = 0; i < giftFromDb.Count; i++)
                {
                    if (giftFromDb[i].Winner != null)
                    {
                        giftDtos[i].CustomerName = giftFromDb[i].Winner.FirstName + " " + giftFromDb[i].Winner.LastName;
                    }
                    else
                    {
                        giftDtos[i].CustomerName = "אין זוכה עדיין";
                    }
                }
                return giftDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.Get");
                throw new Exception("שגיאה בטעינת רשימת המתנות.");
            }
        }

        public async Task<GiftDto> GetById(int id)
        {
            try
            {
                var giftFromDb = await giftDAL.GetById(id);
                return _mapper.Map<GiftDto>(giftFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.GetById: {Id}", id);
                throw new Exception("שגיאה בשליפת פרטי המתנה.");
            }
        }

        public async Task Post(GiftDto gift)
        {
            try
            {
                _logger.LogInformation("Adding a new gift: {GiftName}", gift.Name);

                // ולידציה בסיסית למניעת שגיאת Truncated
                if (gift.Name?.Length > 100) throw new ArgumentException("שם המתנה ארוך מדי (מקסימום 100 תווים)");

                var giftModel = _mapper.Map<GiftModel>(gift);

                if (gift.DonorId != 0)
                {
                    giftModel.DonorId = gift.DonorId;
                    giftModel.Donor = null;
                }

                giftModel.WinnerId = null;
                giftModel.Winner = null;

                await giftDAL.Post(giftModel);
                _logger.LogInformation("Gift {GiftName} added successfully.", gift.Name);
            }
            catch (ArgumentException ex) { throw new Exception(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.Post");
                throw new Exception("הוספת המתנה נכשלה. וודא שהנתונים תקינים.");
            }
        }

        public async Task Update(int id, GiftDto gift)
        {
            try
            {
                _logger.LogInformation("Updating gift with ID: {Id}", id);
                var giftModel = _mapper.Map<GiftModel>(gift);
                await giftDAL.Update(id, giftModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.Update: {Id}", id);
                throw new Exception("עדכון המתנה נכשל.");
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting gift with ID: {Id}", id);
                await giftDAL.Delete(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.Delete: {Id}", id);
                throw new Exception("מחיקת המתנה נכשלה. ייתכן שיש רכישות הקשורות למתנה זו.");
            }
        }

        public async Task<GiftDto> GetByName(string name)
        {
            try
            {
                _logger.LogInformation("Searching for gift by name: {Name}", name);
                var giftModel = await giftDAL.GetByName(name);
                return giftModel == null ? null : _mapper.Map<GiftDto>(giftModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.GetByName");
                throw new Exception("שגיאה בחיפוש לפי שם.");
            }
        }

        public async Task<GiftDto> GetByCategory(string category)
        {
            try
            {
                _logger.LogInformation("Searching for gifts in category: {Category}", category);
                var giftModel = await giftDAL.GetByCategory(category);
                return giftModel == null ? null : _mapper.Map<GiftDto>(giftModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.GetByCategory");
                throw new Exception("שגיאה בחיפוש לפי קטגוריה.");
            }
        }

        public async Task<List<GiftDto>> GetByDonor(string name)
        {
            try
            {
                _logger.LogInformation("Searching for gifts by donor: {DonorName}", name);
                var giftModels = await giftDAL.GetByDonor(name);
                if (giftModels == null) return new List<GiftDto>();
                return _mapper.Map<List<GiftDto>>(giftModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GiftBLL.GetByDonor");
                throw new Exception("שגיאה בשליפת מתנות לפי תורם.");
            }
        }

        public async Task<List<GiftDto>> GetByPurchasesCount(int num)
        {
            try
            {
                _logger.LogInformation("Searching for gifts with purchase count: {Num}", num);
                var giftModels = await giftDAL.GetByPurchasesCount(num);
                if (giftModels == null) return new List<GiftDto>();

                return giftModels.Select(p => new GiftDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PriceCard = p.PriceCard,
                    DonorName = p.Donor != null ? $"{p.Donor.FirstName} {p.Donor.LastName}" : "ללא תורם",
                    Category = p.Category
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByPurchasesCount");
                throw new Exception("שגיאה בסינון לפי כמות רכישות.");
            }
        }

        public async Task<List<CustomerDetailsDto>> GetByParches(int giftId)
        {
            try
            {
                var purchases = await _customerDetailsDAL.GetByGiftId(giftId);
                return purchases.Select(p => new CustomerDetailsDto
                {
                    CustomerName = p.CustomerName,
                    Quntity = p.Quntity,
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByParches for GiftId: {Id}", giftId);
                throw new Exception("שגיאה בשליפת רשימת רוכשים.");
            }
        }

        public async Task<List<GiftDto>> giftExpensive()
        {
            try
            {
                var giftsList = await giftDAL.giftExpensive();
                return giftsList == null ? new List<GiftDto>() : _mapper.Map<List<GiftDto>>(giftsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in giftExpensive");
                throw new Exception("שגיאה במיון המתנות היקרות.");
            }
        }

        public async Task<GiftDto> byMostParches()
        {
            try
            {
                var giftModel = await giftDAL.byMostParches();
                return giftModel == null ? null : _mapper.Map<GiftDto>(giftModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in byMostParches");
                throw new Exception("שגיאה בשליפת המתנה הפופולרית ביותר.");
            }
        }

        public async Task<WinnerDTO> Winner(int giftId)
        {
            try
            {
                _logger.LogInformation("Starting lottery process for gift ID: {GiftId}", giftId);
                var gift = await giftDAL.Winner(giftId);

                var confirmedPurchases = gift?.customerDatails?
                    .Where(d => d.Status == "Confirmed")
                    .ToList();

                if (gift == null || confirmedPurchases == null || !confirmedPurchases.Any() || gift.WinnerId != null)
                {
                    _logger.LogWarning("Lottery failed or already done for gift ID {GiftId}.", giftId);
                    return null;
                }

                var random = new Random();
                var winningPurchase = confirmedPurchases[random.Next(confirmedPurchases.Count)];
                var winner = winningPurchase.customer;

                _logger.LogInformation("Winner selected: {WinnerName} for gift {GiftName}", winner.FirstName, gift.Name);

                gift.WinnerId = winner.Id;
                gift.Winner = null;

                await giftDAL.Update(gift.Id, gift);
                gift.Winner = winner;

                // לוגיקת אימייל - עטופה בנפרד כדי שהגרלה לא תיכשל אם המייל נכשל
                try
                {
                    _logger.LogInformation("Attempting to send winner email to: {Email}", winner.Email);
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("מערכת הגרלות", "talyatoledano10@gmail.com"));
                    message.To.Add(new MailboxAddress(winner.FirstName, winner.Email));
                    message.Subject = "מזל טוב! זכית בפרס";

                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = $@"<div dir='rtl'><h2>שלום {winner.FirstName}!</h2><p>זכית בפרס <b>{gift.Name}</b>!</p></div>"
                    };
                    message.Body = bodyBuilder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                        await client.AuthenticateAsync("talyatoledano10@gmail.com", "xcba vdpp dmaa dvls");
                        await client.SendAsync(message);
                        await client.DisconnectAsync(true);
                    }
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Email sending failed, but winner was saved.");
                }

                return _mapper.Map<WinnerDTO>(gift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Lottery (Winner method) for GiftId: {Id}", giftId);
                throw new Exception("ביצוע ההגרלה נכשל.");
            }
        }

        public async Task<List<GiftDto>> reportWinners()
        {
            try
            {
                var giftFromDb = await giftDAL.reportWinners();
                return _mapper.Map<List<GiftDto>>(giftFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reportWinners");
                throw new Exception("שגיאה בהפקת דו\"ח זוכים.");
            }
        }

        Task<List<CustomerDetailsDto>> IgiftBLL.GetByCategory(string category)
        {
            throw new NotImplementedException();
        }

        public async Task<int> reportAchnasot()
        {
            try
            {
                return await this.giftDAL.reportAchnasot();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reportAchnasot");
                throw new Exception("שגיאה בהפקת דו\"ח הכנסות.");
            }
        }
    }
}