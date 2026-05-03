using System.Security.Claims;
using System.Text.Json;
using DBH.EHR.Service.DbContext;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Models.Enums;
using DBH.Shared.Infrastructure.Notification;
using DBH.Shared.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Services;

public class LabOrderService : ILabOrderService
{
    private readonly EhrPrimaryDbContext _db;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationServiceClient? _notificationClient;
    private readonly ILogger<LabOrderService> _logger;

    public LabOrderService(
        EhrPrimaryDbContext db,
        IAuthServiceClient authServiceClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LabOrderService> logger,
        INotificationServiceClient? notificationClient = null)
    {
        _db = db;
        _authServiceClient = authServiceClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _notificationClient = notificationClient;
    }

    // ─────────────────────────────────────────────────────
    //  Create
    // ─────────────────────────────────────────────────────

    public async Task<LabOrderResponseDto> CreateAsync(CreateLabOrderDto dto, Guid doctorUserId)
    {
        // Validate EHR tồn tại
        var ehrExists = await _db.EhrRecords.AnyAsync(e => e.EhrId == dto.EhrId);
        if (!ehrExists)
            throw new KeyNotFoundException($"EHR {dto.EhrId} không tồn tại");

        var order = new LabOrder
        {
            EhrId = dto.EhrId,
            PatientId = dto.PatientId,
            RequestedBy = doctorUserId,
            OrgId = dto.OrgId,
            TestType = dto.TestType,
            ClinicalNote = dto.ClinicalNote,
            Status = LabOrderStatus.PENDING,
            RequestedAt = VietnamTime.DatabaseNow
        };

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Lab order {LabOrderId} tạo bởi Doctor {DoctorId} cho EHR {EhrId}, loại: {TestType}",
            order.LabOrderId, doctorUserId, dto.EhrId, dto.TestType);

        // Notify bệnh nhân
        if (_notificationClient != null)
        {
            await TrySendNotificationAsync(
                dto.PatientId,
                "Chỉ định xét nghiệm mới",
                $"Bác sĩ đã chỉ định xét nghiệm: {dto.TestType}.",
                "LabOrderCreated",
                order.LabOrderId.ToString());
        }

        return await MapToResponseAsync(order);
    }

    // ─────────────────────────────────────────────────────
    //  Read
    // ─────────────────────────────────────────────────────

    public async Task<LabOrderResponseDto?> GetByIdAsync(Guid labOrderId)
    {
        var order = await _db.LabOrders.FindAsync(labOrderId);
        if (order == null) return null;
        return await MapToResponseAsync(order);
    }

    public async Task<IEnumerable<LabOrderResponseDto>> GetByOrgAsync(Guid orgId, LabOrderStatus? status = null)
    {
        var query = _db.LabOrders.Where(o => o.OrgId == orgId);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var orders = await query.OrderByDescending(o => o.RequestedAt).ToListAsync();
        return await MapToResponseListAsync(orders);
    }

    public async Task<IEnumerable<LabOrderResponseDto>> GetByEhrAsync(Guid ehrId)
    {
        var orders = await _db.LabOrders
            .Where(o => o.EhrId == ehrId)
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync();
        return await MapToResponseListAsync(orders);
    }

    public async Task<IEnumerable<LabOrderResponseDto>> GetByPatientAsync(Guid patientId)
    {
        var orders = await _db.LabOrders
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync();
        return await MapToResponseListAsync(orders);
    }

    // ─────────────────────────────────────────────────────
    //  Update Status
    // ─────────────────────────────────────────────────────

    public async Task<LabOrderResponseDto?> UpdateStatusAsync(Guid labOrderId, Guid labTechUserId, LabOrderStatus newStatus)
    {
        var order = await _db.LabOrders.FindAsync(labOrderId);
        if (order == null) return null;

        // Không cho phép cập nhật đơn đã hủy hoặc hoàn thành (dùng SubmitResult cho COMPLETED)
        if (order.Status == LabOrderStatus.CANCELLED)
        {
            _logger.LogWarning("Lab order {LabOrderId} đã bị hủy, không thể cập nhật trạng thái", labOrderId);
            return null;
        }

        order.Status = newStatus;
        order.AssignedTo = labTechUserId;

        if (newStatus == LabOrderStatus.RECEIVED)
            order.ReceivedAt = VietnamTime.DatabaseNow;
        else if (newStatus == LabOrderStatus.IN_PROGRESS && order.ReceivedAt == null)
            order.ReceivedAt = VietnamTime.DatabaseNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Lab order {LabOrderId} trạng thái → {Status} bởi LabTech {UserId}", labOrderId, newStatus, labTechUserId);

        return await MapToResponseAsync(order);
    }

    // ─────────────────────────────────────────────────────
    //  Submit Result
    // ─────────────────────────────────────────────────────

    public async Task<LabOrderResponseDto?> SubmitResultAsync(Guid labOrderId, Guid labTechUserId, SubmitLabResultDto dto)
    {
        var order = await _db.LabOrders.FindAsync(labOrderId);
        if (order == null) return null;

        if (order.Status == LabOrderStatus.CANCELLED || order.Status == LabOrderStatus.COMPLETED)
        {
            _logger.LogWarning("Lab order {LabOrderId} ở trạng thái {Status}, không thể nhập kết quả", labOrderId, order.Status);
            return null;
        }

        order.AssignedTo = labTechUserId;
        order.Status = LabOrderStatus.COMPLETED;
        order.ResultNote = dto.ResultNote;
        order.CompletedAt = VietnamTime.DatabaseNow;

        if (order.ReceivedAt == null)
            order.ReceivedAt = VietnamTime.DatabaseNow;

        // Serialize result items to JSON
        if (dto.ResultItems != null && dto.ResultItems.Count > 0)
        {
            order.ResultValuesJson = JsonSerializer.Serialize(dto.ResultItems, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Lab order {LabOrderId} hoàn thành bởi LabTech {UserId}, {ItemCount} chỉ số",
            labOrderId, labTechUserId, dto.ResultItems?.Count ?? 0);

        // Notify Doctor khi có kết quả
        if (_notificationClient != null)
        {
            await TrySendNotificationAsync(
                order.RequestedBy,
                "Kết quả xét nghiệm đã sẵn sàng",
                $"Kết quả xét nghiệm '{order.TestType}' đã được cập nhật.",
                "LabOrderCompleted",
                order.LabOrderId.ToString());

            // Notify Patient
            await TrySendNotificationAsync(
                order.PatientId,
                "Kết quả xét nghiệm",
                $"Kết quả xét nghiệm '{order.TestType}' của bạn đã có.",
                "LabOrderCompleted",
                order.LabOrderId.ToString());
        }

        return await MapToResponseAsync(order);
    }

    // ─────────────────────────────────────────────────────
    //  Cancel
    // ─────────────────────────────────────────────────────

    public async Task<bool> CancelAsync(Guid labOrderId, Guid requesterId)
    {
        var order = await _db.LabOrders.FindAsync(labOrderId);
        if (order == null) return false;

        if (order.Status != LabOrderStatus.PENDING)
        {
            _logger.LogWarning("Không thể hủy lab order {LabOrderId} ở trạng thái {Status}", labOrderId, order.Status);
            return false;
        }

        order.Status = LabOrderStatus.CANCELLED;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Lab order {LabOrderId} đã bị hủy bởi {RequesterId}", labOrderId, requesterId);
        return true;
    }

    // ─────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────

    private async Task<LabOrderResponseDto> MapToResponseAsync(LabOrder order)
    {
        var bearerToken = GetBearerToken();

        var dto = new LabOrderResponseDto
        {
            LabOrderId = order.LabOrderId,
            EhrId = order.EhrId,
            PatientId = order.PatientId,
            RequestedBy = order.RequestedBy,
            AssignedTo = order.AssignedTo,
            OrgId = order.OrgId,
            TestType = order.TestType,
            ClinicalNote = order.ClinicalNote,
            Status = order.Status.ToString(),
            ResultNote = order.ResultNote,
            RequestedAt = order.RequestedAt,
            ReceivedAt = order.ReceivedAt,
            CompletedAt = order.CompletedAt
        };

        // Deserialize result items
        if (!string.IsNullOrEmpty(order.ResultValuesJson))
        {
            try
            {
                dto.ResultItems = JsonSerializer.Deserialize<List<LabResultItemDto>>(
                    order.ResultValuesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot deserialize ResultValuesJson for LabOrder {LabOrderId}", order.LabOrderId);
            }
        }

        // Enrich profiles if bearer token available
        if (!string.IsNullOrEmpty(bearerToken))
        {
            dto.PatientProfile = await TryGetProfileByPatientIdAsync(order.PatientId, bearerToken);
            dto.DoctorProfile = await TryGetProfileByUserIdAsync(order.RequestedBy, bearerToken);
            if (order.AssignedTo.HasValue)
                dto.LabTechProfile = await TryGetProfileByUserIdAsync(order.AssignedTo.Value, bearerToken);
        }

        return dto;
    }

    private async Task<List<LabOrderResponseDto>> MapToResponseListAsync(List<LabOrder> orders)
    {
        var tasks = orders.Select(o => MapToResponseAsync(o));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private async Task<AuthUserProfileDetailDto?> TryGetProfileByPatientIdAsync(Guid patientId, string bearerToken)
    {
        try
        {
            var userId = await _authServiceClient.GetUserIdByPatientIdAsync(patientId, bearerToken);
            if (!userId.HasValue) return null;
            return await _authServiceClient.GetUserProfileDetailAsync(userId.Value, bearerToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get patient profile for {PatientId}", patientId);
            return null;
        }
    }

    private async Task<AuthUserProfileDetailDto?> TryGetProfileByUserIdAsync(Guid userId, string bearerToken)
    {
        try
        {
            return await _authServiceClient.GetUserProfileDetailAsync(userId, bearerToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user profile for {UserId}", userId);
            return null;
        }
    }

    private string? GetBearerToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        return authHeader["Bearer ".Length..].Trim();
    }

    private Guid? GetCurrentUserId()
    {
        var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
               ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("userId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private async Task TrySendNotificationAsync(
        Guid recipientId,
        string title,
        string body,
        string type,
        string referenceId)
    {
        try
        {
            if (_notificationClient != null)
            {
                await _notificationClient.SendAsync(
                    recipientId, title, body, type, "Normal", referenceId, "LabOrder");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification to {RecipientId} for LabOrder {ReferenceId}", recipientId, referenceId);
        }
    }
}
