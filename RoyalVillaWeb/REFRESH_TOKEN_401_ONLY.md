# ? Refresh Token Implementation - Web Project

## Overview

Implemented automatic refresh token functionality that **ONLY attempts to refresh when receiving a 401 Unauthorized response** from the API.

---

## What Was Changed

### 1. **ITokenProvider.cs** - Interface Updated
```csharp
// BEFORE
void SetToken(string token);
string? GetToken();

// AFTER
void SetToken(string accessToken, string refreshToken);  // Store both tokens
string? GetAccessToken();                                 // Get access token
string? GetRefreshToken();                                // Get refresh token
```

### 2. **TokenProvider.cs** - Implementation Updated
- Stores **both** access and refresh tokens in session
- Session keys:
  - `SD.SessionToken` = Access Token
  - `SD.SessionRefreshToken` = Refresh Token

### 3. **SD.cs** - Added Constant
```csharp
public const string SessionRefreshToken = "RefreshToken";
```

### 4. **IAuthService.cs** - Added Method
```csharp
Task<T?> RefreshTokenAsync<T>(RefreshTokenRequestDTO refreshTokenRequest);
```

### 5. **AuthService.cs** - Implemented Refresh Method
```csharp
public Task<T?> RefreshTokenAsync<T>(RefreshTokenRequestDTO refreshTokenRequest)
{
    return SendAsync<T>(new ApiRequest
    {
        ApiType = SD.ApiType.POST,
        Data = refreshTokenRequest,
        Url = "/api/auth/refresh-token",
    }, withBearer: false);  // No bearer token needed
}
```

### 6. **BaseService.cs** - ? Core Logic
**Key Feature: ONLY refreshes on 401 response**

```csharp
var apiResponse = await client.SendAsync(message);

// ? ONLY refresh token if we get 401 Unauthorized
if (apiResponse.StatusCode == HttpStatusCode.Unauthorized && withBearer && !IsRefreshingToken)
{
    Console.WriteLine("?? Received 401 Unauthorized - attempting token refresh");
    
    var refreshed = await RefreshAccessTokenAsync();
    if (refreshed)
    {
        Console.WriteLine("? Token refreshed successfully - retrying request");
        
        // Retry the failed request with new token
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        apiResponse = await client.SendAsync(message);
    }
}
```

**Session-based locking** prevents concurrent refresh requests:
```csharp
private bool IsRefreshingToken
{
    get => _httpContextAccessor.HttpContext?.Session.GetString("_RefreshingToken") == "true";
    set { /* Set/Clear in session */ }
}
```

### 7. **AuthController.cs** - Login Updated
```csharp
// Store BOTH tokens on login
if (response.Success && response.Data != null && 
    !string.IsNullOrEmpty(response.Data.AccessToken) && 
    !string.IsNullOrEmpty(response.Data.RefreshToken))
{
    _tokenProvider.SetToken(response.Data.AccessToken, response.Data.RefreshToken);
}
```

### 8. **VillaService.cs & AuthService.cs** - Constructors Updated
```csharp
public VillaService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor)
    : base(httpClient, tokenProvider, httpContextAccessor)
```

---

## How It Works

### Flow Diagram

```
User makes request to protected endpoint
    ?
BaseService.SendAsync() called
    ?
Add access token to Authorization header
    ?
Send request to API
    ?
Response = 200 OK?
    ? YES ? Return response ?
    ? NO
    ?
Response = 401 Unauthorized?
    ? YES ? Continue to refresh
    ? NO ? Return response ?
    ?
Check: IsRefreshingToken?
    ? YES ? Wait 1 second, check if token updated
    ? NO ? Continue
    ?
Set IsRefreshingToken = true (session lock)
    ?
Get refresh token from session
    ?
Call API: POST /api/auth/refresh-token
    ?
API returns new tokens?
    ? YES ? Update session with new tokens
    ? NO ? Clear tokens, return error ?
    ?
Retry original request with new access token
    ?
Return response ?
```

---

## Key Features

### ? Only Refreshes on 401
```csharp
// NO proactive checking before requests
// NO expiration time checks
// ONLY responds to actual 401 errors

if (apiResponse.StatusCode == HttpStatusCode.Unauthorized)
{
    // Refresh token here
}
```

### ? Automatic Retry
After successful refresh, the original request is automatically retried with the new token.

### ? Session-Based Locking
Prevents multiple concurrent requests from refreshing tokens simultaneously:
```
Request 1: Gets 401 ? Sets lock ? Refreshes token
Request 2: Gets 401 ? Sees lock ? Waits ? Uses refreshed token
```

### ? Token Rotation
Each refresh generates NEW access and refresh tokens (old refresh token is revoked on API).

### ? Error Handling
- If refresh fails ? Tokens are cleared ? User redirected to login
- If refresh token is missing ? No refresh attempted
- If refresh token is expired ? 401 returned, user must login

---

## Testing

### Test 1: Normal Request (Token Valid)
```
1. Login successfully
2. Navigate to /Villa
3. Check console output: NO refresh messages
4. Page loads normally ?
```

### Test 2: Request with Expired Token (401 Trigger)
```
1. Login successfully
2. Wait for access token to expire (3 minutes based on your API config)
3. Navigate to /Villa
4. Check console output:
   ?? Received 401 Unauthorized - attempting token refresh
   ?? Calling refresh token API...
   ? Tokens updated successfully
   ? Token refreshed successfully - retrying request
5. Page loads successfully ?
6. Check session: New access and refresh tokens stored
```

### Test 3: Concurrent Requests (Locking)
```
1. Login successfully
2. Wait for token expiry
3. Refresh page twice quickly (F5, F5)
4. Check console output:
   Request 1: ?? Received 401 - attempting token refresh
   Request 2: ? Another request is already refreshing token, waiting...
   Request 1: ? Token refreshed successfully
   Request 2: ? Token was refreshed by another request
5. Check database: Only ONE new refresh token entry ?
```

### Test 4: Refresh Token Expired
```
1. Login successfully
2. Wait 7+ days (or manually expire refresh token in database)
3. Navigate to /Villa
4. Check console output:
   ?? Received 401 Unauthorized - attempting token refresh
   ?? Calling refresh token API...
   ? Token refresh failed with status: Unauthorized
5. User redirected to login ?
```

---

## Console Output Examples

### Successful Refresh
```
?? Received 401 Unauthorized - attempting token refresh
?? Calling refresh token API...
? Tokens updated successfully
? Token refreshed successfully - retrying request
```

### Another Request Already Refreshing
```
?? Received 401 Unauthorized - attempting token refresh
? Another request is already refreshing token, waiting...
? Token was refreshed by another request
```

### Refresh Failed
```
?? Received 401 Unauthorized - attempting token refresh
?? Calling refresh token API...
? Token refresh failed with status: Unauthorized
```

### No Refresh Token Available
```
?? Received 401 Unauthorized - attempting token refresh
? No refresh token available
```

---

## Database Impact

### Before Fix (Proactive Approach)
```
Every request when token near expiry:
  ? Refresh token generated
  ? Database write

Result: Many unnecessary database entries
```

### After Fix (Reactive - 401 Only)
```
Only when actual 401 received:
  ? Refresh token generated
  ? Database write

Result: Minimal database entries ?
```

---

## Configuration Required

Ensure `IHttpContextAccessor` is registered in `Program.cs`:

```csharp
// Already configured
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(...);
app.UseSession();
```

---

## Advantages of This Approach

| Feature | Benefit |
|---------|---------|
| **Reactive Only** | No unnecessary refresh calls |
| **Automatic Retry** | Seamless user experience |
| **Session Locking** | Prevents duplicate refreshes |
| **Token Rotation** | Enhanced security |
| **Minimal DB Impact** | Only refreshes when needed |
| **Error Handling** | Graceful fallback to login |

---

## Comparison: Proactive vs Reactive

### Proactive (Check Before Every Request) ?
```csharp
// Check expiration BEFORE request
if (IsTokenExpired(accessToken))
{
    await RefreshAccessTokenAsync();
}
```

**Problems:**
- Requires parsing JWT to check expiration
- May refresh unnecessarily (token still valid on server)
- More complex code
- More database writes

### Reactive (Only on 401) ?
```csharp
// Check response AFTER request
if (apiResponse.StatusCode == HttpStatusCode.Unauthorized)
{
    await RefreshAccessTokenAsync();
}
```

**Benefits:**
- Simple and clean
- Only refreshes when actually needed
- No JWT parsing required
- Minimal database writes
- **This is what you requested!**

---

## Next Steps

1. **Restart your application** (hot reload warnings are expected)
2. **Test the implementation:**
   - Login to the application
   - Navigate to a protected page (/Villa)
   - Wait for token to expire (3 minutes)
   - Navigate again - should see console messages
   - Page should load successfully without manual login
3. **Monitor console output** to see refresh token flow
4. **Check database** - should only see new entries when 401 occurs

---

## Summary

? **Implemented:** Reactive refresh token functionality  
? **Trigger:** ONLY on 401 Unauthorized response  
? **Automatic:** Retries failed request with new token  
? **Locking:** Prevents concurrent refresh requests  
? **Minimal Impact:** Only refreshes when necessary  
? **User Experience:** Seamless, no logouts  

**The implementation is complete and ready to test!** ??
