namespace DBH.EHR.Service.Models.Enums;

/// <summary>
/// Trạng thái giao dịch blockchain
/// </summary>
public enum TxStatus
{
    PENDING,
    COMMITTED,
    FAILED
}

/// <summary>
/// Loại báo cáo EHR (
/// </summary>
public enum ReportType
{
    LAB,
    PRESCRIPTION,
    VITAL_SIGNS,
    DIAGNOSIS,
    PROCEDURE,
    IMAGING,
    IMMUNIZATION,
    ALLERGY,
    DISCHARGE_SUMMARY,
    CONSULTATION,
    OTHER
}

/// <summary>
/// Trạng thái subscription 
/// </summary>
public enum SubscriptionStatus
{
    ACTIVE,
    CANCELLED, 
    EXPIRED
}

/// <summary>
/// Hành động truy cập EHR 
/// </summary>
public enum AccessAction
{
    VIEW,
    DOWNLOAD,
    UPDATE
}

/// <summary>
/// Kết quả xác thực hash 
/// </summary>
public enum VerifyStatus
{
    PASS,
    FAIL
}
