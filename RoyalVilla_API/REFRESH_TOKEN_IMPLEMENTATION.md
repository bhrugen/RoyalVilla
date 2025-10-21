# Refresh Token Implementation - Complete Guide

## Overview
Implemented complete refresh token functionality for the Royal Villa API, enabling secure token rotation and extended user sessions.

## Architecture

### Token Strategy
- **Access Token**: Short-lived (15 minutes) JWT token for API authentication
- **Refresh Token**: Long-lived (7 days) cryptographically secure random token stored in database

### Security Benefits
? **Reduced Attack Surface**: Short-lived access tokens limit exposure if compromised  
? **Token Rotation**: Each refresh generates new tokens (access + refresh)  
? **Revocation Support**: Refresh tokens can be invalidated (logout, security breach)  
? **Single-Use Refresh Tokens**: Old refresh token is revoked when new one is issued  
? **Database Validation**: Refresh tokens validated against database, not just signature  

## Components Implemented

### 1. Database Model

**RefreshToken.cs**
```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; }              // User who owns the token
    public string JwtTokenId { get; set; }          // JWT ID (jti claim) for tracking
    public string RefreshTokenValue { get; set; }   // Actual refresh token
    public bool IsValid { get; set; }               // Can be revoked
    public DateTime ExpiresAt { get; set; }         // Expiration timestamp
}
```

### 2. DTOs

**TokenDTO.cs** (Enhanced)
```csharp
public class TokenDTO
{
    public string? AccessToken { get; set; }     // JWT access token
    public string? RefreshToken { get; set; }    // Refresh token
    public DateTime? ExpiresAt { get; set; }     // Access token expiration
}
```

**RefreshTokenRequestDTO.cs** (New)
```csharp
public class RefreshTokenRequestDTO
{
    public string? AccessToken { get; set; }     // Optional: old access token
    public string? RefreshToken { get; set; }    // Required: refresh token
}
```

### 3. Service Layer

**ITokenService** (Extended)
```csharp
public interface ITokenService
{
    Task<string> GenerateJwtTokenAsync(ApplicationUser user);
    Task<string> GenerateRefreshTokenAsync();
    ClaimsPrincipal? ValidateToken(string token);
    Task<(bool IsValid, string? UserId)> ValidateRefreshTokenAsync(string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
}
```

**Key Methods:**

#### GenerateRefreshTokenAsync()
- Generates cryptographically secure 64-byte random token
- Base64 encodes for safe transmission
- Checks database for uniqueness (collision prevention)
- Returns unique refresh token

```csharp
public async Task<string> GenerateRefreshTokenAsync()
{
    var randomNumber = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    
    var refreshToken = Convert.ToBase64String(randomNumber);
    
    // Ensure uniqueness
    var exists = await _db.RefreshTokens.AnyAsync(rt => rt.RefreshTokenValue == refreshToken);
    if (exists)
    {
        return await GenerateRefreshTokenAsync(); // Recursive retry
    }
    
    return refreshToken;
}
```

#### ValidateRefreshTokenAsync()
- Queries database for matching refresh token
- Checks if token is valid (not revoked)
- Verifies expiration timestamp
- Returns validation result + user ID

```csharp
public async Task<(bool IsValid, string? UserId)> ValidateRefreshTokenAsync(string refreshToken)
{
    var storedToken = await _db.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.RefreshTokenValue == refreshToken 
                                && rt.IsValid 
                                && rt.ExpiresAt > DateTime.UtcNow);

    if (storedToken == null)
    {
        return (false, null);
    }

    return (true, storedToken.UserId);
}
```

### 4. AuthService Methods

**LoginAsync** (Enhanced)
- Generates both access and refresh tokens
- Extracts JWT ID from access token
- Saves refresh token to database
- Returns complete TokenDTO

```csharp
public async Task<TokenDTO?> LoginAsync(LoginRequestDTO loginRequestDTO)
{
    // 1. Validate user credentials
    var user = await _db.ApplicationUsers.FirstOrDefaultAsync(...);
    bool isValid = await _userManager.CheckPasswordAsync(user, password);
    
    // 2. Generate access token (15 min)
    var accessToken = await _tokenService.GenerateJwtTokenAsync(user);
    
    // 3. Extract JWT ID
    var jwtToken = tokenHandler.ReadJwtToken(accessToken);
    var jwtTokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
    
    // 4. Generate refresh token (7 days)
    var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
    
    // 5. Save to database
    await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, refreshToken, expiresAt);
    
    // 6. Return both tokens
    return new TokenDTO
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = jwtToken.ValidTo
    };
}
```

**RefreshTokenAsync** (New)
- Validates refresh token
- Revokes old refresh token
- Generates new access + refresh tokens
- Returns new TokenDTO

```csharp
public async Task<TokenDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request)
{
    // 1. Validate refresh token
    var (isValid, userId) = await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken);
    if (!isValid) return null;
    
    // 2. Get user
    var user = await _db.ApplicationUsers.FindAsync(userId);
    
    // 3. Revoke old refresh token
    await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
    
    // 4. Generate new tokens
    var newAccessToken = await _tokenService.GenerateJwtTokenAsync(user);
    var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
    
    // 5. Save new refresh token
    await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, newRefreshToken, expiresAt);
    
    // 6. Return new tokens
    return new TokenDTO { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
}
```

**RevokeTokenAsync** (New)
- Marks refresh token as invalid
- Used for logout or security incidents

### 5. API Endpoints

#### POST /api/auth/login
**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Login successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123xyz789...",
    "expiresAt": "2024-01-01T12:15:00Z"
  }
}
```

#### POST /api/auth/refresh-token
**Request:**
```json
{
  "refreshToken": "abc123xyz789..."
}
```

**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "xyz789abc123...",
    "expiresAt": "2024-01-01T12:30:00Z"
  }
}
```

#### POST /api/auth/revoke-token (Requires Auth)
**Request:**
```json
{
  "refreshToken": "xyz789abc123..."
}
```

**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Token revoked successfully",
  "data": {}
}
```

## Token Flow

### Initial Login
```
1. User submits credentials
2. Server validates credentials
3. Server generates:
   - Access Token (15 min, JWT)
   - Refresh Token (7 days, random)
4. Server saves refresh token to database
5. Client receives both tokens
6. Client stores:
   - Access Token ? Memory/SessionStorage
   - Refresh Token ? HttpOnly Cookie/SecureStorage
```

### API Request
```
1. Client sends request with Access Token in Authorization header
2. Server validates JWT signature and expiration
3. If valid ? Process request
4. If expired ? Return 401 Unauthorized
5. Client triggers refresh flow
```

### Token Refresh
```
1. Access Token expires (after 15 min)
2. Client detects 401 response
3. Client sends Refresh Token to /api/auth/refresh-token
4. Server:
   a. Validates Refresh Token against database
   b. Checks if token is valid and not expired
   c. Revokes old Refresh Token
   d. Generates new Access Token
   e. Generates new Refresh Token
   f. Saves new Refresh Token to database
5. Client receives new tokens
6. Client retries original request with new Access Token
```

### Logout
```
1. User clicks logout
2. Client sends Refresh Token to /api/auth/revoke-token
3. Server marks token as invalid in database
4. Client clears stored tokens
5. User session ended
```

## Security Features

### 1. Short-Lived Access Tokens
**Why**: Limits damage if token is compromised
```csharp
Expires = DateTime.UtcNow.AddMinutes(15)
```

### 2. Long-Lived Refresh Tokens
**Why**: Better UX without frequent logins
```csharp
var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
```

### 3. Single-Use Refresh Tokens
**Why**: Prevents replay attacks
```csharp
// Revoke old token before issuing new one
await _tokenService.RevokeRefreshTokenAsync(oldRefreshToken);
```

### 4. Database Validation
**Why**: Centralized token control and revocation
```csharp
// Check database, not just signature
var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(...);
```

### 5. JWT ID Tracking
**Why**: Link access tokens to refresh tokens for audit trail
```csharp
new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
```

### 6. Cryptographically Secure Random Tokens
**Why**: Unpredictable, can't be forged
```csharp
using var rng = RandomNumberGenerator.Create();
rng.GetBytes(randomNumber);
```

## Database Schema

**RefreshTokens Table:**
```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(450) NOT NULL,
    JwtTokenId NVARCHAR(MAX) NOT NULL,
    RefreshTokenValue NVARCHAR(MAX) NOT NULL,
    IsValid BIT NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_RefreshTokens_RefreshTokenValue ON RefreshTokens(RefreshTokenValue);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
```

## Client-Side Implementation Guide

### Storage Strategy
```javascript
// Access Token ? SessionStorage (short-lived, cleared on tab close)
sessionStorage.setItem('accessToken', response.accessToken);

// Refresh Token ? HttpOnly Cookie (more secure, sent automatically)
// OR SecureStorage (encrypted local storage)
secureStorage.setItem('refreshToken', response.refreshToken);
```

### Axios Interceptor Example
```javascript
// Request interceptor - Add access token
axios.interceptors.request.use(
    config => {
        const token = sessionStorage.getItem('accessToken');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    }
);

// Response interceptor - Handle token refresh
axios.interceptors.response.use(
    response => response,
    async error => {
        const originalRequest = error.config;
        
        if (error.response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            
            try {
                // Get refresh token
                const refreshToken = secureStorage.getItem('refreshToken');
                
                // Request new tokens
                const response = await axios.post('/api/auth/refresh-token', {
                    refreshToken
                });
                
                // Store new tokens
                sessionStorage.setItem('accessToken', response.data.data.accessToken);
                secureStorage.setItem('refreshToken', response.data.data.refreshToken);
                
                // Retry original request with new token
                originalRequest.headers.Authorization = `Bearer ${response.data.data.accessToken}`;
                return axios(originalRequest);
            } catch (refreshError) {
                // Refresh failed ? Redirect to login
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }
        
        return Promise.reject(error);
    }
);
```

## Testing

### Test Login & Get Tokens
```bash
curl -X POST https://localhost:7297/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123!"
  }'
```

### Test Refresh Token
```bash
curl -X POST https://localhost:7297/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

### Test Revoke Token
```bash
curl -X POST https://localhost:7297/api/auth/revoke-token \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

## Best Practices Implemented

? **Separate Token Types**: Access (JWT) vs Refresh (Random)  
? **Token Rotation**: New tokens on each refresh  
? **Database Validation**: Can't be forged, can be revoked  
? **Short Access Token Lifetime**: Limits exposure  
? **Long Refresh Token Lifetime**: Better UX  
? **Revocation Support**: Logout, security incidents  
? **JWT ID Tracking**: Audit trail  
? **Secure Random Generation**: Cryptographically secure  
? **Expiration Validation**: Both in JWT and database  
? **Single-Use Refresh Tokens**: Prevents replay attacks  

## Common Attack Scenarios & Mitigations

### 1. Stolen Access Token
**Mitigation**: Short lifetime (15 min), expires quickly

### 2. Stolen Refresh Token
**Mitigation**: Database validation, can be revoked

### 3. Token Replay Attack
**Mitigation**: Single-use refresh tokens, old token invalidated

### 4. Session Fixation
**Mitigation**: New tokens generated on refresh, JWT ID tracking

### 5. XSS Attack
**Mitigation**: Store refresh token in HttpOnly cookie, not accessible to JavaScript

### 6. Man-in-the-Middle
**Mitigation**: HTTPS only, ValidateIssuerSigningKey

## Monitoring & Maintenance

### Cleanup Old Tokens
```csharp
// Periodic task to delete expired tokens
public async Task CleanupExpiredTokensAsync()
{
    var expiredTokens = await _db.RefreshTokens
        .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
        .ToListAsync();
        
    _db.RefreshTokens.RemoveRange(expiredTokens);
    await _db.SaveChangesAsync();
}
```

### Revoke All User Tokens (Security Breach)
```csharp
public async Task RevokeAllUserTokensAsync(string userId)
{
    var userTokens = await _db.RefreshTokens
        .Where(rt => rt.UserId == userId && rt.IsValid)
        .ToListAsync();
        
    foreach (var token in userTokens)
    {
        token.IsValid = false;
    }
    
    await _db.SaveChangesAsync();
}
```

## Summary

This implementation provides enterprise-grade refresh token functionality with:

- ? **Security**: Short-lived access tokens, secure refresh tokens
- ? **Revocation**: Database-backed, can invalidate tokens
- ? **Rotation**: New tokens on each refresh
- ? **Tracking**: JWT IDs for audit trail
- ? **Validation**: Multiple layers of verification
- ? **Best Practices**: Industry-standard implementation

**Ready for production use!** ???
