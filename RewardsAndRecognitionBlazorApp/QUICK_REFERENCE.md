# Quick Reference: Protecting Pages with Authorization

## Copy-Paste Solutions

### 1. Protect Entire Page (Easiest)

Add this at the TOP of your Razor page:

```razor
@page "/your-page-route"
@attribute [Authorize]

<h1>Your Page Title</h1>
<!-- Your content here -->
```

**That's it!** Only logged-in users can access this page.

---

### 2. Protect Part of Page (More Flexible)

Wrap the content you want to protect:

```razor
@page "/your-page-route"

<h1>Your Page Title</h1>

<AuthorizeView>
    <Authorized>
        <!-- THIS SHOWS ONLY FOR LOGGED-IN USERS -->
        <button>Delete</button>
        <table>
            @foreach (var item in items)
            {
                <tr>...</tr>
            }
        </table>
    </Authorized>
    <NotAuthorized>
        <!-- THIS SHOWS FOR NON-LOGGED-IN USERS -->
        <div class="alert alert-warning">
            Please <a href="/login">log in</a> to see this content.
        </div>
    </NotAuthorized>
</AuthorizeView>
```

---

### 3. Show Different Content By Role

```razor
<AuthorizeView Roles="Manager,Director">
    <Authorized>
        <!-- ONLY Managers and Directors see this -->
        <button class="btn btn-danger">Delete User</button>
    </Authorized>
    <NotAuthorized>
        <!-- Everyone else sees this -->
        <p>You don't have permission to delete users.</p>
    </NotAuthorized>
</AuthorizeView>
```

---

### 4. Add Logout Button to NavMenu

Already done in `NavMenu.razor`, but if you need it elsewhere:

```razor
<AuthorizeView>
    <Authorized>
        <div>
            <span>Welcome, @context.User.Identity?.Name</span>
            <a href="/logout" class="btn btn-danger">Logout</a>
        </div>
    </Authorized>
    <NotAuthorized>
        <a href="/login" class="btn btn-primary">Login</a>
    </NotAuthorized>
</AuthorizeView>
```

---

## Applying to Your Pages

### Users Page (`Pages/User/Index.razor` or similar)
```razor
@page "/users"
@attribute [Authorize]

<h1>Users</h1>
<!-- Your users table -->
```

### Teams Page
```razor
@page "/teams"
@attribute [Authorize]

<h1>Teams</h1>
<!-- Your teams content -->
```

### Categories Page
```razor
@page "/categories"
@attribute [Authorize]

<h1>Categories</h1>
<!-- Your categories content -->
```

### Year Quarter Page
```razor
@page "/yearquarter"
@attribute [Authorize]

<h1>Year Quarter</h1>
<!-- Your quarter content -->
```

---

## How to Debug

### Check if User is Logged In

```javascript
// Open browser console (F12) and type:
localStorage.getItem('userId')
```

**Should return:** `"user123"` or similar user ID
**If returns:** `null` ? User is NOT logged in

---

### Force Logout (for testing)

```javascript
// Open browser console and type:
localStorage.removeItem('userId');
localStorage.removeItem('role');
localStorage.removeItem('isLoggedIn');
location.reload();  // Refresh page
```

---

## Testing Checklist

- [ ] Logout page works (click "Logout" button)
- [ ] After logout, localStorage is empty
- [ ] After logout, can't access protected pages
- [ ] After login, can access protected pages
- [ ] Closing browser doesn't log you out (localStorage persists)
- [ ] NavMenu shows username when logged in
- [ ] NavMenu shows "Logout" button when logged in
- [ ] NavMenu shows "Login" button when NOT logged in
- [ ] Protected pages show data for logged-in users
- [ ] Protected pages show "Please log in" for non-logged-in users

---

## API Endpoints Reference

```
POST /api/account/login
?? Input: { email: string, password: string, rememberMe: bool }
?? Output: { userId: string, role: string }

POST /api/account/logout
?? Input: (empty)
?? Output: 200 OK

GET /api/users (protected)
?? Requires: Logged-in user
?? Returns: List of users

GET /api/categories (protected)
?? Requires: Logged-in user
?? Returns: List of categories
```

---

## One More Thing...

**Your BrowserAuthStateProvider does this automatically:**
1. Checks localStorage for userId
2. If found ? Tells entire app user is logged in
3. AuthorizeView components see this ? Show authorized content
4. If not found ? Tells entire app user is NOT logged in
5. AuthorizeView components see this ? Show "Please log in" message

**You don't need to do anything else!** Just add `@attribute [Authorize]` or `<AuthorizeView>` to your pages.

---

Good luck! You've got this! ??
