/**
 * AUTHENTICATION MODULE
 */

async function handleLogin(event) {
    event.preventDefault(); // Stop the form from refreshing the page

    // 1. Get the current values from the form
    const usernameInput = document.getElementById('loginEmail').value;
    const passwordInput = document.getElementById('loginPassword').value;
    const errorMessage = document.getElementById('errorMessage');
    const loginBtn = document.getElementById('loginBtn');
    const originalBtnContent = loginBtn.innerHTML;

    // 2. Prepare the payload (Keys must match C# LoginRequest properties exactly)
    const payload = {
        Username: usernameInput,
        Password: passwordInput
    };

    try {
        // Show loading state
        loginBtn.disabled = true;
        loginBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Signing In...';
        errorMessage.style.display = 'none';

        // 3. Send the request to your C# AuthController
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const data = await response.json();

            // 4. Store user info for the dashboard to use
            // Note: Use lowercase keys to match the C# 'Ok(new { ... })' object keys
            sessionStorage.setItem('userName', data.userName);
            sessionStorage.setItem('userRole', data.role);
            sessionStorage.setItem('isLoggedIn', 'true');

            // 5. Success! Redirect to the dashboard
            window.location.href = 'dashboard.html';
        } else {
            // 6. Handle 401 Unauthorized
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = '<i class="fas fa-exclamation-circle"></i> Invalid credentials. Please try again.';

            // Restore button state
            loginBtn.disabled = false;
            loginBtn.innerHTML = originalBtnContent;
        }
    } catch (err) {
        // 7. Handle Server Offline / Network errors
        console.error("Login Error:", err);
        errorMessage.style.display = 'block';
        errorMessage.innerText = "Connection failed. Please ensure the backend server is running.";

        // Restore button state
        loginBtn.disabled = false;
        loginBtn.innerHTML = originalBtnContent;
    }
}