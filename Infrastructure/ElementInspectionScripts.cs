namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Contains JavaScript code for element inspection and selection.
    /// </summary>
    public static class ElementInspectionScripts
    {
        /// <summary>
        /// JavaScript to enable element selection mode.
        /// </summary>
        public static readonly string EnableSelectionMode = @"
(function() {
    console.log('Starting element selection mode setup');
    
    // Remove any existing handlers first
    if (window.__selectionModeActive) {
        console.log('Removing existing selection mode handlers');
        document.removeEventListener('mouseover', window.__mouseoverHandler, true);
        document.removeEventListener('mouseout', window.__mouseoutHandler, true);
        document.removeEventListener('click', window.__clickHandler, true);
    }
    
    window.__selectionModeActive = true;
    window.__selectedElement = null;
    console.log('Selection mode flag set to true');
    
    // Define helper functions
    window.__getElementSelector = function(element) {
        if (element.id !== '')
            return '#' + element.id;
        
        let path = [];
        let el = element;
        while (el.parentElement) {
            let selector = el.tagName.toLowerCase();
            if (el.id) {
                selector += '#' + el.id;
                path.unshift(selector);
                break;
            } else {
                let sibling = el;
                let nth = 1;
                while (sibling = sibling.previousElementSibling) {
                    if (sibling.tagName.toLowerCase() == selector)
                        nth++;
                }
                if (nth > 1)
                    selector += ':nth-of-type(' + nth + ')';
            }
            path.unshift(selector);
            el = el.parentElement;
        }
        return path.join(' > ');
    };
    
    window.__getElementAttributes = function(element) {
        const attrs = {};
        for (let attr of element.attributes) {
            attrs[attr.name] = attr.value;
        }
        return attrs;
    };
    
    // Mouseover handler - highlight elements
    window.__mouseoverHandler = function(e) {
        if (!window.__selectionModeActive) return;
        e.target.style.outline = '3px solid #FF6B6B';
        e.target.style.outlineOffset = '2px';
    };
    
    // Mouseout handler - remove highlight
    window.__mouseoutHandler = function(e) {
        if (!window.__selectionModeActive) return;
        e.target.style.outline = '';
        e.target.style.outlineOffset = '';
    };
    
    // Click handler - capture element
    window.__clickHandler = function(e) {
        if (!window.__selectionModeActive) {
            console.log('Selection mode not active, ignoring click');
            return;
        }
        
        console.log('Element clicked, capturing info for:', e.target.tagName);
        
        window.__selectedElement = e.target;
        const elementInfo = {
            type: e.target.tagName.toLowerCase(),
            id: e.target.id || '',
            className: e.target.className || '',
            text: (e.target.textContent || '').substring(0, 100),
            selector: window.__getElementSelector(e.target),
            attributes: window.__getElementAttributes(e.target)
        };
        
        const message = JSON.stringify({
            type: 'elementSelected',
            data: elementInfo
        });
        
        console.log('Sending message:', message);
        try {
            window.chrome.webview.postMessage(message);
            console.log('Message sent successfully');
        } catch(err) {
            console.error('Failed to send message:', err);
        }
        
        // Stop propagation to prevent triggering actual click handlers
        e.stopImmediatePropagation();
    };
    
    // Add event listeners
    console.log('Adding event listeners');
    document.addEventListener('mouseover', window.__mouseoverHandler, true);
    document.addEventListener('mouseout', window.__mouseoutHandler, true);
    document.addEventListener('click', window.__clickHandler, true);
    
    console.log('Selection mode enabled successfully');
})();
";

        /// <summary>
        /// JavaScript to disable element selection mode.
        /// </summary>
        public static readonly string DisableSelectionMode = @"
(function() {
    console.log('Disabling element selection mode');
    window.__selectionModeActive = false;
    
    // Remove event listeners
    if (window.__mouseoverHandler) {
        console.log('Removing mouseover listener');
        document.removeEventListener('mouseover', window.__mouseoverHandler, true);
    }
    if (window.__mouseoutHandler) {
        console.log('Removing mouseout listener');
        document.removeEventListener('mouseout', window.__mouseoutHandler, true);
    }
    if (window.__clickHandler) {
        console.log('Removing click listener');
        document.removeEventListener('click', window.__clickHandler, true);
    }
    
    // Clear outline from selected element
    if (window.__selectedElement) {
        console.log('Clearing outline from selected element');
        window.__selectedElement.style.outline = '';
        window.__selectedElement.style.outlineOffset = '';
        window.__selectedElement = null;
    }
    
    console.log('Selection mode disabled successfully');
})();
";

        /// <summary>
        /// JavaScript to get element information.
        /// </summary>
        public static readonly string GetElementInfo = @"
(function() {
    if (window.__selectedElement) {
        return JSON.stringify({
            type: window.__selectedElement.tagName.toLowerCase(),
            id: window.__selectedElement.id || '',
            className: window.__selectedElement.className || '',
            text: (window.__selectedElement.textContent || '').substring(0, 100),
            selector: window.__getElementSelector(window.__selectedElement),
            attributes: window.__getElementAttributes(window.__selectedElement)
        });
    }
    return null;
})();
";
    }
}
