using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalVilla.DTO;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using System.Collections;

namespace RoyalVilla_API.Controllers.v1
{
   
    [Route("api/v{version:apiVersion}/villa")]
    [ApiVersion("1.0")]
    [ApiController]
    //[Authorize(Roles = "Customer,Admin")]
    public class VillaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public VillaController(ApplicationDbContext db, IMapper mapper)
        {
            _db= db;
            _mapper= mapper;
        }


        [HttpGet]
        [ResponseCache(CacheProfileName = "Default30")]
        //[Authorize(Roles ="Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> GetVillas([FromQuery] string? filterBy,
            [FromQuery] string? filterQuery,
             [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder = "asc",
              [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit maximum page size
            var villasQuery = _db.Villa.AsQueryable();

            // Apply filtering if both filterBy and filterQuery are provided
            if (!string.IsNullOrWhiteSpace(filterBy) && !string.IsNullOrWhiteSpace(filterQuery))
            {
                switch (filterBy.ToLower())
                {
                    case "name":
                        villasQuery = villasQuery.Where(v => v.Name.Contains(filterQuery));
                        break;
                    case "details":
                        villasQuery = villasQuery.Where(v => v.Details != null && v.Details.Contains(filterQuery));
                        break;
                    case "rate":
                        if (double.TryParse(filterQuery, out double rate))
                        {
                            villasQuery = villasQuery.Where(v => v.Rate == rate);
                        }
                        break;
                    case "minrate":
                        if (double.TryParse(filterQuery, out double minRate))
                        {
                            villasQuery = villasQuery.Where(v => v.Rate >= minRate);
                        }
                        break;
                    case "maxrate":
                        if (double.TryParse(filterQuery, out double maxRate))
                        {
                            villasQuery = villasQuery.Where(v => v.Rate <= maxRate);
                        }
                        break;
                    case "occupancy":
                        if (int.TryParse(filterQuery, out int occupancy))
                        {
                            villasQuery = villasQuery.Where(v => v.Occupancy == occupancy);
                        }
                        break;

                    default:
                        // If filterBy is not recognized, ignore filtering
                        break;
                }

            }
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var isDescending = sortOrder?.ToLower() == "desc";

                villasQuery = sortBy.ToLower() switch
                {
                    "name" => isDescending
                        ? villasQuery.OrderByDescending(v => v.Name)
                        : villasQuery.OrderBy(v => v.Name),
                    "rate" => isDescending
                        ? villasQuery.OrderByDescending(v => v.Rate)
                        : villasQuery.OrderBy(v => v.Rate),
                    "occupancy" => isDescending
                        ? villasQuery.OrderByDescending(v => v.Occupancy)
                        : villasQuery.OrderBy(v => v.Occupancy),
                    "sqft" => isDescending
                        ? villasQuery.OrderByDescending(v => v.Sqft)
                        : villasQuery.OrderBy(v => v.Sqft),
                    "id" => isDescending
                        ? villasQuery.OrderByDescending(v => v.Id)
                        : villasQuery.OrderBy(v => v.Id),
                    _ => villasQuery.OrderBy(v => v.Id) // Default sort by ID if sortBy is not recognized
                };
            }
            else
            {
                // Default sorting by ID if no sortBy is specified
                villasQuery = villasQuery.OrderBy(v => v.Id);
            }
            // Apply pagination
            var skip = (page - 1) * pageSize;
            var villas = await villasQuery
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var villaDTOs = _mapper.Map<List<VillaDTO>>(villas);

            // Calculate pagination metadata
            var totalCount = await villasQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var hasNextPage = page < totalPages;
            var hasPreviousPage = page > 1;

            // Build comprehensive message
            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.Append($"Successfully retrieved {villaDTOs.Count} villa(s)");
            messageBuilder.Append($" (Page {page} of {totalPages}, {totalCount} total)");

            if (!string.IsNullOrWhiteSpace(filterBy) && !string.IsNullOrWhiteSpace(filterQuery))
            {
                messageBuilder.Append($" filtered by {filterBy}: '{filterQuery}'");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                messageBuilder.Append($" sorted by {sortBy} ({sortOrder?.ToLower() ?? "asc"})");
            }

            // Create response with pagination metadata
            var response = ApiResponse<IEnumerable<VillaDTO>>.Ok(villaDTOs, messageBuilder.ToString());

            // Add pagination metadata to response headers for better API design
            Response.Headers.Append("X-Pagination-CurrentPage", page.ToString());
            Response.Headers.Append("X-Pagination-PageSize", pageSize.ToString());
            Response.Headers.Append("X-Pagination-TotalCount", totalCount.ToString());
            Response.Headers.Append("X-Pagination-TotalPages", totalPages.ToString());
            Response.Headers.Append("X-Pagination-HasNext", hasNextPage.ToString().ToLower());
            Response.Headers.Append("X-Pagination-HasPrevious", hasPreviousPage.ToString().ToLower());
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 30)]
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
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<VillaDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaDTO>>> CreateVilla(VillaCreateDTO villaDTO)
        {
            try
            {
                if (villaDTO == null)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa data is required"));
                }


                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower());

                if (duplicateVilla != null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"A villa with the name '{villaDTO.Name}' already exists"));
                }

                Villa villa = _mapper.Map<Villa>(villaDTO);

                await _db.Villa.AddAsync(villa);
                await _db.SaveChangesAsync();

                var response = ApiResponse<VillaDTO>.CreatedAt(_mapper.Map<VillaDTO>(villa), "Villa created successfully");
                return CreatedAtAction(nameof(CreateVilla), new {id=villa.Id},response);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
                return StatusCode(500,errorResponse);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<VillaDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaDTO>>> UpdateVilla(int id, VillaUpdateDTO villaDTO)
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
                

                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVilla ==null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa with ID {id} was not found"));
                }

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower()
                && u.Id != id);

                if (duplicateVilla != null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"A villa with the name '{villaDTO.Name}' already exists"));
                }

                _mapper.Map(villaDTO,existingVilla);
                existingVilla.UpdatedDate = DateTime.Now;
                
                await _db.SaveChangesAsync();
                var response = ApiResponse<VillaDTO>.Ok(_mapper.Map<VillaDTO>(villaDTO), "Villa updated successfully");
                return Ok(villaDTO);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
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

                _db.Villa.Remove(existingVilla);
                await _db.SaveChangesAsync();

                var response = ApiResponse<object>.NoContent("Villa deleted successfully");
                return Ok(response);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

    }
}
