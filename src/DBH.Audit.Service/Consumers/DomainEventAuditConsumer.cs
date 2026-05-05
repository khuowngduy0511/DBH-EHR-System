using DBH.Audit.Service.DTOs;
using DBH.Audit.Service.Models.Enums;
using DBH.Audit.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Audit.Service.Consumers;

public class DomainEventAuditConsumer :
    IConsumer<EhrCreatedEvent>,
    IConsumer<EhrUpdatedEvent>,
    IConsumer<EhrAccessedEvent>,
    IConsumer<ConsentGrantedEvent>,
    IConsumer<ConsentRevokedEvent>,
    IConsumer<AppointmentCreatedEvent>,
    IConsumer<InvoicePaidEvent>
{
    private readonly IAuditService _auditService;
    private readonly ILogger<DomainEventAuditConsumer> _logger;

    public DomainEventAuditConsumer(IAuditService auditService, ILogger<DomainEventAuditConsumer> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EhrCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing EhrCreatedEvent EhrId={EhrId}", evt.EhrId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.CreatedByDid,
            ActorType = ActorType.DOCTOR,
            Action = AuditAction.CREATE,
            TargetType = TargetType.EHR,
            TargetId = evt.EhrId,
            PatientDid = evt.PatientDid,
            OrganizationId = evt.OrganizationId,
            Result = AuditResult.SUCCESS
        });
    }

    public async Task Consume(ConsumeContext<EhrUpdatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing EhrUpdatedEvent EhrId={EhrId}", evt.EhrId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.UpdatedByDid,
            ActorType = ActorType.DOCTOR,
            Action = AuditAction.UPDATE,
            TargetType = TargetType.EHR,
            TargetId = evt.EhrId,
            PatientDid = evt.PatientDid,
            Result = AuditResult.SUCCESS,
            Metadata = evt.ChangeDescription
        });
    }

    public async Task Consume(ConsumeContext<EhrAccessedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing EhrAccessedEvent EhrId={EhrId}", evt.EhrId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.AccessedByDid,
            ActorType = ActorType.DOCTOR,
            Action = AuditAction.VIEW,
            TargetType = TargetType.EHR,
            TargetId = evt.EhrId,
            PatientDid = evt.PatientDid,
            OrganizationId = evt.AccessedByOrganizationId,
            ConsentId = Guid.TryParse(evt.ConsentId, out var cid) ? cid : null,
            Result = AuditResult.SUCCESS,
            Metadata = $"AccessType={evt.AccessType};Purpose={evt.Purpose}"
        });
    }

    public async Task Consume(ConsumeContext<ConsentGrantedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing ConsentGrantedEvent ConsentId={ConsentId}", evt.ConsentId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.PatientDid,
            ActorType = ActorType.PATIENT,
            Action = AuditAction.GRANT_CONSENT,
            TargetType = TargetType.CONSENT,
            TargetId = Guid.TryParse(evt.ConsentId, out var cgid) ? cgid : null,
            PatientDid = evt.PatientDid,
            ConsentId = cgid == default ? null : cgid,
            Result = AuditResult.SUCCESS,
            Metadata = $"GranteeDid={evt.GranteeDid};Purpose={evt.Purpose}"
        });
    }

    public async Task Consume(ConsumeContext<ConsentRevokedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing ConsentRevokedEvent ConsentId={ConsentId}", evt.ConsentId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.PatientDid,
            ActorType = ActorType.PATIENT,
            Action = AuditAction.REVOKE_CONSENT,
            TargetType = TargetType.CONSENT,
            TargetId = Guid.TryParse(evt.ConsentId, out var crid) ? crid : null,
            PatientDid = evt.PatientDid,
            ConsentId = crid == default ? null : crid,
            Result = AuditResult.SUCCESS,
            Metadata = $"GranteeDid={evt.GranteeDid};Reason={evt.Reason}"
        });
    }

    public async Task Consume(ConsumeContext<AppointmentCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing AppointmentCreatedEvent AppointmentId={AppointmentId}", evt.AppointmentId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.PatientId.ToString(),
            ActorType = ActorType.PATIENT,
            Action = AuditAction.CREATE,
            TargetType = TargetType.SYSTEM,
            TargetId = evt.AppointmentId,
            PatientId = evt.PatientId,
            OrganizationId = evt.OrganizationId,
            Result = AuditResult.SUCCESS
        });
    }

    public async Task Consume(ConsumeContext<InvoicePaidEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Auditing InvoicePaidEvent InvoiceId={InvoiceId}", evt.InvoiceId);
        await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
        {
            ActorDid = evt.PatientId.ToString(),
            ActorType = ActorType.PATIENT,
            Action = AuditAction.UPDATE,
            TargetType = TargetType.SYSTEM,
            TargetId = evt.InvoiceId,
            PatientId = evt.PatientId,
            OrganizationId = evt.OrgId,
            Result = AuditResult.SUCCESS,
            Metadata = $"TotalAmount={evt.TotalAmount};PaidAt={evt.PaidAt:O}"
        });
    }
}
