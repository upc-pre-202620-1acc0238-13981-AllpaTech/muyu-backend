using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.ClimateAlerts.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.ClimateAlerts.Presentation;

[ApiController]
[Authorize]
[Route("api/climate-alerts")]
[Route("api/v1/climate-alerts")]
public sealed class AlertsController(
    ICommandHandler<MonitorParcelCommand, MonitoringResult> monitorHandler,
    IQueryHandler<ListOpenClimateAlertsQuery, IReadOnlyList<ClimateAlertResponse>> listHandler,
    ICommandHandler<AcknowledgeClimateAlertCommand, bool> acknowledgeHandler,
    ICommandHandler<ResolveClimateAlertCommand, bool> resolveHandler) : ControllerBase
{
    [HttpPost("monitor")]
    [HttpPost("/api/v1/webhooks/weather-update")]
    public async Task<ActionResult<MonitoringResult>> Monitor(
        MonitorParcelCommand command,
        CancellationToken cancellationToken) =>
        Ok(await monitorHandler.HandleAsync(command, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClimateAlertResponse>>> ListOpen(
        [FromQuery] Guid parcelId,
        CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(new ListOpenClimateAlertsQuery(parcelId), cancellationToken));

    [HttpGet("/api/v1/alerts/{parcelId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ClimateAlertResponse>>> ListByParcel(
        Guid parcelId,
        CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(new ListOpenClimateAlertsQuery(parcelId), cancellationToken));

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken) =>
        await acknowledgeHandler.HandleAsync(new AcknowledgeClimateAlertCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken cancellationToken) =>
        await resolveHandler.HandleAsync(new ResolveClimateAlertCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();
}
