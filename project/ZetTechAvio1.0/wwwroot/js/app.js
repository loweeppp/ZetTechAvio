// Initialize event listeners when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing event listeners');
    
    // Find the login button
    const loginButton = document.getElementById('loginButton');
    if (loginButton) {
        console.log('Login button found');
        loginButton.addEventListener('click', function(e) {
            console.log('Login button clicked via JavaScript');
            // The Blazor handler will still be called
        });
    } else {
        console.log('Login button not found');
    }
});