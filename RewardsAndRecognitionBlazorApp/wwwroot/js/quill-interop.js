// Quill Rich Text Editor Interop for Blazor
window.QuillInterop = {
    editors: {},
    
    // Initialize Quill editor
    initializeEditor: function (editorId, dotNetRef, initialValue) {
        try {
            const container = document.getElementById(editorId);
            if (!container) {
                console.error('Editor container not found:', editorId);
                return false;
            }

            // Create toolbar with emoji button
            const toolbarOptions = [
                ['bold', 'italic', 'underline'],
                [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                ['link'],
                ['emoji'], // Custom emoji button
                ['clean']
            ];

            // Set placeholder based on editor ID
            let placeholder = 'Enter text with formatting and emojis...';
            if (editorId === 'description-editor') {
                placeholder = 'Enter nomination description...';
            } else if (editorId === 'achievements-editor') {
                placeholder = 'Enter nomination achievements...';
            }

            const quill = new Quill(container, {
                theme: 'snow',
                modules: {
                    toolbar: {
                        container: toolbarOptions,
                        handlers: {
                            'emoji': function () {
                                QuillInterop.toggleEmojiPicker(editorId);
                            }
                        }
                    }
                },
                placeholder: placeholder
            });

            // Set initial value if provided
            if (initialValue) {
                quill.root.innerHTML = initialValue;
            }

            // Determine callback method name based on editor ID
            let callbackMethod = 'OnEditorContentChanged';
            if (editorId === 'achievements-editor') {
                callbackMethod = 'OnAchievementsEditorContentChanged';
            }

            // Store editor instance
            this.editors[editorId] = {
                quill: quill,
                dotNetRef: dotNetRef,
                emojiPickerOpen: false,
                callbackMethod: callbackMethod
            };

            // Notify Blazor of text changes
            quill.on('text-change', () => {
                const html = quill.root.innerHTML;
                dotNetRef.invokeMethodAsync(callbackMethod, html);
            });

            // Add custom emoji icon to toolbar
            const emojiButton = container.parentElement.querySelector('.ql-emoji');
            if (emojiButton) {
                emojiButton.innerHTML = '😊';
                emojiButton.style.fontSize = '18px';
                emojiButton.style.width = 'auto';
                emojiButton.style.padding = '0 8px';
            }

            // Create emoji picker
            this.createEmojiPicker(editorId);

            return true;
        } catch (error) {
            console.error('Error initializing Quill editor:', error);
            return false;
        }
    },

    // Create emoji picker popup
    createEmojiPicker: function (editorId) {
        const container = document.getElementById(editorId);
        if (!container) return;

        const picker = document.createElement('div');
        picker.id = editorId + '-emoji-picker';
        picker.className = 'emoji-picker';
        picker.style.display = 'none';
        
        // Common emojis organized by category
        const emojis = [
            // Smileys & People
            '😀', '😃', '😄', '😁', '😆', '😅', '🤣', '😂', '🙂', '🙃',
            '😉', '😊', '😇', '🥰', '😍', '🤩', '😘', '😗', '😚', '😙',
            '😋', '😛', '😜', '🤪', '😝', '🤑', '🤗', '🤭', '🤫', '🤔',
            '🤐', '🤨', '😐', '😑', '😶', '😏', '😒', '🙄', '😬', '🤥',
            '😌', '😔', '😪', '🤤', '😴', '😷', '🤒', '🤕', '🤢', '🤮',
            '🤧', '🥵', '🥶', '😵', '🤯', '🤠', '🥳', '😎', '🤓', '🧐',
            // Hand gestures
            '👍', '👎', '👌', '✌️', '🤞', '🤟', '🤘', '🤙', '👈', '👉',
            '👆', '👇', '☝️', '✋', '🤚', '🖐️', '🖖', '👋', '🤝', '🙏',
            // Hearts & Symbols
            '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '💔', '❣️', '💕',
            '💞', '💓', '💗', '💖', '💘', '💝', '💟', '✨', '⭐', '🌟',
            '💫', '✅', '❌', '⚠️', '📌', '🔥', '💯', '🎉', '🎊', '🎈'
        ];

        emojis.forEach(emoji => {
            const span = document.createElement('span');
            span.textContent = emoji;
            span.className = 'emoji-item';
            span.onclick = () => this.insertEmoji(editorId, emoji);
            picker.appendChild(span);
        });

        container.parentElement.appendChild(picker);
    },

    // Toggle emoji picker visibility
    toggleEmojiPicker: function (editorId) {
        const picker = document.getElementById(editorId + '-emoji-picker');
        const editor = this.editors[editorId];
        
        if (!picker || !editor) return;

        // Close all other emoji pickers
        Object.keys(this.editors).forEach(id => {
            if (id !== editorId) {
                const otherPicker = document.getElementById(id + '-emoji-picker');
                if (otherPicker) {
                    otherPicker.style.display = 'none';
                    this.editors[id].emojiPickerOpen = false;
                }
            }
        });

        // Toggle current picker
        if (editor.emojiPickerOpen) {
            picker.style.display = 'none';
            editor.emojiPickerOpen = false;
        } else {
            // Position picker below toolbar
            const toolbar = document.getElementById(editorId).parentElement.querySelector('.ql-toolbar');
            const toolbarRect = toolbar.getBoundingClientRect();
            picker.style.top = (toolbarRect.bottom - toolbarRect.top + 5) + 'px';
            picker.style.display = 'block';
            editor.emojiPickerOpen = true;
        }
    },

    // Insert emoji at cursor position
    insertEmoji: function (editorId, emoji) {
        const editor = this.editors[editorId];
        if (!editor) return;

        const quill = editor.quill;
        const range = quill.getSelection(true);
        
        // Insert emoji at cursor position
        quill.insertText(range ? range.index : 0, emoji);
        
        // Move cursor after emoji
        quill.setSelection(range ? range.index + emoji.length : emoji.length);
        
        // Close picker
        const picker = document.getElementById(editorId + '-emoji-picker');
        if (picker) {
            picker.style.display = 'none';
            editor.emojiPickerOpen = false;
        }
        
        // Focus back on editor
        quill.focus();
    },

    // Get editor content as HTML
    getContent: function (editorId) {
        const editor = this.editors[editorId];
        return editor ? editor.quill.root.innerHTML : '';
    },

    // Set editor content
    setContent: function (editorId, html) {
        const editor = this.editors[editorId];
        if (editor) {
            editor.quill.root.innerHTML = html || '';
        }
    },

    // Destroy editor instance
    destroyEditor: function (editorId) {
        const editor = this.editors[editorId];
        if (editor) {
            const picker = document.getElementById(editorId + '-emoji-picker');
            if (picker) {
                picker.remove();
            }
            delete this.editors[editorId];
        }
    }
};

// Close emoji picker when clicking outside
document.addEventListener('click', function (e) {
    const isEmojiButton = e.target.classList.contains('ql-emoji') || 
                         e.target.closest('.ql-emoji');
    const isEmojiPicker = e.target.classList.contains('emoji-picker') || 
                         e.target.closest('.emoji-picker');
    
    if (!isEmojiButton && !isEmojiPicker) {
        Object.keys(QuillInterop.editors).forEach(editorId => {
            const picker = document.getElementById(editorId + '-emoji-picker');
            if (picker && picker.style.display === 'block') {
                picker.style.display = 'none';
                QuillInterop.editors[editorId].emojiPickerOpen = false;
            }
        });
    }
});
