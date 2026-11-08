using core_service.Application.Bookings.Commands.CancelBooking;
using core_service.Application.Bookings.Commands.CreateBooking;
using core_service.Application.Bookings.Commands.RescheduleBooking;
using core_service.Application.Bookings.Queries;
using core_service.Application.Models;
using core_service.Application.ViewModels;
using core_service.Presentation.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace core_service.Presentation.Controllers;

[Authorize(Policy = "AccessOnly")]
[ApiController]
[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateBookingAsync([FromBody] CreateBookingCommand request,CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelBookingAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelBookingCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }


    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingItemVM>>> GetBookingsAsync([FromQuery] GetBookingsByAuthorQuery query,
                                                                                 CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleBookingAsync([FromRoute] Guid id,
                                                            [FromBody] ResheduleBookingDTO dto,
                                                            CancellationToken cancellationToken = default)
    {
        var request = new RescheduleBookingCommand(id, dto.NewStartTime, dto.NewEndTime);
        var result = await Sender.Send(request, cancellationToken);

        return HandleResult(result);
    }
}
