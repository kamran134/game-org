using System.Text.RegularExpressions;
using GameOrg.Api.Common;
using GameOrg.Api.Features.Social;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Profiles;

/// <summary>Управление профилем текущего пользователя: базовые поля + UserSport.</summary>
public sealed partial class ProfileService(GameOrgDbContext db, ActivityService activityService)
{
    /// <summary>Применяет непустые поля запроса. Error — null при успехе.</summary>
    public async Task<(string? Error, bool Conflict)> UpdateMeAsync(User user, UpdateMeRequest request, CancellationToken ct)
    {
        if (request.Handle is not null && request.Handle != user.Handle)
        {
            if (!HandleFormat().IsMatch(request.Handle))
                return ("Хендл: 3-30 символов, строчные латинские буквы/цифры/_, не начинается с цифры.", false);

            var taken = await db.Users.AnyAsync(u => u.Handle == request.Handle && u.Id != user.Id && u.DeletedAt == null, ct);
            if (taken)
                return ("Этот хендл уже занят.", true);

            user.Handle = request.Handle;
        }

        if (request.DisplayName is not null)
        {
            if (request.DisplayName.ExceedsMaxLength(80))
                return ("Имя — до 80 символов на каждый язык.", false);
            var displayNameDict = request.DisplayName.ToDict();
            if (displayNameDict is null)
                return ("Имя: хотя бы один язык обязателен.", false);
            user.DisplayNameI18n = displayNameDict;
        }

        if (request.Bio is not null)
        {
            if (request.Bio.ExceedsMaxLength(500))
                return ("Bio — до 500 символов на каждый язык.", false);
            user.BioI18n = request.Bio.ToDict();
        }

        if (request.Phone is not null)
        {
            if (request.Phone.Length > 20)
                return ("Телефон — до 20 символов.", false);
            user.Phone = request.Phone.Length == 0 ? null : request.Phone;
        }

        if (request.Locale is not null)
        {
            if (!RequestLocale.Supported.Contains(request.Locale))
                return ("Locale должен быть az, ru или en.", false);
            user.Locale = request.Locale;
        }
        if (request.Timezone is not null) user.Timezone = request.Timezone;
        if (request.CityId is not null) user.CityId = request.CityId;
        if (request.ProfileVisibility is not null) user.ProfileVisibility = request.ProfileVisibility.Value;
        if (request.BirthDate is not null) user.BirthDate = request.BirthDate;
        if (request.Gender is not null) user.Gender = request.Gender;

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (null, false);
    }

    public async Task<(UserSport? Sport, string? Error)> UpsertSportAsync(
        Guid userId, Guid sportId, UpsertUserSportRequest request, CancellationToken ct)
    {
        var sportExists = await db.Sports.AnyAsync(s => s.Id == sportId && s.IsActive, ct);
        if (!sportExists)
            return (null, "Вид спорта не найден.");

        var positionIds = request.PositionIds.Distinct().ToList();
        if (positionIds.Count > 0)
        {
            var validCount = await db.SportPositions.CountAsync(p => p.SportId == sportId && positionIds.Contains(p.Id), ct);
            if (validCount != positionIds.Count)
                return (null, "Одна или несколько позиций не относятся к этому виду спорта.");
        }

        if (request.PrimaryPositionId is not null && !positionIds.Contains(request.PrimaryPositionId.Value))
            return (null, "Основная позиция должна быть среди выбранных.");

        if (request.IsPrimary)
        {
            // Отдельным SaveChanges, ДО апсерта нового — иначе partial unique
            // index user_sports_primary_uq (одна is_primary-строка на юзера)
            // словит конфликт при попытке иметь две true-строки одновременно.
            var previousPrimary = await db.UserSports
                .FirstOrDefaultAsync(us => us.UserId == userId && us.IsPrimary && us.SportId != sportId, ct);
            if (previousPrimary is not null)
            {
                previousPrimary.IsPrimary = false;
                await db.SaveChangesAsync(ct);
            }
        }

        var userSport = await db.UserSports
            .Include(us => us.Positions)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SportId == sportId, ct);

        var isNewSport = userSport is null;
        if (userSport is null)
        {
            userSport = new UserSport { UserId = userId, SportId = sportId };
            db.UserSports.Add(userSport);
        }

        userSport.Level = request.Level;
        userSport.IsPrimary = request.IsPrimary;
        userSport.PlayingSince = request.PlayingSince;
        userSport.Footedness = request.Footedness;
        userSport.HeightCm = request.HeightCm;
        userSport.JerseyNumber = request.JerseyNumber;
        userSport.Note = request.Note;
        userSport.Visibility = request.Visibility;
        userSport.UpdatedAt = DateTime.UtcNow;

        userSport.Positions.Clear();
        foreach (var positionId in positionIds)
        {
            userSport.Positions.Add(new UserSportPosition
            {
                PositionId = positionId,
                IsPrimary = positionId == request.PrimaryPositionId,
            });
        }

        await db.SaveChangesAsync(ct);

        if (isNewSport && request.Visibility == Visibility.Public)
        {
            var profileVisibility = await db.Users.Where(u => u.Id == userId).Select(u => u.ProfileVisibility).FirstAsync(ct);
            if (profileVisibility == Visibility.Public)
                await activityService.EmitAsync(userId, ActivityVerb.AddedSport, null, null, null, null, ct);
        }

        return (userSport, null);
    }

    public async Task<bool> RemoveSportAsync(Guid userId, Guid sportId, CancellationToken ct)
    {
        var userSport = await db.UserSports.FirstOrDefaultAsync(us => us.UserId == userId && us.SportId == sportId, ct);
        if (userSport is null)
            return false;

        db.UserSports.Remove(userSport);
        await db.SaveChangesAsync(ct);
        return true;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{2,29}$")]
    private static partial Regex HandleFormat();
}
