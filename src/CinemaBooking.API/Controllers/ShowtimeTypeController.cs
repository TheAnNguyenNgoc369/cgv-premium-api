using CinemaBooking.API.Contracts.ShowtimeTypes;
using CinemaBooking.Application.ActivityLogs;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.ShowtimeTypes;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/showtime-types"), Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
public sealed class ShowtimeTypeController(IShowtimeTypeService service, IManagerCinemaScopeService scopes, IActivityLogService logs) : ControllerBase
{
 [HttpGet] public async Task<IActionResult> List(int? cinemaId,bool? isActive,int page=1,int pageSize=10,CancellationToken ct=default){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.ListAsync(cinemaId,isActive,page,pageSize,s.CinemaId,ct);if(!r.Succeeded)return Error(r.Error!);var p=r.Page!;return Ok(new{items=p.Items.Select(Summary),page,pageSize,totalItems=p.TotalItems,totalPages=(int)Math.Ceiling(p.TotalItems/(double)pageSize)});}
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.GetAsync(id,s.CinemaId,ct);return r.Succeeded?Ok(Detail(r.Value!)):Error(r.ErrorMessage!);}
 [HttpPost] public async Task<IActionResult> Create(CreateShowtimeTypeRequest q,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.CreateAsync(q.CinemaId,q.Name,q.Slots,s.CinemaId,ct);if(!r.Succeeded)return Error(r.ErrorMessage!);await Log(AdminActionTypes.CreateShowtimeType,r.Value!.ShowtimeTypeID,"Created showtime type",ct);return StatusCode(201,new{success=true,id=r.Value.ShowtimeTypeID,message="Created showtime type successfully."});}
 [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,UpdateShowtimeTypeRequest q,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.UpdateAsync(id,q.Name,q.IsActive,q.Slots,s.CinemaId,ct);if(!r.Succeeded)return Error(r.ErrorMessage!);await Log(AdminActionTypes.UpdateShowtimeType,id,"Updated showtime type",ct);return Ok(new{success=true,id,message="Updated showtime type successfully."});}
 [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.DeleteAsync(id,s.CinemaId,ct);if(!r.Succeeded)return Error(r.ErrorMessage!);await Log(AdminActionTypes.DeleteShowtimeType,id,"Deactivated showtime type",ct);return NoContent();}
 [HttpPost("preview")] public async Task<IActionResult> Preview(ShowtimeTypeBatchRequest q,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.PreviewAsync(q.MovieId,q.RoomId,q.StartDate,q.EndDate,q.ShowtimeTypeId,q.BasePrice,s.CinemaId,ct);return r.Succeeded?Ok(Batch(r,false)):Error(r.ErrorMessage!);}
 [HttpPost("generate")] public async Task<IActionResult> Generate(ShowtimeTypeBatchRequest q,CancellationToken ct){var s=await Scope(ct);if(s.Forbidden)return Denied();var r=await service.GenerateAsync(q.MovieId,q.RoomId,q.StartDate,q.EndDate,q.ShowtimeTypeId,q.BasePrice,s.CinemaId,ct);if(!r.Succeeded)return Error(r.ErrorMessage!);await Log(AdminActionTypes.GenerateShowtimeByType,q.ShowtimeTypeId,$"Generated {r.ValidCount}; skipped {r.ConflictCount}",ct);return Ok(Batch(r,true));}
 private async Task<(bool Forbidden,int? CinemaId)> Scope(CancellationToken ct){if(User.IsInRole(Roles.Admin))return(false,null);if(!int.TryParse(User.FindFirst("userId")?.Value,out var id))return(true,null);var c=await scopes.GetAssignedCinemaIdAsync(id,ct);return(!c.HasValue,c);}
 private IActionResult Error(string m)=>m switch{"Access denied."=>Denied(),"Showtime type not found." or "Movie not found." or "Room not found." or "Cinema not found."=>NotFound(new{success=false,message=m}),_=>BadRequest(new{success=false,message=m})};
 private ObjectResult Denied()=>StatusCode(403,new{success=false,message="Access denied."});
 private Task Log(string a,int id,string d,CancellationToken ct)=>logs.RecordAsync(this.AuditActorId(),a,"ShowtimeType",id,d,this.AuditIpAddress(),ct);
 private static object Summary(ShowtimeType x)=>new{id=x.ShowtimeTypeID,cinemaId=x.CinemaID,name=x.Name,isActive=x.IsActive,slots=x.Slots.OrderBy(s=>s.StartTime).Select(s=>s.StartTime)};
 private static object Detail(ShowtimeType x)=>new{id=x.ShowtimeTypeID,cinemaId=x.CinemaID,name=x.Name,isActive=x.IsActive,slots=x.Slots.OrderBy(s=>s.StartTime).Select(s=>new{id=s.SlotID,startTime=s.StartTime})};
 private static object Batch(ShowtimeTypeBatchResult r,bool g)=>new{success=g?(bool?)true:null,items=r.Items.Select(x=>new{date=x.Date,startTime=VietnamTime.FromUtc(x.StartTime),endTime=VietnamTime.FromUtc(x.EndTime),isConflict=x.IsConflict,conflictCode=x.ConflictCode,status=g?x.Status:null,reason=x.Reason}),validCount=g?(int?)null:r.ValidCount,conflictCount=g?(int?)null:r.ConflictCount,generatedCount=g?r.ValidCount:(int?)null,skippedCount=g?r.ConflictCount:(int?)null};
}
