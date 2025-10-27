# ?? CancellationToken Demonstration Guide

## What is CancellationToken?

A `CancellationToken` is a mechanism in .NET that allows you to signal that an operation should be cancelled. It's essential for:
- **Resource Management** - Stop work that's no longer needed
- **Responsiveness** - Free up server resources immediately
- **Cost Savings** - Don't waste CPU/DB time on abandoned requests
- **User Experience** - Handle client disconnections gracefully

---

## ?? Problem: WITHOUT CancellationToken

### What Happens?
```csharp
public async Task<IActionResult> SearchWithoutCancellation()
{
    // User clicks search
    await Task.Delay(5000); // Long operation
    
    // User navigates away (client disconnects)
    // ? BUT THE SERVER KEEPS WORKING!
    
    await _db.Villas.ToListAsync(); // Still executes!
    
    // ? Wastes: CPU time, database connections, memory
    return Ok(results); // Nobody is listening!
}
```

### Problems:
- ? Server continues processing even after client disconnects
- ? Wastes database connections
- ? Wastes CPU and memory
- ? Can't implement timeouts
- ? Reduces server capacity for real requests

---

## ? Solution: WITH CancellationToken

### How It Works:
```csharp
public async Task<IActionResult> SearchWithCancellation(CancellationToken cancellationToken)
{
    // User clicks search
    await Task.Delay(5000, cancellationToken); // ? Pass token
    
    // User navigates away (client disconnects)
    // ? TOKEN GETS CANCELLED!
    
    cancellationToken.ThrowIfCancellationRequested(); // ? Check
    
    await _db.Villas.ToListAsync(cancellationToken); // ? Pass to EF Core
    
    // ? Throws OperationCanceledException
    // ? Resources freed immediately!
}
```

### Benefits:
- ? Stops immediately when client disconnects
- ? Frees database connections instantly
- ? Saves CPU and memory
- ? Can implement custom timeouts
- ? Better server capacity utilization

---

## ?? Performance Comparison

| Scenario | Without CancellationToken | With CancellationToken |
|----------|---------------------------|------------------------|
| **Client disconnects after 2s** | Continues for 10s more | Stops at 2s |
| **Database connections** | Held until completion | Released at 2s |
| **CPU usage** | 100% for full duration | 0% after cancellation |
| **Memory** | Held until GC | Released immediately |
| **Cost (cloud)** | Full operation cost | Only 20% of cost |

---

## ?? Test Scenarios in the Demo

### 1. WITHOUT Cancellation Token (Bad Practice)
**Endpoint:** `GET /api/v2/villa-search/without-cancellation`

**What to test:**
```bash
# Start the request
curl "https://localhost:7297/api/v2/villa-search/without-cancellation?searchTerm=villa" -H "Authorization: Bearer YOUR_TOKEN"

# Cancel it (Ctrl+C) after 2 seconds
# ? Check the server logs - it keeps running!
# ? Full 6+ seconds of processing even though you cancelled
```

**Expected Output:**
```
?? WITHOUT CancellationToken - Search started at 2024-01-15 10:00:00
   Applying search term filter...
   Calculating statistics...
?? WITHOUT CancellationToken - Search COMPLETED after 6234ms (even if client disconnected!)
```

### 2. WITH Cancellation Token (Best Practice) ?
**Endpoint:** `GET /api/v2/villa-search/with-cancellation`

**What to test:**
```bash
# Start the request
curl "https://localhost:7297/api/v2/villa-search/with-cancellation?searchTerm=villa" -H "Authorization: Bearer YOUR_TOKEN"

# Cancel it (Ctrl+C) after 2 seconds
# ? Check the server logs - it stops immediately!
# ? Only ~2 seconds of processing
```

**Expected Output:**
```
? WITH CancellationToken - Search started at 2024-01-15 10:00:00
   Applying search term filter...
   Applying min rate filter...
? WITH CancellationToken - Search CANCELLED after 2156ms (saved resources!)
```

### 3. WITH Timeout (Automatic Cancellation)
**Endpoint:** `GET /api/v2/villa-search/with-timeout?timeoutSeconds=5`

**What to test:**
```bash
# Request with 5 second timeout
curl "https://localhost:7297/api/v2/villa-search/with-timeout?searchTerm=villa&timeoutSeconds=5"

# The operation takes 10 seconds
# ? Automatically cancels at 5 seconds
```

**Expected Output:**
```
?? Search with 5s timeout started
   Starting long operation...
?? Search TIMED OUT after 5002ms
Response: 408 Request Timeout
```

### 4. WITH Linked Cancellation (Multiple Sources)
**Endpoint:** `GET /api/v2/villa-search/with-linked-cancellation`

**What to test:**
```bash
# Request that can be cancelled by multiple sources
curl "https://localhost:7297/api/v2/villa-search/with-linked-cancellation?searchTerm=villa"

# Can be cancelled by:
# 1. Client disconnect (Ctrl+C)
# 2. 8 second timeout (automatic)
# 3. Manual trigger (programmatic)
```

**Expected Output:**
```
?? Search with LINKED cancellation started
   This operation can be cancelled by:
   1. Client disconnect (HTTP context)
   2. 8 second timeout
   3. Manual trigger
?? Search cancelled after 8001ms. Reason: 8 second timeout exceeded
```

---

## ?? Testing Instructions

### Step 1: Start the API
```bash
cd RoyalVilla_API
dotnet run
```

### Step 2: Get Authentication Token
```bash
# Login to get token
curl -X POST "https://localhost:7297/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123!"}'

# Copy the accessToken from response
```

### Step 3: Test WITHOUT CancellationToken
```bash
# Open a terminal
curl "https://localhost:7297/api/v2/villa-search/without-cancellation?searchTerm=villa&minRate=100" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# After 2 seconds, press Ctrl+C to cancel
# Watch the API console logs - it keeps running!
```

### Step 4: Test WITH CancellationToken
```bash
# Open a terminal
curl "https://localhost:7297/api/v2/villa-search/with-cancellation?searchTerm=villa&minRate=100" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# After 2 seconds, press Ctrl+C to cancel
# Watch the API console logs - it stops immediately!
```

### Step 5: Test Timeout
```bash
# Request with 3 second timeout (operation takes 10 seconds)
curl "https://localhost:7297/api/v2/villa-search/with-timeout?searchTerm=villa&timeoutSeconds=3" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Will automatically timeout after 3 seconds
```

---

## ?? Real-World Impact

### Scenario: 100 concurrent users

#### Without CancellationToken:
```
50 users navigate away after 2 seconds
? Server still processes all 50 for 10 seconds each
? 500 seconds of wasted CPU time
? 50 database connections held
? Memory usage: HIGH
? Cost: $$$
```

#### With CancellationToken:
```
50 users navigate away after 2 seconds
? Server stops immediately for those 50
? 100 seconds saved (80% reduction!)
? Database connections released instantly
? Memory usage: LOW
? Cost: $
```

### Cost Savings Example (Azure):
```
Without CancellationToken:
- 10,000 abandoned requests/day
- Average 8 seconds wasted per request
- 80,000 seconds = 22 hours of compute
- Cost: ~$50/month

With CancellationToken:
- Same 10,000 requests
- Average 1 second before cancel
- 10,000 seconds = 2.7 hours of compute
- Cost: ~$6/month
- SAVINGS: $44/month (88% reduction!)
```

---

## ?? Best Practices

### ? DO:
1. **Always accept CancellationToken in async methods**
   ```csharp
   public async Task<IActionResult> MyAction(CancellationToken cancellationToken)
   ```

2. **Pass it to all async operations**
   ```csharp
   await Task.Delay(1000, cancellationToken);
   await _db.Villas.ToListAsync(cancellationToken);
   await httpClient.GetAsync(url, cancellationToken);
   ```

3. **Check for cancellation in long loops**
   ```csharp
   foreach (var item in largeList)
   {
       cancellationToken.ThrowIfCancellationRequested();
       // Process item
   }
   ```

4. **Use linked tokens for multiple sources**
   ```csharp
   using var cts = CancellationTokenSource.CreateLinkedTokenSource(
       httpContextToken, 
       timeoutToken
   );
   ```

### ? DON'T:
1. **Don't ignore cancellation tokens**
   ```csharp
   // ? BAD
   public async Task<IActionResult> MyAction(CancellationToken cancellationToken)
   {
       await Task.Delay(1000); // Missing cancellationToken!
   }
   ```

2. **Don't swallow OperationCanceledException**
   ```csharp
   // ? BAD
   try
   {
       await DoWork(cancellationToken);
   }
   catch (OperationCanceledException)
   {
       // Ignoring it - bad practice!
   }
   ```

3. **Don't forget to dispose CancellationTokenSource**
   ```csharp
   // ? GOOD
   using var cts = new CancellationTokenSource();
   
   // ? BAD
   var cts = new CancellationTokenSource(); // Memory leak!
   ```

---

## ?? How to See It in Action

### Visual Test with Browser:

1. **Open Scalar/Swagger:**
   - Navigate to `https://localhost:7297/scalar/v1`
   - Login to get token

2. **Test WITHOUT CancellationToken:**
   - Call `/api/v2/villa-search/without-cancellation?searchTerm=villa`
   - After 2 seconds, close the browser tab or cancel request
   - Watch API console - it keeps running for 6+ seconds!
   - **Log shows:** `?? WITHOUT CancellationToken - Search COMPLETED after 6234ms (even if client disconnected!)`

3. **Test WITH CancellationToken:**
   - Call `/api/v2/villa-search/with-cancellation?searchTerm=villa`
   - After 2 seconds, close the browser tab or cancel request
   - Watch API console - it stops immediately!
   - **Log shows:** `? WITH CancellationToken - Search CANCELLED after 2156ms (saved resources!)`

### Watch the difference:
```
WITHOUT Token:
[10:00:00] Start
[10:00:02] User cancels ? (but server doesn't know)
[10:00:03] Still processing...
[10:00:04] Still processing...
[10:00:05] Still processing...
[10:00:06] Finally done! (nobody listening)

WITH Token:
[10:00:00] Start
[10:00:02] User cancels ?
[10:00:02] Stopped immediately!
```

---

## ?? Key Takeaways

1. **CancellationToken is free** - No performance overhead, only benefits
2. **Saves money** - Less compute time in cloud environments
3. **Better UX** - Server responds to user actions
4. **Resource efficiency** - Database connections, memory freed immediately
5. **Scalability** - Handle more concurrent users
6. **Required for production** - Not optional for long-running operations

---

## ?? Quick Quiz

**Q: When should you use CancellationToken?**
A: ? ALWAYS in async methods, especially:
- Database queries
- HTTP calls
- File I/O operations
- Long computations
- Any operation > 100ms

**Q: What happens if you don't use it?**
A: Server wastes resources processing abandoned requests

**Q: Does it add overhead?**
A: No! It's a lightweight struct with near-zero cost

**Q: Can I create my own timeouts?**
A: Yes! Use `new CancellationTokenSource(TimeSpan.FromSeconds(30))`

---

## ?? Next Steps

1. Run the demo endpoints
2. Watch the console logs
3. Compare the behavior
4. Add CancellationToken to your existing endpoints
5. Measure the improvement in resource usage

**Remember:** Adding CancellationToken is one of the easiest ways to improve your API's performance and cost efficiency! ??
