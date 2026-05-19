using System;
using System.Collections.Generic;
using System.Linq;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Помощник для расчёта загруженности отеля
/// </summary>
public static class OccupancyHelper
{
    /// <summary>
    /// Рассчитывает загруженность по дням в процентах
    /// </summary>
    public static async Task<Dictionary<DateTime, double>> CalculateOccupancyByDayAsync(
        IRoomService roomService,
        IBookingService bookingService,
        DateTime startDate,
        DateTime endDate)
    {
        var rooms = await roomService.GetAllRoomsAsync();
        var totalRooms = rooms.Count();
        
        if (totalRooms == 0)
            return new Dictionary<DateTime, double>();
        
        // Получаем все бронирования за период (включая завершённые)
        var bookings = await bookingService.GetBookingsByDateRangeAsync(
            startDate.AddDays(-30), 
            endDate.AddDays(30));
        
        var bookingsList = bookings.ToList();
        var result = new Dictionary<DateTime, double>();
        
        var currentDate = startDate.Date;
        var endDateDate = endDate.Date;
        
        while (currentDate <= endDateDate)
        {
            // Считаем номера, которые были заняты в этот день
            // Номер занят в currentDate, если CheckInDate <= currentDate < CheckOutDate
            // (гость заселяется в CheckInDate и выселяется в CheckOutDate)
            var bookedRooms = bookingsList.Count(b =>
                // Учитываем активные и завершённые бронирования
                (b.Status == BookingStatus.Подтверждено || 
                 b.Status == BookingStatus.Подтверждено ||
                 b.Status == BookingStatus.Заселён ||
                 b.Status == BookingStatus.Завершено) &&
                // Проверяем, пересекается ли бронирование с текущим днём
                b.CheckInDate.Date <= currentDate &&
                b.CheckOutDate.Date > currentDate);
            
            var occupancyPercent = (double)bookedRooms / totalRooms * 100;
            result[currentDate] = Math.Min(occupancyPercent, 100.0);
            
            currentDate = currentDate.AddDays(1);
        }
        
        return result;
    }

    /// <summary>
    /// Рассчитывает общую загруженность за период в процентах
    /// </summary>
    public static double CalculateTotalOccupancy(
        IEnumerable<Booking> bookings,
        int totalRooms,
        DateTime startDate,
        DateTime endDate)
    {
        if (totalRooms == 0)
            return 0;
        
        var bookingsList = bookings.ToList();
        var totalDays = (endDate - startDate).Days * totalRooms;
        
        var bookedDays = bookingsList
            .Where(b => b.Status != BookingStatus.Отменено)
            .SelectMany(b => Enumerable.Range(0, (b.CheckOutDate - b.CheckInDate).Days)
                .Select(d => b.CheckInDate.AddDays(d)))
            .Distinct()
            .Count();
        
        return totalDays > 0 ? (double)bookedDays / totalDays * 100 : 0;
    }
}