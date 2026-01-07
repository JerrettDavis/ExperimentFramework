// Theme detection and application utilities
window.themeUtils = {
    // Get the system color scheme preference
    getSystemTheme: function() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    },

    // Apply theme classes to document
    applyTheme: function(theme) {
        const html = document.documentElement;

        // Remove existing theme classes from html
        html.classList.remove('theme-light', 'theme-dark');

        // Apply new theme class to html
        if (theme === 'dark') {
            html.classList.add('theme-dark');
        } else if (theme === 'light') {
            html.classList.add('theme-light');
        }

        // Also apply to body if it exists (may not exist when called from head)
        if (document.body) {
            document.body.classList.remove('theme-light', 'theme-dark');
            if (theme === 'dark') {
                document.body.classList.add('theme-dark');
            } else if (theme === 'light') {
                document.body.classList.add('theme-light');
            }
        }
        // 'system' doesn't add a class - CSS media queries handle it
    },

    // Get stored theme preference or return 'system' as default
    getStoredTheme: function() {
        try {
            return localStorage.getItem('optimizelab-theme') || 'system';
        } catch {
            return 'system';
        }
    },

    // Store theme preference
    storeTheme: function(theme) {
        try {
            localStorage.setItem('optimizelab-theme', theme);
        } catch { }
    },

    // Initialize theme on page load
    initializeTheme: function() {
        const storedTheme = this.getStoredTheme();
        let effectiveTheme;

        if (storedTheme === 'system') {
            effectiveTheme = this.getSystemTheme();
        } else {
            effectiveTheme = storedTheme;
        }

        this.applyTheme(effectiveTheme);
        return effectiveTheme;
    },

    // Watch for system theme changes
    watchSystemTheme: function(dotNetHelper) {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

            // Remove any existing listener
            if (window._themeChangeHandler) {
                mediaQuery.removeEventListener('change', window._themeChangeHandler);
            }

            // Add new listener
            window._themeChangeHandler = function(e) {
                const newTheme = e.matches ? 'dark' : 'light';
                // Re-apply theme if set to system
                const storedTheme = window.themeUtils.getStoredTheme();
                if (storedTheme === 'system') {
                    window.themeUtils.applyTheme(newTheme);
                }
                dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', newTheme);
            };

            mediaQuery.addEventListener('change', window._themeChangeHandler);
        }
    },

    // Stop watching for system theme changes
    stopWatchingSystemTheme: function() {
        if (window.matchMedia && window._themeChangeHandler) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.removeEventListener('change', window._themeChangeHandler);
            window._themeChangeHandler = null;
        }
    }
};

// Set page title utility
window.setPageTitle = function(title) {
    document.title = title;
};

// Initialize theme immediately on script load
(function() {
    window.themeUtils.initializeTheme();

    // If body doesn't exist yet, apply theme to it when DOM is ready
    if (!document.body) {
        document.addEventListener('DOMContentLoaded', function() {
            // Re-apply to ensure body gets the class
            var storedTheme = window.themeUtils.getStoredTheme();
            var theme = storedTheme === 'system' ? window.themeUtils.getSystemTheme() : storedTheme;
            if (document.body) {
                document.body.classList.remove('theme-light', 'theme-dark');
                if (theme === 'dark') {
                    document.body.classList.add('theme-dark');
                } else if (theme === 'light') {
                    document.body.classList.add('theme-light');
                }
            }
        });
    }
})();
