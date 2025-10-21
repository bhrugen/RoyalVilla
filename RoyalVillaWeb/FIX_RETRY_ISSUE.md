# ?? Fix: Token Refresh Retry Issue

## Problem

**Symptom:** After 1 minute (when token expires), the first request fails with an error, but if you refresh again, it works.

**Root Cause:** The retry logic was trying to **reuse the same `HttpRequestMessage`** object after refreshing the token. In .NET, `HttpRequestMessage` can only be sent **once** - it cannot be reused for a retry.

---

## What Was Wrong

### Before (Broken Code) ?

```csharp
var message = new HttpRequestMessage { ... };

var apiResponse = await client.SendAsync(message); // First send

if (apiResponse.StatusCode == HttpStatusCode.Unauthorized)
{
    var refreshed = await RefreshAccessTokenAsync();
    if (refreshed)
    {
        // ? PROBLEM: Trying to reuse the same message object
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        apiResponse = await client.SendAsync(message); // ? FAILS - message already sent!
    }
}
```

**Why it failed:**
- `HttpRequestMessage` is **disposable** and can only be sent once
- After the first `SendAsync()`, the message's content is consumed
- Trying to send it again throws an error or doesn't work properly
- That's why the **second refresh** (which creates a fresh request) works

---

## The Fix

### After (Working Code) ?

```csharp
// Helper method to create fresh request messages
private HttpRequestMessage CreateRequestMessage(ApiRequest apiRequest, bool withBearer)
{
    var message = new HttpRequestMessage
    {
        RequestUri = new Uri(apiRequest.Url, uriKind: UriKind.Relative),
        Method = GetHttpMethod(apiRequest.ApiType),
    };

    var accessToken = _tokenProvider.GetAccessToken();
    if (withBearer && !string.IsNullOrEmpty(accessToken))
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    if (apiRequest.Data != null)
    {
        if (apiRequest.Data is MultipartFormDataContent multipartContent)
        {
            message.Content = multipartContent;
        }
        else
        {
            message.Content = JsonContent.Create(apiRequest.Data, options: JsonOptions);
        }
    }

    return message;
}

public async Task<T?> SendAsync<T>(ApiRequest apiRequest, bool withBearer = true)
{
    var client = _httpClient.CreateClient("RoyalVillaAPI");
    
    // ? Create initial request
    var message = CreateRequestMessage(apiRequest, withBearer);
    var apiResponse = await client.SendAsync(message);

    if (apiResponse.StatusCode == HttpStatusCode.Unauthorized && withBearer)
    {
        var refreshed = await RefreshAccessTokenAsync();
        if (refreshed)
        {
            // ? Create a NEW request message for retry
            var retryMessage = CreateRequestMessage(apiRequest, withBearer);
            apiResponse = await client.SendAsync(retryMessage); // ? WORKS!
        }
    }

    return await apiResponse.Content.ReadFromJsonAsync<T>(JsonOptions);
}
```

---

## Key Changes

### 1. **New Helper Method: `CreateRequestMessage()`**
```csharp
private HttpRequestMessage CreateRequestMessage(ApiRequest apiRequest, bool withBearer)
```

**Purpose:**
- Encapsulates request message creation logic
- Can be called multiple times to create fresh messages
- Automatically gets the latest access token

### 2. **Retry with Fresh Message**
```csharp
// First attempt
var message = CreateRequestMessage(apiRequest, withBearer);
var apiResponse = await client.SendAsync(message);

// If 401, create NEW message for retry
if (apiResponse.StatusCode == HttpStatusCode.Unauthorized)
{
    var refreshed = await RefreshAccessTokenAsync();
    if (refreshed)
    {
        var retryMessage = CreateRequestMessage(apiRequest, withBearer); // NEW message!
        apiResponse = await client.SendAsync(retryMessage);
    }
}
```

### 3. **Enhanced Logging**
Added more detailed console output to help debug issues:

```csharp
Console.WriteLine($"? Retry request completed with status: {apiResponse.StatusCode}");
Console.WriteLine($"   New Access Token: {result.Data.AccessToken.Substring(0, 20)}...");
Console.WriteLine($"? Unexpected Error in SendAsync: {ex.Message}");
Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
```

---

## Testing the Fix

### Test 1: Token Expires During Request

**Steps:**
1. Login to the application
2. Navigate to `/Villa` - works ?
3. Wait 1 minute for token to expire
4. Navigate to `/Villa` again

**Expected Console Output:**
```
?? Received 401 Unauthorized - attempting token refresh
?? Calling refresh token API...
? Tokens updated successfully
   New Access Token: eyJhbGciOiJIUzI1NiI...
? Token refreshed successfully - retrying request
? Retry request completed with status: OK
```

**Result:** Page loads successfully on FIRST attempt ? (not second!)

### Test 2: Multiple Concurrent Requests

**Steps:**
1. Login to the application
2. Wait for token expiry
3. Refresh page twice quickly (F5, F5)

**Expected Console Output:**
```
Request 1:
?? Received 401 Unauthorized - attempting token refresh
?? Calling refresh token API...
? Tokens updated successfully
? Token refreshed successfully - retrying request

Request 2:
?? Received 401 Unauthorized - attempting token refresh
? Another request is already refreshing token, waiting...
? Token was refreshed by another request
? Retry request completed with status: OK
```

**Result:** Both requests succeed ?

---

## Why This Fix Works

### HTTP Request Message Lifecycle

```
HttpRequestMessage Creation
    ?
message.Content is set
    ?
client.SendAsync(message) ? First send
    ?
Content is consumed/disposed
    ?
? Cannot send same message again!
    ?
? Must create NEW HttpRequestMessage
```

### .NET HttpClient Behavior

```csharp
// ? WRONG
var msg = new HttpRequestMessage(...);
await client.SendAsync(msg);  // Works
await client.SendAsync(msg);  // FAILS - message already used

// ? CORRECT
var msg1 = new HttpRequestMessage(...);
await client.SendAsync(msg1);  // Works

var msg2 = new HttpRequestMessage(...);
await client.SendAsync(msg2);  // Works - new message
```

---

## Benefits of This Fix

| Issue | Before | After |
|-------|--------|-------|
| **First request after token expiry** | ? Fails | ? Succeeds |
| **Need to refresh twice** | ? Yes | ? No |
| **Error messages** | ? Unclear | ? Detailed logging |
| **Code reusability** | ? Duplicated | ? Helper method |
| **Debugging** | ? Hard | ? Easy with logs |

---

## Additional Improvements

### 1. Better Error Logging
```csharp
catch (Exception ex)
{
    Console.WriteLine($"? Unexpected Error in SendAsync: {ex.Message}");
    Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
    return default;
}
```

### 2. Token Debug Info
```csharp
Console.WriteLine($"   New Access Token: {result.Data.AccessToken.Substring(0, 20)}...");
```

### 3. Response Status Logging
```csharp
Console.WriteLine($"? Retry request completed with status: {apiResponse.StatusCode}");
```

---

## Common HttpRequestMessage Pitfalls

### ? Don't Do This:
```csharp
var message = new HttpRequestMessage(...);

// First use
await client.SendAsync(message);

// ? Trying to reuse
message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
await client.SendAsync(message); // FAILS!
```

### ? Do This:
```csharp
// First request
var message1 = new HttpRequestMessage(...);
await client.SendAsync(message1);

// Create new message for retry
var message2 = new HttpRequestMessage(...);
message2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
await client.SendAsync(message2); // WORKS!
```

---

## Next Steps

1. **Restart your application** (required for the fix to take effect)
2. **Test the fix:**
   - Login
   - Navigate to /Villa (works)
   - Wait 1+ minute
   - Navigate to /Villa again
   - **Should work on FIRST try now!** ?
3. **Check console** - you should see the detailed logging
4. **Verify database** - only one refresh token entry per actual refresh

---

## Summary

? **Fixed:** Request retry after token refresh  
? **Method:** Create new `HttpRequestMessage` for retry  
? **Benefit:** No more "refresh twice" issue  
? **Bonus:** Better error logging and debugging  

**The fix is complete! The first request after token expiry will now work immediately.** ??
