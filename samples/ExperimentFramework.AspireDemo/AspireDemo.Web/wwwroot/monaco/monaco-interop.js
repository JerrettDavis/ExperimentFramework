// Monaco Editor Interop for Blazor
// Provides bridge between Blazor components and Monaco Editor

window.monacoEditor = {
    editors: {},
    monacoReady: false,
    pendingInitializations: [],
    themeObserverInitialized: false,

    // Set up global theme observer (called once when Monaco is ready)
    _setupThemeObserver: function() {
        if (this.themeObserverInitialized) return;
        this.themeObserverInitialized = true;

        // Set initial theme
        const isDark = document.body.classList.contains('theme-dark');
        monaco.editor.setTheme(isDark ? 'vs-dark' : 'vs');

        // Watch for theme changes globally
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.attributeName === 'class') {
                    const isDark = document.body.classList.contains('theme-dark');
                    monaco.editor.setTheme(isDark ? 'vs-dark' : 'vs');
                }
            });
        });
        observer.observe(document.body, { attributes: true });
    },

    // Initialize Monaco Editor in a container
    initialize: function(containerId, options, dotNetHelper) {
        // If Monaco isn't ready yet, queue this initialization
        if (!this.monacoReady) {
            this.pendingInitializations.push({ containerId, options, dotNetHelper });
            return true; // Return immediately to not block Blazor
        }

        return this._createEditor(containerId, options, dotNetHelper);
    },

    // Actually create the editor (called when Monaco is ready)
    _createEditor: function(containerId, options, dotNetHelper) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return false;
        }

        // Determine theme based on current page theme
        const isDark = document.body.classList.contains('theme-dark');

        // Create editor
        const editor = monaco.editor.create(container, {
            value: options.value || '',
            language: options.language || 'yaml',
            theme: isDark ? 'vs-dark' : 'vs',
            automaticLayout: true,
            minimap: { enabled: options.minimap !== false },
            lineNumbers: options.lineNumbers !== false ? 'on' : 'off',
            fontSize: options.fontSize || 14,
            tabSize: 2,
            wordWrap: 'on',
            scrollBeyondLastLine: false,
            renderWhitespace: 'selection',
            folding: true,
            readOnly: options.readOnly || false,
            scrollbar: {
                vertical: 'auto',
                horizontal: 'auto',
                useShadows: false,
                verticalScrollbarSize: 10,
                horizontalScrollbarSize: 10
            }
        });

        // Store reference
        this.editors[containerId] = { editor, dotNetHelper };

        // Set up change handler with debouncing
        let changeTimeout;
        editor.onDidChangeModelContent(() => {
            clearTimeout(changeTimeout);
            changeTimeout = setTimeout(async () => {
                if (dotNetHelper) {
                    try {
                        await dotNetHelper.invokeMethodAsync('OnContentChanged', editor.getValue());
                    } catch (e) {
                        console.error('Failed to notify Blazor of content change:', e);
                    }
                }
            }, 300); // 300ms debounce
        });

        return true;
    },

    // Process any pending initializations
    _processPendingInitializations: function() {
        while (this.pendingInitializations.length > 0) {
            const pending = this.pendingInitializations.shift();
            this._createEditor(pending.containerId, pending.options, pending.dotNetHelper);
        }
    },

    // Load Monaco from CDN (called immediately on script load)
    loadMonaco: function() {
        if (window.monacoLoading) {
            return window.monacoLoading;
        }

        window.monacoLoading = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.min.js';
            script.onload = () => {
                require.config({
                    paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' }
                });
                require(['vs/editor/editor.main'], () => {
                    // Configure YAML language
                    monaco.languages.register({ id: 'yaml' });

                    // Register YAML completion provider
                    monaco.languages.registerCompletionItemProvider('yaml', {
                        provideCompletionItems: function(model, position) {
                            const suggestions = [
                                {
                                    label: 'experiments',
                                    kind: monaco.languages.CompletionItemKind.Keyword,
                                    insertText: 'experiments:\n  - name: ${1:experiment-name}\n    metadata:\n      displayName: "${2:Display Name}"\n      description: "${3:Description}"\n      category: "${4:Category}"\n    trials:\n      - serviceType: "${5:IServiceType}"\n        selectionMode:\n          type: configurationKey\n          key: "${6:Experiments:Key}"\n        control:\n          key: ${7:default}\n          implementationType: "${8:DefaultImplementation}"\n        conditions:\n          - key: ${9:variant}\n            implementationType: "${10:VariantImplementation}"\n        errorPolicy:\n          type: fallbackToControl',
                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                    documentation: 'Define a new experiment'
                                },
                                {
                                    label: 'trial',
                                    kind: monaco.languages.CompletionItemKind.Snippet,
                                    insertText: '- serviceType: "${1:IServiceType}"\n  selectionMode:\n    type: ${2:configurationKey}\n    key: "${3:Experiments:Key}"\n  control:\n    key: ${4:default}\n    implementationType: "${5:DefaultImplementation}"\n  conditions:\n    - key: ${6:variant}\n      implementationType: "${7:VariantImplementation}"',
                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                    documentation: 'Add a trial to an experiment'
                                },
                                {
                                    label: 'condition',
                                    kind: monaco.languages.CompletionItemKind.Snippet,
                                    insertText: '- key: ${1:variant}\n  implementationType: "${2:ImplementationType}"',
                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                    documentation: 'Add a condition/variant'
                                },
                                {
                                    label: 'selectionMode',
                                    kind: monaco.languages.CompletionItemKind.Property,
                                    insertText: 'selectionMode:\n  type: ${1|configurationKey,featureFlag,rollout,targeting|}\n  key: "${2:key}"',
                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                    documentation: 'Configure selection mode'
                                },
                                {
                                    label: 'errorPolicy',
                                    kind: monaco.languages.CompletionItemKind.Property,
                                    insertText: 'errorPolicy:\n  type: ${1|fallbackToControl,throw,fallbackTo,tryInOrder|}',
                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                    documentation: 'Configure error handling policy'
                                }
                            ];

                            return { suggestions: suggestions };
                        }
                    });

                    // Mark Monaco as ready
                    window.monacoEditor.monacoReady = true;

                    // Set up global theme observer
                    window.monacoEditor._setupThemeObserver();

                    // Process pending initializations
                    window.monacoEditor._processPendingInitializations();

                    resolve();
                });
            };
            script.onerror = (e) => {
                console.error('Failed to load Monaco Editor:', e);
                reject(e);
            };
            document.head.appendChild(script);
        });

        return window.monacoLoading;
    },

    // Set editor value
    setValue: function(containerId, value) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            editorData.editor.setValue(value);
        }
    },

    // Get editor value
    getValue: function(containerId) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            return editorData.editor.getValue();
        }
        return '';
    },

    // Set validation markers
    setMarkers: function(containerId, markers) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            const model = editorData.editor.getModel();
            if (model) {
                const monacoMarkers = markers.map(m => ({
                    startLineNumber: m.line || 1,
                    startColumn: m.column || 1,
                    endLineNumber: m.endLine || m.line || 1,
                    endColumn: m.endColumn || (m.column || 1) + 10,
                    message: m.message,
                    severity: m.severity === 'error'
                        ? monaco.MarkerSeverity.Error
                        : m.severity === 'warning'
                            ? monaco.MarkerSeverity.Warning
                            : monaco.MarkerSeverity.Info
                }));
                monaco.editor.setModelMarkers(model, 'dsl-validation', monacoMarkers);
            }
        }
    },

    // Clear markers
    clearMarkers: function(containerId) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            const model = editorData.editor.getModel();
            if (model) {
                monaco.editor.setModelMarkers(model, 'dsl-validation', []);
            }
        }
    },

    // Go to line
    goToLine: function(containerId, line, column) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            editorData.editor.revealLineInCenter(line);
            editorData.editor.setPosition({ lineNumber: line, column: column || 1 });
            editorData.editor.focus();
        }
    },

    // Focus editor
    focus: function(containerId) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            editorData.editor.focus();
        }
    },

    // Dispose editor
    dispose: function(containerId) {
        const editorData = this.editors[containerId];
        if (editorData && editorData.editor) {
            editorData.editor.dispose();
            delete this.editors[containerId];
        }
    }
};

// Start loading Monaco immediately when the script loads
window.monacoEditor.loadMonaco();
