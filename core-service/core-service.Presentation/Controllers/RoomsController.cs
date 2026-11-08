using core_service.Application.Models;
using core_service.Application.Rooms.Queries;
using core_service.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace core_service.Presentation.Controllers;

[Authorize(Policy = "AccessOnly")]
[ApiController]
[Route("api/rooms")]
public class RoomsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MeetingRoomVM>>> GetRooms([FromQuery] GetMeetingRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDetailsVM>> GetRoomDetails([FromRoute] Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMeetingRoomByIdQuery(id, pageNumber, pageSize);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
