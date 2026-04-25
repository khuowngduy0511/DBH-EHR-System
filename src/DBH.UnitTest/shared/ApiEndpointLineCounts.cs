using System;

namespace DBH.UnitTest.Shared;

/// <summary>
/// Line counts for the controller action behind each API endpoint in ApiEndpoints.
/// </summary>
public static class Auth
{
    public const int Register = 8;
    public const int RegisterDoctor = 9;
    public const int RegisterStaff = 9;
    public const int RegisterStaffDoctor = 9;
    public const int UpdateRole = 8;
    public const int UpdateUser = 8;
    public const int ChangeMyPassword = 13;
    public const int AdminChangePassword = 8;
    public const int Login = 10;
    public const int RefreshToken = 10;
    public const int RevokeToken = 13;
    public const int Me = 15;
    public const int UpdateProfile = 11;
    public const int Users = 14;
    public const int UsersByContact = 11;
    public const int UserId = 15;

    public static int UserProfile(Guid userId) => 7;
    public static int DeleteUser(Guid userId) => 18;
    public static int UserKeys(Guid userId) => 8;
}

public static class Doctors
{
    public const int GetAll = 11;
    public const int ByMyOrganization = 15;
    public static int ByUserIdInMyOrg(Guid userId) => 18;
    public static int GetById(Guid doctorId) => 10;
    public const int Create = 22;
    public static int Update(Guid doctorId) => 17;
    public static int Delete(Guid doctorId) => 13;
}

public static class Patients
{
    public const int GetAll = 10;
    public const int Create = 19;
    public static int GetById(Guid patientId) => 9;
    public static int Update(Guid patientId) => 13;
    public static int Delete(Guid patientId) => 10;
}

public static class Staff
{
    public const int GetAll = 10;
    public const int Create = 23;
    public static int GetById(Guid staffId) => 8;
    public static int Update(Guid staffId) => 17;
    public static int Delete(Guid staffId) => 12;
}

public static class Appointments
{
    public const int Create = 13;
    public const int GetAll = 13;
    public const int SearchDoctors = 9;
    public static int GetById(Guid id) => 12;
    public static int UpdateStatus(Guid id, string status) => 11;
    public static int Reschedule(Guid id, string newDate) => 12;
    public static int Confirm(Guid id) => 10;
    public static int Reject(Guid id) => 12;
    public static int Cancel(Guid id) => 13;
    public static int CheckIn(Guid id) => 12;
    public static int PatientsByDoctor(Guid doctorId) => 9;
}

public static class Encounters
{
    public const int Create = 12;
    public static int GetById(Guid id) => 12;
    public static int ByAppointment(Guid appointmentId) => 10;
    public static int ByPatient(Guid patientId) => 10;
    public static int Update(Guid id) => 11;
    public static int Complete(Guid id) => 11;
}

public static class Audit
{
    public const int Create = 6;
    public static int GetById(Guid id) => 7;
    public const int Search = 7;
    public const int Stats = 8;
    public static int ByPatient(Guid patientId) => 6;
    public static int ByActor(Guid actorUserId) => 6;
    public static int ByTarget(Guid targetId) => 6;
    public static int SyncFromBlockchain(string blockchainAuditId) => 7;
}

public static class Consents
{
    public const int Grant = 9;
    public static int GetById(Guid id) => 9;
    public static int ByPatient(Guid patientId) => 10;
    public static int ByGrantee(Guid granteeId) => 10;
    public const int Search = 7;
    public static int Revoke(Guid id) => 10;
    public const int Verify = 7;
    public static int SyncFromBlockchain(string blockchainConsentId) => 9;
}

public static class AccessRequests
{
    public const int Create = 9;
    public static int GetById(Guid id) => 9;
    public static int ByPatient(Guid patientId) => 14;
    public static int ByRequester(Guid requesterId) => 14;
    public static int Respond(Guid id) => 11;
    public static int Cancel(Guid id) => 9;
}

public static class Ehr
{
    public const int CreateRecord = 14;
    public const int EncryptToIpfs = 15;
    public const int DecryptFromIpfs = 15;
    public static int GetRecord(Guid ehrId) => 25;
    public static int UpdateRecord(Guid ehrId) => 12;
    public static int GetDocument(Guid ehrId) => 18;
    public static int GetDocumentSelf(Guid ehrId) => 11;
    public static int PatientRecords(Guid patientId) => 8;
    public static int OrgRecords(Guid orgId) => 8;
    public static int Versions(Guid ehrId) => 8;
    public static int VersionById(Guid ehrId, Guid versionId) => 9;
    public static int Files(Guid ehrId) => 8;
    public static int AddFile(Guid ehrId) => 14;
    public static int DeleteFile(Guid ehrId, Guid fileId) => 9;
}

public static class Notifications
{
    public const int Send = 6;
    public const int Broadcast = 7;
    public static int ByUser(string userDid) => 7;
    public static int Unread(string userDid) => 7;
    public static int UnreadCount(string userDid) => 6;
    public static int MarkRead(string userDid) => 7;
    public static int MarkAllRead(string userDid) => 7;
    public static int Delete(Guid id) => 7;
}

public static class DeviceTokens
{
    public const int Register = 6;
    public static int ByUser(string userDid) => 5;
    public static int Deactivate(Guid id) => 6;
    public static int DeactivateAll(string userDid) => 6;
}

public static class Preferences
{
    public static int Get(string userDid) => 6;
    public static int Update(string userDid) => 7;
}

public static class Organizations
{
    public const int Create = 8;
    public const int GetAll = 9;
    public static int GetById(Guid id) => 7;
    public static int Update(Guid id) => 8;
    public static int Delete(Guid id) => 8;
    public static int Verify(Guid id, Guid verifiedByUserId) => 9;
}

public static class Departments
{
    public const int Create = 8;
    public static int GetById(Guid id) => 8;
    public static int ByOrganization(Guid orgId) => 10;
    public static int Update(Guid id) => 8;
    public static int Delete(Guid id) => 8;
}

public static class Memberships
{
    public const int Create = 8;
    public static int GetById(Guid id) => 8;
    public static int ByOrganization(Guid orgId) => 9;
    public static int ByUser(Guid userId) => 9;
    public const int SearchDoctors = 6;
    public static int Update(Guid id) => 8;
    public static int Delete(Guid id) => 8;
}

public static class PaymentConfig
{
    public static int Configure(Guid orgId) => 7;
    public static int Update(Guid orgId) => 8;
    public static int GetStatus(Guid orgId) => 6;
}

public static class Internal
{
    public static int GetPaymentKeys(Guid orgId) => 18;
}

public static class Invoices
{
    public const int Create = 8;
    public static int GetById(Guid invoiceId) => 8;
    public static int ByPatient(Guid patientId) => 5;
    public static int ByOrg(Guid orgId) => 5;
    public static int Cancel(Guid invoiceId) => 8;
    public static int Checkout(Guid invoiceId) => 8;
    public static int PayCash(Guid invoiceId) => 10;
}

public static class Payments
{
    public static int GetById(Guid paymentId) => 8;
    public static int Verify(Guid paymentId) => 8;
    public const int Webhook = 8;
}