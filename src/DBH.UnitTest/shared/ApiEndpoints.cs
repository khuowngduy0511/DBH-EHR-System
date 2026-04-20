namespace DBH.UnitTest.Shared;

/// <summary>
/// Centralized list of all API endpoint URLs used across integration tests.
/// Organized by service and controller.
/// </summary>
public static class ApiEndpoints
{
    // =========================================================================
    // COMMON
    // =========================================================================
    public static class Health
    {
        public const string Check = "/health";
    }

    // =========================================================================
    // AUTH SERVICE
    // =========================================================================
    public static class Auth
    {
        public const string Register = "/api/v1/auth/register";
        public const string RegisterDoctor = "/api/v1/auth/register-doctor";
        public const string RegisterStaff = "/api/v1/auth/register-staff";
        public const string RegisterStaffDoctor = "/api/v1/auth/registerStaffDoctor";
        public const string UpdateUser = "/api/v1/auth/users/{userId}";
        public const string ChangeMyPassword = "/api/v1/auth/me/change-password";
        public const string AdminChangePassword = "/api/v1/auth/users/{userId}/change-password";
        public const string Login = "/api/v1/auth/login";
        public const string RefreshToken = "/api/v1/auth/refresh-token";
        public const string RevokeToken = "/api/v1/auth/revoke-token";
        public const string UpdateRole = "/api/v1/auth/updateRole";
        public const string Me = "/api/v1/auth/me";
        public const string UpdateProfile = "/api/v1/auth/me/profile";
        public const string Users = "/api/v1/auth/users";
        public const string UsersByContact = "/api/v1/auth/users/by-contact";
        public const string UserId = "/api/v1/auth/user-id";

        public static string UserProfile(Guid userId) => $"/api/v1/auth/users/{userId}";
        public static string DeleteUser(Guid userId) => $"/api/v1/auth/users/{userId}";
        public static string UserKeys(Guid userId) => $"/api/v1/auth/{userId}/keys";
    }

    public static class Doctors
    {
        public const string GetAll = "/api/v1/doctors";
        public const string ByMyOrganization = "/api/v1/doctors/organization/me";

        public static string ByUserIdInMyOrg(Guid userId) => $"/api/v1/doctors/organization/me/{userId}";
        public static string GetById(Guid doctorId) => $"/api/v1/doctors/{doctorId}";
        public static string Create => "/api/v1/doctors";
        public static string Update(Guid doctorId) => $"/api/v1/doctors/{doctorId}";
        public static string Delete(Guid doctorId) => $"/api/v1/doctors/{doctorId}";
    }

    public static class Patients
    {
        public const string GetAll = "/api/v1/patients";
        public const string Create = "/api/v1/patients";

        public static string GetById(Guid patientId) => $"/api/v1/patients/{patientId}";
        public static string Update(Guid patientId) => $"/api/v1/patients/{patientId}";
        public static string Delete(Guid patientId) => $"/api/v1/patients/{patientId}";
    }

    public static class Staff
    {
        public const string GetAll = "/api/v1/staff";
        public const string Create = "/api/v1/staff";

        public static string GetById(Guid staffId) => $"/api/v1/staff/{staffId}";
        public static string Update(Guid staffId) => $"/api/v1/staff/{staffId}";
        public static string Delete(Guid staffId) => $"/api/v1/staff/{staffId}";
    }

    // =========================================================================
    // APPOINTMENT SERVICE
    // =========================================================================
    public static class Appointments
    {
        public const string Create = "/api/v1/appointments";
        public const string GetAll = "/api/v1/appointments";
        public const string SearchDoctors = "/api/v1/appointments/doctors/search";

        public static string GetById(Guid id) => $"/api/v1/appointments/{id}";
        public static string UpdateStatus(Guid id, string status) => $"/api/v1/appointments/{id}/status?status={status}";
        public static string Reschedule(Guid id, string newDate) => $"/api/v1/appointments/{id}/reschedule?newDate={newDate}";
        public static string Confirm(Guid id) => $"/api/v1/appointments/{id}/confirm";
        public static string Reject(Guid id) => $"/api/v1/appointments/{id}/reject";
        public static string Cancel(Guid id) => $"/api/v1/appointments/{id}/cancel";
        public static string CheckIn(Guid id) => $"/api/v1/appointments/{id}/check-in";
        public static string PatientsByDoctor(Guid doctorId) => $"/api/v1/appointments/doctors/{doctorId}/patients";
    }

    public static class Encounters
    {
        public const string Create = "/api/v1/encounters";

        public static string GetById(Guid id) => $"/api/v1/encounters/{id}";
        public static string ByAppointment(Guid appointmentId) => $"/api/v1/encounters/by-appointment/{appointmentId}";
        public static string ByPatient(Guid patientId) => $"/api/v1/encounters/by-patient/{patientId}";
        public static string Update(Guid id) => $"/api/v1/encounters/{id}";
        public static string Complete(Guid id) => $"/api/v1/encounters/{id}/complete";
    }

    // =========================================================================
    // AUDIT SERVICE
    // =========================================================================
    public static class Audit
    {
        public const string Create = "/api/v1/audit";
        public const string Search = "/api/v1/audit/search";
        public const string Stats = "/api/v1/audit/stats";

        public static string GetById(Guid id) => $"/api/v1/audit/{id}";
        public static string ByPatient(Guid patientId) => $"/api/v1/audit/by-patient/{patientId}";
        public static string ByActor(Guid actorUserId) => $"/api/v1/audit/by-actor/{actorUserId}";
        public static string ByTarget(Guid targetId) => $"/api/v1/audit/by-target/{targetId}";
        public static string SyncFromBlockchain(string blockchainAuditId) => $"/api/v1/audit/sync/{blockchainAuditId}";
    }

    // =========================================================================
    // CONSENT SERVICE
    // =========================================================================
    public static class Consents
    {
        public const string Grant = "/api/v1/consents";
        public const string Search = "/api/v1/consents/search";
        public const string Verify = "/api/v1/consents/verify";

        public static string GetById(Guid id) => $"/api/v1/consents/{id}";
        public static string ByPatient(Guid patientId) => $"/api/v1/consents/by-patient/{patientId}";
        public static string ByGrantee(Guid granteeId) => $"/api/v1/consents/by-grantee/{granteeId}";
        public static string Revoke(Guid id) => $"/api/v1/consents/{id}/revoke";
        public static string SyncFromBlockchain(string blockchainConsentId) => $"/api/v1/consents/sync/{blockchainConsentId}";
    }

    public static class AccessRequests
    {
        public const string Create = "/api/v1/access-requests";

        public static string GetById(Guid id) => $"/api/v1/access-requests/{id}";
        public static string ByPatient(Guid patientId) => $"/api/v1/access-requests/by-patient/{patientId}";
        public static string ByRequester(Guid requesterId) => $"/api/v1/access-requests/by-requester/{requesterId}";
        public static string Respond(Guid id) => $"/api/v1/access-requests/{id}/respond";
        public static string Cancel(Guid id) => $"/api/v1/access-requests/{id}";
    }

    // =========================================================================
    // EHR SERVICE
    // =========================================================================
    public static class Ehr
    {
        public const string CreateRecord = "/api/v1/ehr/records";
        public const string EncryptToIpfs = "/api/v1/ehr/ipfs/encrypt";
        public const string DecryptFromIpfs = "/api/v1/ehr/ipfs/decrypt";

        public static string GetRecord(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}";
        public static string UpdateRecord(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}";
        public static string GetDocument(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}/document";
        public static string GetDocumentSelf(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}/document/self";
        public static string PatientRecords(Guid patientId) => $"/api/v1/ehr/records/patient/{patientId}";
        public static string OrgRecords(Guid orgId) => $"/api/v1/ehr/records/org/{orgId}";
        public static string Versions(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}/versions";
        public static string VersionById(Guid ehrId, Guid versionId) => $"/api/v1/ehr/records/{ehrId}/versions/{versionId}";
        public static string Files(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}/files";
        public static string AddFile(Guid ehrId) => $"/api/v1/ehr/records/{ehrId}/files";
        public static string DeleteFile(Guid ehrId, Guid fileId) => $"/api/v1/ehr/records/{ehrId}/files/{fileId}";
    }

    // =========================================================================
    // NOTIFICATION SERVICE
    // =========================================================================
    public static class Notifications
    {
        public const string Send = "/api/v1/notifications";
        public const string Broadcast = "/api/v1/notifications/broadcast";

        public static string ByUser(string userDid) => $"/api/v1/notifications/by-user/{userDid}";
        public static string Unread(string userDid) => $"/api/v1/notifications/by-user/{userDid}/unread";
        public static string UnreadCount(string userDid) => $"/api/v1/notifications/by-user/{userDid}/unread-count";
        public static string MarkRead(string userDid) => $"/api/v1/notifications/by-user/{userDid}/mark-read";
        public static string MarkAllRead(string userDid) => $"/api/v1/notifications/by-user/{userDid}/mark-all-read";
        public static string Delete(Guid id) => $"/api/v1/notifications/{id}";
    }

    public static class DeviceTokens
    {
        public const string Register = "/api/v1/notifications/device-tokens";

        public static string ByUser(string userDid) => $"/api/v1/notifications/device-tokens/by-user/{userDid}";
        public static string Deactivate(Guid id) => $"/api/v1/notifications/device-tokens/{id}";
        public static string DeactivateAll(string userDid) => $"/api/v1/notifications/device-tokens/by-user/{userDid}/all";
    }

    public static class Preferences
    {
        public static string Get(string userDid) => $"/api/v1/notifications/preferences/by-user/{userDid}";
        public static string Update(string userDid) => $"/api/v1/notifications/preferences/by-user/{userDid}";
    }

    // =========================================================================
    // ORGANIZATION SERVICE
    // =========================================================================
    public static class Organizations
    {
        public const string Create = "/api/v1/organizations";
        public const string GetAll = "/api/v1/organizations";

        public static string GetById(Guid id) => $"/api/v1/organizations/{id}";
        public static string Update(Guid id) => $"/api/v1/organizations/{id}";
        public static string Delete(Guid id) => $"/api/v1/organizations/{id}";
        public static string Verify(Guid id, Guid verifiedByUserId) => $"/api/v1/organizations/{id}/verify?verifiedByUserId={verifiedByUserId}";
    }

    public static class Departments
    {
        public const string Create = "/api/v1/departments";

        public static string GetById(Guid id) => $"/api/v1/departments/{id}";
        public static string ByOrganization(Guid orgId) => $"/api/v1/departments/by-organization/{orgId}";
        public static string Update(Guid id) => $"/api/v1/departments/{id}";
        public static string Delete(Guid id) => $"/api/v1/departments/{id}";
    }

    public static class Memberships
    {
        public const string Create = "/api/v1/memberships";
        public const string SearchDoctors = "/api/v1/memberships/doctors/search";

        public static string GetById(Guid id) => $"/api/v1/memberships/{id}";
        public static string ByOrganization(Guid orgId) => $"/api/v1/memberships/by-organization/{orgId}";
        public static string ByUser(Guid userId) => $"/api/v1/memberships/by-user/{userId}";
        public static string Update(Guid id) => $"/api/v1/memberships/{id}";
        public static string Delete(Guid id) => $"/api/v1/memberships/{id}";
    }

    public static class PaymentConfig
    {
        public static string Configure(Guid orgId) => $"/api/v1/organizations/{orgId}/payment-config";
        public static string Update(Guid orgId) => $"/api/v1/organizations/{orgId}/payment-config";
        public static string GetStatus(Guid orgId) => $"/api/v1/organizations/{orgId}/payment-config";
    }

    public static class Internal
    {
        public static string GetPaymentKeys(Guid orgId) => $"/api/v1/internal/organizations/{orgId}/payment-keys";
    }

    // =========================================================================
    // PAYMENT SERVICE
    // =========================================================================
    public static class Invoices
    {
        public const string Create = "/api/v1/invoices";

        public static string GetById(Guid invoiceId) => $"/api/v1/invoices/{invoiceId}";
        public static string ByPatient(Guid patientId) => $"/api/v1/invoices/patient/{patientId}";
        public static string ByOrg(Guid orgId) => $"/api/v1/invoices/org/{orgId}";
        public static string Cancel(Guid invoiceId) => $"/api/v1/invoices/{invoiceId}/cancel";
        public static string Checkout(Guid invoiceId) => $"/api/v1/invoices/{invoiceId}/checkout";
        public static string PayCash(Guid invoiceId) => $"/api/v1/invoices/{invoiceId}/pay-cash";
    }

    public static class Payments
    {
        public const string Webhook = "/api/v1/payments/webhook";

        public static string GetById(Guid paymentId) => $"/api/v1/payments/{paymentId}";
        public static string Verify(Guid paymentId) => $"/api/v1/payments/{paymentId}/verify";
    }
}
