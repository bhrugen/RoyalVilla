using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalVilla.DTO;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using RoyalVilla_API.Services;
using System.Collections;

namespace RoyalVilla_API.Controllers.v2
{

    [Route("api/v{version:apiVersion}/villa")]
    [ApiVersion("2.0")]
    [ApiController]
    //[Authorize(Roles = "Customer,Admin")]
    public class VillaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public VillaController(ApplicationDbContext db, IMapper mapper, IFileService fileService)
        {
            _db = db;
            _mapper = mapper;
            _fileService = fileService;
        }


        [HttpGet]
        //[Authorize(Roles ="Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> GetVillas([FromQuery] string? filterBy,
            [FromQuery] string? filterQuery, [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder = "asc", [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            var villasQuery = _db.Villa.AsQueryable();
            if(!string.IsNullOrEmpty(filterQuery) && !string.IsNullOrEmpty(filterBy))
            {
                switch (filterBy.ToLower())
                {
                    case "name":
                        villasQuery = villasQuery.Where(u => u.Name.ToLower().Contains(filterQuery.ToLower()));
                        break;
                    case "details":
                        villasQuery = villasQuery.Where(u => u.Details.ToLower().Contains(filterQuery.ToLower()));
                        break;
                    case "rate":
                        if (double.TryParse(filterQuery, out double rate))
                        {
                            villasQuery = villasQuery.Where(u => u.Rate == rate);
                        }
                        break;
                    case "minrate":
                        if (double.TryParse(filterQuery, out double minrate))
                        {
                            villasQuery = villasQuery.Where(u => u.Rate >= minrate);
                        }
                        break;
                    case "maxrate":
                        if (double.TryParse(filterQuery, out double maxrate))
                        {
                            villasQuery = villasQuery.Where(u => u.Rate <= maxrate);
                        }
                        break;
                    case "occupancy":
                        if (int.TryParse(filterQuery, out int occupancy))
                        {
                            villasQuery = villasQuery.Where(u => u.Occupancy == occupancy);
                        }
                        break;
                }               
            }

            //sorting logic
            if (!string.IsNullOrEmpty(sortBy))
            {
                var isDescending = sortOrder?.ToLower() == "desc";

                villasQuery = sortBy.ToLower() switch
                {
                    "name" => isDescending ? villasQuery.OrderByDescending(u => u.Name)
                    : villasQuery.OrderBy(u => u.Name),
                    "rate" => isDescending ? villasQuery.OrderByDescending(u => u.Rate)
                    : villasQuery.OrderBy(u => u.Rate),
                    "occupancy" => isDescending ? villasQuery.OrderByDescending(u => u.Occupancy)
                    : villasQuery.OrderBy(u => u.Occupancy),
                    "sqft" => isDescending ? villasQuery.OrderByDescending(u => u.Sqft)
                    : villasQuery.OrderBy(u => u.Sqft),
                    "id" => isDescending ? villasQuery.OrderByDescending(u => u.Id)
                    : villasQuery.OrderBy(u => u.Id),
                    _=> villasQuery.OrderBy(u=>u.Id)
                };
            }
            else
            {
                villasQuery = villasQuery.OrderBy(u => u.Id);
            }

            //page 5, pagesize 10
            var skip = (page - 1) * pageSize;
            var totalCount = await villasQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var villas = await villasQuery.Skip(skip).Take(pageSize).ToListAsync();
            var dtoResponseVilla = _mapper.Map<List<VillaDTO>>(villas);
            

            var messageBuilder = new System.Text.StringBuilder();


            messageBuilder.Append($"Successfully retrieved {dtoResponseVilla.Count} villa(s)");
            messageBuilder.Append($"(Page {page} of {totalPages}, {totalCount} total records");
            if (!string.IsNullOrEmpty(filterQuery) && !string.IsNullOrEmpty(filterBy))
            {
                messageBuilder.Append($" filtered by {filterBy}: '{filterQuery}'");
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                messageBuilder.Append($" sorted by {sortBy}: '{sortOrder?.ToLower() ?? "asc"}'");
            }

            Response.Headers.Append("X-Pagination-CurrentPage", page.ToString());
            Response.Headers.Append("X-Pagination-PageSize", pageSize.ToString());
            Response.Headers.Append("X-Pagination-TotalCount", totalCount.ToString());
            Response.Headers.Append("X-Pagination-TotalPages", totalPages.ToString());


            var response = ApiResponse<IEnumerable<VillaDTO>>.Ok(dtoResponseVilla, messageBuilder.ToString());
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        //[AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<VillaDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<VillaDTO>>> GetVillaById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return NotFound(ApiResponse<object>.NotFound("Villa ID must be greater than 0"));
                }

                var villa = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);
                if (villa == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa with ID {id} was not found"));
                }
                return Ok(ApiResponse<VillaDTO>.Ok(_mapper.Map<VillaDTO>(villa), "Records retrieved successfully"));
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while retrieving the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<VillaDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaDTO>>> CreateVilla([FromForm] VillaCreateDTO villaDTO)
        {
            try
            {
                if (villaDTO == null)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa data is required"));
                }

                // Validate image if provided
                if (villaDTO.Image != null && !_fileService.ValidateImage(villaDTO.Image))
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Invalid image file. Allowed formats: jpg, jpeg, png, gif, webp. Max size: 5MB"));
                }

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower());

                if (duplicateVilla != null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"A villa with the name '{villaDTO.Name}' already exists"));
                }

                Villa villa = _mapper.Map<Villa>(villaDTO);

                // Handle image upload
                if (villaDTO.Image != null)
                {
                    villa.ImageUrl = await _fileService.UploadImageAsync(villaDTO.Image);
                }

                villa.CreatedDate = DateTime.UtcNow;

                await _db.Villa.AddAsync(villa);
                await _db.SaveChangesAsync();

                var response = ApiResponse<VillaDTO>.CreatedAt(_mapper.Map<VillaDTO>(villa), "Villa created successfully");
                return CreatedAtAction(nameof(GetVillaById), new { id = villa.Id, version = "2.0" }, response);
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<VillaDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaDTO>>> UpdateVilla(int id, [FromForm] VillaUpdateDTO villaDTO)
        {
            try
            {
                if (villaDTO == null)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa data is required"));
                }

                if (id != villaDTO.Id)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa ID in URL does not match Villa ID in request body"));
                }

                // Validate image if provided
                if (villaDTO.Image != null && !_fileService.ValidateImage(villaDTO.Image))
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Invalid image file. Allowed formats: jpg, jpeg, png, gif, webp. Max size: 5MB"));
                }

                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVilla == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa with ID {id} was not found"));
                }

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower()
                && u.Id != id);

                if (duplicateVilla != null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"A villa with the name '{villaDTO.Name}' already exists"));
                }

                // Store old image URL for deletion
                var oldImageUrl = existingVilla.ImageUrl;

                // Map DTO to existing entity
                _mapper.Map(villaDTO, existingVilla);

                // Handle image upload
                if (villaDTO.Image != null)
                {
                    // Upload new image
                    existingVilla.ImageUrl = await _fileService.UploadImageAsync(villaDTO.Image);

                    // Delete old image if it exists and is different
                    if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != existingVilla.ImageUrl)
                    {
                        await _fileService.DeleteImageAsync(oldImageUrl);
                    }
                }

                existingVilla.UpdatedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var response = ApiResponse<VillaDTO>.Ok(_mapper.Map<VillaDTO>(existingVilla), "Villa updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while updating the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }


        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteVilla(int id)
        {
            try
            {
                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVilla == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa with ID {id} was not found"));
                }

                // Delete associated image if it exists
                if (!string.IsNullOrEmpty(existingVilla.ImageUrl))
                {
                    await _fileService.DeleteImageAsync(existingVilla.ImageUrl);
                }

                _db.Villa.Remove(existingVilla);
                await _db.SaveChangesAsync();

                var response = ApiResponse<object>.NoContent("Villa deleted successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while deleting the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

    }
}
