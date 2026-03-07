// Auto-authorize Swagger UI after a successful login call.
// Intercepts responses from /api/auth/login and /api/auth/register,
// reads the "token" field, and calls preauthorizeApiKey so you never
// have to copy-paste a Bearer token manually.
(function waitForSwaggerUi() {
    const INTERVAL_MS = 150;
    const MAX_ATTEMPTS = 100; // ~15 seconds
    let attempts = 0;

    const interval = setInterval(function () {
        attempts++;
        if (attempts > MAX_ATTEMPTS) {
            clearInterval(interval);
            return;
        }

        if (!window.ui) return;
        clearInterval(interval);

        const originalResponseInterceptor = window.ui.getConfigs().responseInterceptor;

        window.ui.setConfigs({
            responseInterceptor: function (response) {
                try {
                    const isAuthEndpoint =
                        response.url && (
                            response.url.includes('/api/auth/login') ||
                            response.url.includes('/api/auth/register')
                        );

                    if (isAuthEndpoint && response.status === 200) {
                        const body = typeof response.body === 'string'
                            ? JSON.parse(response.body)
                            : response.body;

                        const token = body?.token || body?.Token;
                        if (token) {
                            window.ui.preauthorizeApiKey('Bearer', 'Bearer ' + token);
                            console.info('[Swagger] 🔑 Firebase token auto-applied.');
                        }
                    }
                } catch (e) {
                    console.warn('[Swagger] Auto-auth error:', e);
                }

                return originalResponseInterceptor
                    ? originalResponseInterceptor(response)
                    : response;
            }
        });
    }, INTERVAL_MS);
})();
