using System;
using System.Collections.Generic;
using System.Linq;
using HotelSystem.Models.Entities;

namespace HotelSystem.Helpers;

/// <summary>
/// Вспомогательный класс для поиска и фильтрации данных
/// </summary>
public static class SearchHelper
{
    /// <summary>
    /// Фильтрует клиентов по поисковому запросу (по приоритету: ID, ФИО, паспорт, телефон, почта)
    /// </summary>
    public static IEnumerable<Client> FilterClients(IEnumerable<Client> clients, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return clients;
        
        var query = searchQuery.Trim().ToLower();
        
        return clients.Where(c =>
            c.Id.ToString().Contains(query) ||
            c.FullName.ToLower().Contains(query) ||
            (c.Passport != null && c.Passport.ToLower().Contains(query)) ||
            (c.Phone != null && c.Phone.ToLower().Contains(query)) ||
            (c.Email != null && c.Email.ToLower().Contains(query))
        );
    }
    
    /// <summary>
    /// Сортирует клиентов по приоритету совпадения
    /// </summary>
    public static IEnumerable<Client> SortClientsByPriority(IEnumerable<Client> clients, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return clients;
        
        var query = searchQuery.Trim().ToLower();
        
        return clients.OrderBy(c =>
        {
            if (c.Id.ToString().Contains(query)) return 0;
            if (c.FullName.ToLower().Contains(query)) return 1;
            if (c.Passport != null && c.Passport.ToLower().Contains(query)) return 2;
            if (c.Phone != null && c.Phone.ToLower().Contains(query)) return 3;
            if (c.Email != null && c.Email.ToLower().Contains(query)) return 4;
            return 5;
        });
    }
    
    /// <summary>
    /// Фильтрует бронирования по параметрам
    /// </summary>
    public static IEnumerable<Booking> FilterBookings(
        IEnumerable<Booking> bookings, 
        string? searchQuery, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        int? clientId = null)
    {
        var query = bookings.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(b =>
                b.Id.ToString().Contains(search) ||
                (b.Client != null && b.Client.FullName.ToLower().Contains(search)) ||
                (b.Client != null && b.Client.Phone != null && b.Client.Phone.ToLower().Contains(search)) ||
                b.Room.Name.ToLower().Contains(search)
            );
        }
        
        if (startDate.HasValue)
            query = query.Where(b => b.CheckInDate >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(b => b.CheckOutDate <= endDate.Value.AddDays(1));
        
        if (clientId.HasValue)
            query = query.Where(b => b.ClientId == clientId.Value);
        
        return query;
    }
    
    /// <summary>
    /// Фильтрует оплаты услуг по параметрам
    /// </summary>
    public static IEnumerable<Models.Entities.ServicePaymentDisplay> FilterServicePayments(
        IEnumerable<Models.Entities.ServicePaymentDisplay> payments,
        string? searchQuery,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? clientId = null,
        int? serviceId = null)
    {
        var query = payments.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(p =>
                p.Id.ToString().Contains(search) ||
                p.ClientName.ToLower().Contains(search) ||
                p.ServiceName.ToLower().Contains(search) ||
                p.TransactionDate.ToString("dd.MM.yyyy").Contains(search)
            );
        }
        
        if (startDate.HasValue)
            query = query.Where(p => p.TransactionDate >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(p => p.TransactionDate <= endDate.Value.AddDays(1));
        
        if (clientId.HasValue)
            query = query.Where(p => p.ClientName.Contains(clientId.ToString()));
        
        if (serviceId.HasValue)
            query = query.Where(p => p.ServiceName.Contains(serviceId.ToString()));
        
        return query;
    }
    
    /// <summary>
    /// Фильтрует услуги по поисковому запросу
    /// </summary>
    public static IEnumerable<Service> FilterServices(
        IEnumerable<Service> services,
        string? searchQuery,
        bool? isActive = null)
    {
        var query = services.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(s =>
                s.Id.ToString().Contains(search) ||
                s.Name.ToLower().Contains(search) ||
                (s.Description != null && s.Description.ToLower().Contains(search))
            );
        }
        
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);
        
        return query;
    }
    
    /// <summary>
    /// Фильтрует номера по параметрам
    /// </summary>
    public static IEnumerable<Room> FilterRooms(
        IEnumerable<Room> rooms,
        string? searchQuery,
        RoomType? type = null,
        RoomStatus? status = null)
    {
        var query = rooms.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(r =>
                r.Id.ToString().Contains(search) ||
                r.Name.ToLower().Contains(search) ||
                r.Type.ToString().ToLower().Contains(search)
            );
        }
        
        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);
        
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        
        return query;
    }
}
