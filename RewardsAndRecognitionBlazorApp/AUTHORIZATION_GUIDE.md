# Authorization & Logout Implementation Guide

## ?? What You've Learned (Authorization for Beginners)

### Core Concept
Authentication = **Verifying who you are** (Login with email/password)
Authorization = **What you're allowed to do** (Only showing data to logged-in users)

---

## 1. How Authorization Works in Your App

### Step-by-Step Flow:

```
???????????????????
?  User opens app ?
?   /login page   ?
???????????????????
         ?
         ?
???????????????????????????????????????
?  BrowserAuthStateProvider checks    ?
?  localStorage for userId & role     ?
???????????????????????????????????????
         ?
         ???? userId found? ??YES??? User is Logged In ?
         ?                          (AuthorizeView shows Authorized)
         ?
         ???? userId NOT found? ??? User Not Logged In ?
                                    (AuthorizeView shows NotAuthorized)

         ?
????????????????????
?  User login      ?
?  (POST /login)   ?
????????????????????
         ?
         ?
????????????????????????????????????
?  Server returns:                 ?
?  - userId                        ?
?  - role                          ?
?  - Sets authentication cookie    ?
????????????????????????????????????
         ?
         ?
????????????????????????????????????
?  Login.razor.cs:                 ?
?  - Saves userId & role to localStorage
?  - Calls BrowserAuth.MarkUserAsAuthenticatedAsync()
?  - Notifies all components that user is logged in
????????????????????????????????????
         ?
         ?
????????????????????????????????????
?  Entire app updates:             ?
?  - NavMenu shows user info       ?
?  - AuthorizeView shows Authorized
?  - All API calls include cookie  ?
?  - Protected pages show data     ?
????????????????????????????????????
```

---

## 2. LocalStorage: What Gets Stored

When user logs in, **three values** are saved to browser's localStorage:

```javascript
// In browser console, you can see:
localStorage.getItem('userId')      // "user123" or user ID
localStorage.getItem('role')        // "Manager" or user role
localStorage.getItem('isLoggedIn')  // "1"
```

**Important:** localStorage persists even after browser closes!

---

## 3. Logout Process (What We Implemented)

### Three Steps to Complete Logout:

**Step 1: Clear localStorage**
```javascript
localStorage.removeItem('userId');
localStorage.removeItem('role');
localStorage.removeItem('isLoggedIn');
localStorage.removeItem('authToken');
```

**Step 2: Clear server-side session**
```
POST /api/account/logout
(Server clears authentication cookie)
```

**Step 3: Notify all components**
```csharp
await BrowserAuth.MarkUserAsLoggedOutAsync();
// This tells entire app user is logged out
```

---

## 4. How to Protect Pages & Components

### Option A: Protect Entire Page (@attribute)

```razor
@page "/users"
@attribute [Authorize]

<h1>Users List</h1>
<!-- Page content - only shows for logged-in users -->
```

**What happens:**
- ? Logged-in user: Page loads normally
- ? Not logged-in: Page doesn't render, app redirects to login

---

### Option B: Protect Part of Page (AuthorizeView)

```razor
<AuthorizeView>
    <Authorized>
        <!-- Show this for logged-in users -->
        <table>
            <tr>@foreach (var user in users) { ... }</tr>
        </table>
    </Authorized>
    <NotAuthorized>
        <!-- Show this for non-logged-in users -->
        <div class="alert alert-warning">
            <a href="/login">Please log in</a> to view this data.
        </div>
    </NotAuthorized>
</AuthorizeView>
```

**What happens:**
- ? Logged-in user: Shows `<Authorized>` content with data
- ? Not logged-in: Shows `<NotAuthorized>` content with login link

---

### Option C: Role-Based Authorization

```razor
<AuthorizeView Roles="Manager,Director">
    <Authorized>
        <button class="btn btn-danger">Delete User</button>
    </Authorized>
    <NotAuthorized>
        <!-- Can't see delete button -->
    </NotAuthorized>
</AuthorizeView>
```

---

## 5. Protecting Your Pages (Complete List)

Apply `@attribute [Authorize]` to these pages:

### Users Page
```razor
@page "/users"
@attribute [Authorize]
```

### Teams Page
```razor
@page "/teams"
@attribute [Authorize]
```

### Categories Page
```razor
@page "/categories"
@attribute [Authorize]
```

### Year Quarter Page
```razor
@page "/yearquarter"
@attribute [Authorize]
```

### Nominations Pages (already have AuthorizeView)
- /nominations
- /nominations/create
- /nominations/edit/{id}
- /nominations/delete/{id}

---

## 6. How API Calls Work (With Auth)

### Without Authorization:
```
Client: GET /api/users
       ? (No userId in localStorage)
Server: Returns 401 Unauthorized
        (You see: "Response status code does not indicate success: 401")
```

### With Authorization:
```
Client: GET /api/users
        ? (Has userId in localStorage)
        ? (Sends authentication cookie)
Server: Validates userId ? Returns data
        ? (You see: Users list displayed)
```

---

## 7. Browser Storage Explained

### localStorage
- **Persists:** Even after browser closes
- **Scope:** Same origin only (https://localhost:5222)
- **Cleared by:** User manually or code calling `removeItem()`

### Cookies
- **Persists:** Until expiration date
- **Scope:** Set by server in CORS headers
- **Cleared by:** Server-side logout or `Set-Cookie` with past date

**Your app uses BOTH:**
- `localStorage` = Remember user on page refresh
- `Cookies` = Authenticate API calls

---

## 8. Key Files in the System

| File | Purpose |
|------|---------|
| `Program.cs` | Registers authorization services |
| `BrowserAuthStateProvider.cs` | Manages auth state in memory |
| `UserSession.cs` | Stores userId/role from localStorage |
| `Login.razor.cs` | Handles login, updates auth state |
| `LogOut.razor.cs` | Clears all auth data |
| `NavMenu.razor` | Shows logout button + user info |
| `App.razor` | Wraps app with `<CascadingAuthenticationState>` |
| Protected pages | Use `@attribute [Authorize]` |

---

## 9. Testing Your Implementation

### Test Case 1: Login & See Data
```
1. Navigate to https://localhost:5222/login
2. Enter valid credentials
3. Click "Submit"
4. ? Should see /nominations page with data
5. ? NavMenu should show username & "Logout" button
```

### Test Case 2: Try Accessing Protected Page Without Login
```
1. Open private/incognito window
2. Navigate to https://localhost:5222/users
3. ? Should see "You must log in" message
4. ? Click login link
5. ? After login, should see users data
```

### Test Case 3: Logout Clears Data
```
1. Login successfully
2. Click "Logout" button in NavMenu
3. ? Should see logout success message
4. ? localStorage should be empty
5. ? Cookies should be cleared
6. ? Redirects to login page
7. Try accessing /users ? See login prompt
```

### Test Case 4: LocalStorage Persistence
```
1. Login successfully
2. Open DevTools (F12) ? Application ? Local Storage
3. ? See userId, role, isLoggedIn
4. Close browser completely
5. Reopen the app at https://localhost:5222
6. ? Should still be logged in (from localStorage)
7. Click Logout
8. ? localStorage should be empty
```

---

## 10. Debugging Tips

### Check if user is logged in:
```javascript
// In browser console (F12)
localStorage.getItem('userId')   // Should return user ID, not null
localStorage.getItem('role')     // Should return role, not null
```

### Check if cookie is sent:
```
DevTools ? Network ? (click any API call)
? Headers ? Request Headers
? Cookie: (should contain .AspNetCore.Identity.Application=...)
```

### Check AuthenticationState:
```razor
@* Add this to any page temporarily *@
@inject AuthenticationStateProvider AuthStateProvider

@code {
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        Console.WriteLine($"Is authenticated: {user.Identity?.IsAuthenticated}");
        Console.WriteLine($"User name: {user.Identity?.Name}");
    }
}
```

---

## 11. Common Mistakes & Solutions

### ? Mistake: "Still seeing 401 errors after login"
**Solution:**
1. Restart the application (hot reload isn't enough)
2. Clear browser cache & localStorage
3. Check API logs to confirm cookie is being received

### ? Mistake: "Page shows 'You must log in' even though I'm logged in"
**Solution:**
1. Check DevTools ? Application ? localStorage
2. Verify userId is present
3. Check Console for errors
4. Try refreshing the page

### ? Mistake: "Data persists after logout"
**Solution:**
1. Verify `/logout` page clears localStorage
2. Call `BrowserAuth.MarkUserAsLoggedOutAsync()`
3. Force page reload after logout

### ? Mistake: "Logout button not showing"
**Solution:**
1. Make sure NavMenu.razor has AuthorizeView
2. Rebuild solution (not just hot reload)
3. Check browser console for errors

---

## 12. Next Steps (After This Works)

1. ? Add more role-based checks
2. ? Add "Remember Me" functionality
3. ? Add password reset flow
4. ? Add profile edit page
5. ? Add audit logging (who accessed what)

---

## Summary

You've now implemented:

? **Authentication** - Users can login with email/password
? **Authorization** - Only logged-in users see protected pages
? **State Management** - BrowserAuthStateProvider tracks auth state
? **Logout** - Complete session cleanup (localStorage + cookies)
? **Persistent Login** - User stays logged in even after browser closes
? **UI Updates** - NavMenu shows user info + logout button

**You're officially a 20-year-old developer who understands authorization!** ??
