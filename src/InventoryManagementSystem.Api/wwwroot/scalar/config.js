// Loaded only by the development Scalar page. Keep CSRF enforcement on the API.
export default {
    onBeforeRequest: async ({ request, requestBuilder }) => {
        const url = new URL(request.url, window.location.origin);
        if (url.origin !== window.location.origin || !url.pathname.toLowerCase().startsWith('/api/')) {
            return;
        }

        const method = request.method.toUpperCase();
        if (['GET', 'HEAD', 'OPTIONS'].includes(method)) {
            return;
        }

        // Fetch a fresh token so login/logout identity changes cannot leave a stale token.
        // The browser stores and sends the paired HttpOnly antiforgery cookie.
        const response = await fetch('/api/Auth/csrf-token', {
            credentials: 'same-origin',
            cache: 'no-store',
            headers: { Accept: 'application/json' }
        });
        if (!response.ok) {
            throw new Error('Could not obtain the CSRF token. Reload Scalar and try again.');
        }

        const result = await response.json();
        const token = result.data?.csrfToken;
        if (typeof token !== 'string' || token.length === 0) {
            throw new Error('The CSRF endpoint did not return a token.');
        }

        // Support both the bundled client and newer Scalar request-builder clients.
        request.headers.set('X-XSRF-TOKEN', token);
        requestBuilder?.headers.set('X-XSRF-TOKEN', token);
    }
};
