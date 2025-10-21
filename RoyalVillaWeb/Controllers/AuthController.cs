using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;
using System.Diagnostics;

namespace RoyalVillaWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, ITokenProvider tokenProvider, IMapper mapper)
        {
            _mapper = mapper;
            _authService = authService;
            _tokenProvider = tokenProvider;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDTO loginRequestDTO)
        {
            try
            {
                var response = await _authService.LoginAsync<ApiResponse<TokenDTO>>(loginRequestDTO);
                
                // Handle null response
                if (response == null)
                {
                    TempData["error"] = "Unable to connect to the server. Please try again.";
                    return View(loginRequestDTO);
                }

                // Handle successful login - check for BOTH tokens
                if (response.Success && response.Data != null && 
                    !string.IsNullOrEmpty(response.Data.AccessToken) && 
                    !string.IsNullOrEmpty(response.Data.RefreshToken))
                {
                    // Extract claims from JWT token using TokenProvider
                    var principal = _tokenProvider.GetPrincipalFromToken(response.Data.AccessToken);
                    
                    if (principal != null)
                    {
                        // Sign in the user with cookie authentication
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                        
                        // ✅ Store BOTH access and refresh tokens
                        _tokenProvider.SetToken(response.Data.AccessToken, response.Data.RefreshToken);
                        
                        TempData["success"] = "Login successful!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["error"] = "Invalid token received. Please try again.";
                    }
                }
                else
                {
                    TempData["error"] = response.Message ?? "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"An error occurred: {ex.Message}";
            }

            return View(loginRequestDTO);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterationRequestDTO
            {
                Email = string.Empty,
                Name = string.Empty,
                Password = string.Empty,
                Role = "Customer"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            try
            {
                var response = await _authService.RegisterAsync<ApiResponse<UserDTO>>(registerationRequestDTO);
                
                // Handle null response
                if (response == null)
                {
                    TempData["error"] = "Unable to connect to the server. Please try again.";
                    return View(registerationRequestDTO);
                }

                // Handle successful registration
                if (response.Success && response.Data != null)
                {
                    TempData["success"] = "Registration successful! Please login with your credentials.";
                    return RedirectToAction("Login");
                }
                
                // Handle API errors
                TempData["error"] = response.Message ?? "Registration failed. Please try again.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"An error occurred: {ex.Message}";
            }

            return View(registerationRequestDTO);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            // Clear authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Clear tokens from session using TokenProvider
            _tokenProvider.ClearToken();
            
            // Clear session
            HttpContext.Session.Clear();
            
            TempData["success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}

