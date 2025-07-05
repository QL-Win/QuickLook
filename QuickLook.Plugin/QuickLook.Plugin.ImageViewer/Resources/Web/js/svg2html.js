/**
 * SvgViewer: Provides SVG preview with the following features.
 *
 * Requirements:
 * - Requires the following HTML structure:
 *   <div id="svgContainer">
 *     <div id="svgWrapper"></div>
 *   </div>
 * - SVG content is obtained via chrome.webview.hostObjects.external.GetSvgContent()
 *
 * Features:
 * - Fit SVG to window with no upscaling
 * - Mouse wheel zoom in/out (with smooth animation)
 * - Double-click to fit SVG to window
 * - Mouse drag to pan (only when SVG is larger than the container)
 * - Pan is limited to visible overflow area
 * - Handles SVGs with or without width/height/viewBox attributes
 * - Resets pan on zoom or fit
 * - No animation during pan, smooth animation during zoom/fit
 */
class SvgViewer {
    constructor() {
        // Initial scale and scale limits
        this.scale = 1;
        this.minScale = 0.1;
        this.maxScale = 100; // Increased zoom upper limit by 10x
        this.scaleStep = 1.2;
        this.baseScale = 1;

        // SVG viewBox dimensions
        this.viewBoxWidth = null;
        this.viewBoxHeight = null;
        this.svgElement = null;
        this.wrapper = document.getElementById('svgWrapper');
        this.transitionEnabled = false;

        // Offset for panning
        this.offsetX = 0;
        this.offsetY = 0;

        // Drag state
        this.isDragging = false;
        this.lastMouseX = 0;
        this.lastMouseY = 0;
    }

    /**
     * Ensure SVG has proper size attributes and extract viewBox dimensions.
     * @param {SVGElement} svgElement The SVG element to fix.
     */
    fixSvgSize(svgElement) {
        let widthAttr = svgElement.getAttribute('width');
        let heightAttr = svgElement.getAttribute('height');
        let viewBox = svgElement.getAttribute('viewBox');
        let viewBoxWidth = null, viewBoxHeight = null;

        // If viewBox is missing but width/height exist, set viewBox
        if (!viewBox && widthAttr && heightAttr && widthAttr.trim() !== '' && heightAttr.trim() !== '') {
            svgElement.setAttribute('viewBox', `0 0 ${widthAttr} ${heightAttr}`);
            viewBox = svgElement.getAttribute('viewBox');
        }

        // Parse viewBox dimensions
        if (viewBox) {
            const vb = viewBox.split(/\s+/);

            if (vb.length === 4) {
                viewBoxWidth = parseFloat(vb[2]);
                viewBoxHeight = parseFloat(vb[3]);
            }
        }

        // If width or height is missing, set from viewBox if possible
        if (!widthAttr || widthAttr.trim() === '' || !heightAttr || heightAttr.trim() === '') {
            if (viewBoxWidth && viewBoxHeight) {
                svgElement.setAttribute('width', viewBoxWidth.toString());
                svgElement.setAttribute('height', viewBoxHeight.toString());
            } else {
                // Fallback: use 100vw if no size info
                svgElement.style.width = '100vw';
                svgElement.style.height = '100vw';
            }
        }
        this.viewBoxWidth = viewBoxWidth;
        this.viewBoxHeight = viewBoxHeight;
    }

    /**
     * Calculate the base scale to fit SVG into the container.
     * @returns {number} The base scale factor.
     */
    computeBaseScale() {
        if (!this.viewBoxWidth || !this.viewBoxHeight) return 1;

        const wrapperRect = document.getElementById('svgContainer').getBoundingClientRect();
        const scaleX = wrapperRect.width / this.viewBoxWidth;
        const scaleY = wrapperRect.height / this.viewBoxHeight;

        return Math.min(scaleX, scaleY, 1); // Never upscale by default
    }

    /**
     * Update SVG transform for scale and pan.
     */
    updateTransform() {
        // Calculate actual SVG display size
        const container = document.getElementById('svgContainer');
        const containerRect = container.getBoundingClientRect();
        const svgWidth = this.viewBoxWidth * this.scale;
        const svgHeight = this.viewBoxHeight * this.scale;

        // Limit pan offset to visible area
        let maxOffsetX = Math.max(0, (svgWidth - containerRect.width) / 2);
        let maxOffsetY = Math.max(0, (svgHeight - containerRect.height) / 2);
        this.offsetX = Math.max(-maxOffsetX, Math.min(this.offsetX, maxOffsetX));
        this.offsetY = Math.max(-maxOffsetY, Math.min(this.offsetY, maxOffsetY));

        // Only allow pan if SVG is larger than container
        if (svgWidth <= containerRect.width) this.offsetX = 0;
        if (svgHeight <= containerRect.height) this.offsetY = 0;
        this.svgElement.style.transform = `scale(${this.scale}) translate(${this.offsetX / this.scale}px, ${this.offsetY / this.scale}px)`;
        this.svgElement.style.transformOrigin = 'center center';
    }

    /**
     * Enable transform transition (for zoom).
     */
    enableTransition() {
        this.svgElement.style.transition = 'transform 0.1s ease-out';
        this.transitionEnabled = true;
    }

    /**
     * Disable transform transition (for pan).
     */
    disableTransition() {
        this.svgElement.style.transition = '';
        this.transitionEnabled = false;
    }

    /**
     * Fit SVG to window and reset pan.
     */
    fitToWindow() {
        this.baseScale = this.computeBaseScale();
        this.scale = this.baseScale;
        this.offsetX = 0;
        this.offsetY = 0;
        this.updateTransform();
    }

    /**
     * Bind mouse and wheel events for zoom and pan.
     */
    bindEvents() {
        // Prevent file drag-and-drop on the window to disable dropping files into the viewer
        window.addEventListener('dragover', function (e) {
            e.preventDefault();
        });
        window.addEventListener('drop', function (e) {
            e.preventDefault();
        });

        // Prevent the context menu (right-click menu) from appearing anywhere in the viewer
        window.addEventListener('contextmenu', function (e) {
            e.preventDefault();
        });

        // Zoom with mouse wheel
        this.wrapper.addEventListener("wheel", (e) => {
            this.enableTransition();
            e.preventDefault();
            if (e.deltaY < 0) {
                this.scale = Math.min(this.maxScale, this.scale * this.scaleStep);
            } else {
                this.scale = Math.max(this.minScale, this.scale / this.scaleStep);
            }

            // Reset pan on zoom
            this.offsetX = 0;
            this.offsetY = 0;
            this.updateTransform();
        }, { passive: false });

        // Double click to fit
        this.wrapper.addEventListener('dblclick', () => {
            this.enableTransition();
            this.fitToWindow();
        });

        // Start pan on mouse down
        this.wrapper.addEventListener('mousedown', (e) => {
            // Only left mouse button
            if (e.button !== 0) return;

            // Only allow pan if SVG is larger than container
            const container = document.getElementById('svgContainer');
            const containerRect = container.getBoundingClientRect();
            const svgWidth = this.viewBoxWidth * this.scale;
            const svgHeight = this.viewBoxHeight * this.scale;

            if (svgWidth > containerRect.width || svgHeight > containerRect.height) {
                this.isDragging = true;
                this.lastMouseX = e.clientX;
                this.lastMouseY = e.clientY;
                this.disableTransition(); // Disable animation while panning
            }
        });

        // Pan on mouse move
        window.addEventListener('mousemove', (e) => {
            if (!this.isDragging) return;

            const container = document.getElementById('svgContainer');
            const containerRect = container.getBoundingClientRect();
            const svgWidth = this.viewBoxWidth * this.scale;
            const svgHeight = this.viewBoxHeight * this.scale;

            // Only allow pan if SVG is larger than container
            if (svgWidth > containerRect.width || svgHeight > containerRect.height) {
                let dx = e.clientX - this.lastMouseX;
                let dy = e.clientY - this.lastMouseY;
                // Only allow pan in directions where SVG is larger
                if (svgWidth > containerRect.width) {
                    this.offsetX += dx;
                }
                if (svgHeight > containerRect.height) {
                    this.offsetY += dy;
                }
                this.lastMouseX = e.clientX;
                this.lastMouseY = e.clientY;
                this.updateTransform();
            }
        });

        // End pan on mouse up
        window.addEventListener('mouseup', () => {
            if (this.isDragging) {
                this.isDragging = false;
            }
        });
    }

    /**
     * Render SVG file.
     * @async
     */
    async render() {
        const rawSvg = await chrome.webview.hostObjects.external.GetSvgContent();
        const parser = new DOMParser();
        const doc = parser.parseFromString(rawSvg, 'image/svg+xml');
        this.svgElement = doc.documentElement;

        // Fix SVG size and get viewBox dimensions
        this.fixSvgSize(this.svgElement);

        this.wrapper.innerHTML = '';
        this.wrapper.appendChild(this.svgElement);

        this.fitToWindow();
        this.bindEvents();
        this.updateTransform();
    }
}

// Create the SVG viewer and render
new SvgViewer().render();