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
using System.Diagnostics;

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
        private readonly ILogger<VillaController> _logger;

        public VillaController(ApplicationDbContext db, IMapper mapper, IFileService fileService, ILogger<VillaController> logger)
        {
            _db = db;
            _mapper = mapper;
            _fileService = fileService;
            _logger = logger;
        }


        [HttpGet]
        //[Authorize(Roles ="Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> GetVillas(
            [FromQuery] string? filterBy,
            [FromQuery] string? filterQuery, 
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder = "asc", 
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default) // ✅ Added CancellationToken
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
            var totalCount = await villasQuery.CountAsync(cancellationToken); // ✅ Pass cancellationToken
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var villas = await villasQuery.Skip(skip).Take(pageSize).ToListAsync(cancellationToken); // ✅ Pass cancellationToken
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
        public async Task<ActionResult<ApiResponse<VillaDTO>>> GetVillaById(int id, CancellationToken cancellationToken = default) // ✅ Added CancellationToken
        {
            try
            {
                if (id <= 0)
                {
                    return NotFound(ApiResponse<object>.NotFound("Villa ID must be greater than 0"));
                }

                var villa = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id, cancellationToken); // ✅ Pass cancellationToken
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
        public async Task<ActionResult<ApiResponse<VillaDTO>>> CreateVilla([FromForm] VillaCreateDTO villaDTO, CancellationToken cancellationToken = default) // ✅ Added CancellationToken
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

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower(), cancellationToken); // ✅ Pass cancellationToken

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

                await _db.Villa.AddAsync(villa, cancellationToken); // ✅ Pass cancellationToken
                await _db.SaveChangesAsync(cancellationToken); // ✅ Pass cancellationToken

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
        public async Task<ActionResult<ApiResponse<VillaDTO>>> UpdateVilla(int id, [FromForm] VillaUpdateDTO villaDTO, CancellationToken cancellationToken = default) // ✅ Added CancellationToken
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

                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id, cancellationToken); // ✅ Pass cancellationToken

                if (existingVilla == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa with ID {id} was not found"));
                }

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower()
                && u.Id != id, cancellationToken); // ✅ Pass cancellationToken

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

                await _db.SaveChangesAsync(cancellationToken); // ✅ Pass cancellationToken

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
        public async Task<ActionResult<ApiResponse<object>>> DeleteVilla(int id, CancellationToken cancellationToken = default) // ✅ Added CancellationToken
        {
            try
            {
                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id, cancellationToken); // ✅ Pass cancellationToken

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
                await _db.SaveChangesAsync(cancellationToken); // ✅ Pass cancellationToken

                var response = ApiResponse<object>.NoContent("Villa deleted successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while deleting the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        // ========================================
        // 🎯 CANCELLATION TOKEN DEMONSTRATION ENDPOINTS
        // ========================================

        /// <summary>
        /// 🔴 Demo: Search WITHOUT CancellationToken (BAD PRACTICE)
        /// This endpoint demonstrates what happens when you DON'T use CancellationToken.
        /// Even if the client cancels, the server keeps processing!
        /// </summary>
        [HttpGet("demo/without-cancellation")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> DemoSearchWithoutCancellation(
            [FromQuery] string? searchTerm)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("🔴 DEMO WITHOUT CancellationToken - Started at {Time}", DateTime.UtcNow);

            try
            {
                var query = _db.Villa.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    _logger.LogInformation("   Filtering by search term...");
                    await Task.Delay(2000); // ❌ Simulate slow operation WITHOUT cancellation support
                    query = query.Where(v => v.Name.Contains(searchTerm) || v.Details.Contains(searchTerm));
                }

                _logger.LogInformation("   Executing database query...");
                await Task.Delay(2000); // ❌ Another slow operation
                var villas = await _db.Villa.ToListAsync(); // ❌ No cancellation token passed

                _logger.LogInformation("   Calculating statistics...");
                await Task.Delay(2000); // ❌ Yet another slow operation

                stopwatch.Stop();
                _logger.LogWarning("🔴 DEMO - Completed after {Duration}ms (even if client cancelled!)", stopwatch.ElapsedMilliseconds);

                var villaList = _mapper.Map<List<VillaDTO>>(villas);
                var message = $"⚠️ BAD: Server processed for {stopwatch.ElapsedMilliseconds}ms even if you cancelled! Wasted resources. Found {villaList.Count} villas.";

                return Ok(ApiResponse<IEnumerable<VillaDTO>>.Ok(villaList, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error in demo WITHOUT cancellation");
                return StatusCode(500, ApiResponse<IEnumerable<VillaDTO>>.Error(500, "Search failed", ex.Message));
            }
        }

        /// <summary>
        /// ✅ Demo: Search WITH CancellationToken (BEST PRACTICE)
        /// This endpoint demonstrates proper CancellationToken usage.
        /// When client cancels, the server stops immediately and frees resources!
        /// </summary>
        [HttpGet("demo/with-cancellation")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> DemoSearchWithCancellation(
            [FromQuery] string? searchTerm,
            CancellationToken cancellationToken) // ✅ Accept CancellationToken
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("✅ DEMO WITH CancellationToken - Started at {Time}", DateTime.UtcNow);

            try
            {
                var query = _db.Villa.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    _logger.LogInformation("   Filtering by search term...");
                    await Task.Delay(2000, cancellationToken); // ✅ Pass cancellationToken
                    query = query.Where(v => v.Name.Contains(searchTerm) || v.Details.Contains(searchTerm));
                }

                cancellationToken.ThrowIfCancellationRequested(); // ✅ Check for cancellation

                _logger.LogInformation("   Executing database query...");
                await Task.Delay(2000, cancellationToken); // ✅ Pass cancellationToken
                var villas = await query.ToListAsync(cancellationToken); // ✅ Pass to EF Core

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("   Calculating statistics...");
                await Task.Delay(2000, cancellationToken); // ✅ Pass cancellationToken

                stopwatch.Stop();
                _logger.LogInformation("✅ DEMO - Completed in {Duration}ms", stopwatch.ElapsedMilliseconds);

                var villaList = _mapper.Map<List<VillaDTO>>(villas);
                var message = $"✅ GOOD: Completed in {stopwatch.ElapsedMilliseconds}ms. Would have stopped immediately if cancelled! Found {villaList.Count} villas.";

                return Ok(ApiResponse<IEnumerable<VillaDTO>>.Ok(villaList, message));
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("✅ DEMO - CANCELLED after {Duration}ms (resources freed immediately!)", stopwatch.ElapsedMilliseconds);
                
                var message = $"✅ GOOD: Stopped after only {stopwatch.ElapsedMilliseconds}ms. Resources freed immediately! Compare to 6000ms+ in 'without-cancellation'.";
                return StatusCode(499, ApiResponse<IEnumerable<VillaDTO>>.Error(499, "Search cancelled by client", message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✅ Error in demo WITH cancellation");
                return StatusCode(500, ApiResponse<IEnumerable<VillaDTO>>.Error(500, "Search failed", ex.Message));
            }
        }

        /// <summary>
        /// ⏱️ Demo: Search with automatic timeout
        /// Demonstrates how to create a custom timeout using CancellationTokenSource
        /// </summary>
        [HttpGet("demo/with-timeout")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDTO>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaDTO>>>> DemoSearchWithTimeout(
            [FromQuery] string? searchTerm,
            [FromQuery] int timeoutSeconds = 3)
        {
            // ✅ Create CancellationTokenSource with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("⏱️ DEMO with {Timeout}s timeout - Started", timeoutSeconds);

            try
            {
                var query = _db.Villa.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(v => v.Name.Contains(searchTerm) || v.Details.Contains(searchTerm));
                }

                // Simulate a VERY long operation (10 seconds)
                _logger.LogInformation("   Starting long operation (10s - will timeout at {Timeout}s)...", timeoutSeconds);
                await Task.Delay(10000, cts.Token); // Will timeout before completing
                
                var villas = await query.ToListAsync(cts.Token);

                stopwatch.Stop();
                _logger.LogInformation("⏱️ DEMO - Completed in {Duration}ms", stopwatch.ElapsedMilliseconds);

                var villaList = _mapper.Map<List<VillaDTO>>(villas);
                var message = $"Completed within {timeoutSeconds}s timeout. Found {villaList.Count} villas in {stopwatch.ElapsedMilliseconds}ms.";

                return Ok(ApiResponse<IEnumerable<VillaDTO>>.Ok(villaList, message));
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("⏱️ DEMO - TIMED OUT after {Duration}ms (limit was {Timeout}s)", 
                    stopwatch.ElapsedMilliseconds, timeoutSeconds);
                
                var message = $"Operation exceeded the {timeoutSeconds}s timeout limit. Stopped at {stopwatch.ElapsedMilliseconds}ms to prevent long-running queries.";
                return StatusCode(408, ApiResponse<IEnumerable<VillaDTO>>.Error(408, $"Search timed out after {timeoutSeconds} seconds", message));
            }
        }
    }
}
