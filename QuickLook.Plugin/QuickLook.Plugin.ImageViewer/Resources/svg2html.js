class SvgViewer {
    constructor() {
        this.scale = 1;
        this.minScale = 0.1;
        this.maxScale = 10;
        this.scaleStep = 1.2;
        this.baseScale = 1;
        this.viewBoxWidth = null;
        this.viewBoxHeight = null;
        this.svgElement = null;
        this.wrapper = document.getElementById('svgWrapper');
        this.transitionEnabled = false;
    }

    fixSvgSize(svgElement) {
        let widthAttr = svgElement.getAttribute('width');
        let heightAttr = svgElement.getAttribute('height');
        let viewBox = svgElement.getAttribute('viewBox');
        let viewBoxWidth = null, viewBoxHeight = null;

        if (!viewBox && widthAttr && heightAttr && widthAttr.trim() !== '' && heightAttr.trim() !== '') {
            svgElement.setAttribute('viewBox', `0 0 ${widthAttr} ${heightAttr}`);
            viewBox = svgElement.getAttribute('viewBox');
        }

        if (viewBox) {
            const vb = viewBox.split(/\s+/);
            if (vb.length === 4) {
                viewBoxWidth = parseFloat(vb[2]);
                viewBoxHeight = parseFloat(vb[3]);
            }
        }

        // If width or height is missing or empty, try to set from viewBox
        if (!widthAttr || widthAttr.trim() === '' || !heightAttr || heightAttr.trim() === '') {
            if (viewBoxWidth && viewBoxHeight) {
                svgElement.setAttribute('width', viewBoxWidth.toString());
                svgElement.setAttribute('height', viewBoxHeight.toString());
            } else {
                // fallback: if viewBox is missing or zero, use 100vw
                svgElement.style.width = '100vw';
                svgElement.style.height = '100vw';
            }
        }
        this.viewBoxWidth = viewBoxWidth;
        this.viewBoxHeight = viewBoxHeight;
    }

    computeBaseScale() {
        if (!this.viewBoxWidth || !this.viewBoxHeight) return 1;
        const wrapperRect = document.getElementById('svgContainer').getBoundingClientRect();
        const scaleX = wrapperRect.width / this.viewBoxWidth;
        const scaleY = wrapperRect.height / this.viewBoxHeight;
        return Math.min(scaleX, scaleY, 1); // never upscale by default
    }

    updateTransform() {
        this.svgElement.style.transform = `scale(${this.scale})`;
        this.svgElement.style.transformOrigin = 'center center';
    }

    enableTransition() {
        if (!this.transitionEnabled) {
            this.svgElement.style.transition = 'transform 0.1s ease-out';
            this.transitionEnabled = true;
        }
    }

    fitToWindow() {
        this.baseScale = this.computeBaseScale();
        this.scale = this.baseScale;
        this.updateTransform();
    }

    bindEvents() {
        this.wrapper.addEventListener("wheel", (e) => {
            this.enableTransition();
            e.preventDefault();
            if (e.deltaY < 0) {
                this.scale = Math.min(this.maxScale, this.scale * this.scaleStep);
            } else {
                this.scale = Math.max(this.minScale, this.scale / this.scaleStep);
            }
            this.updateTransform();
        }, { passive: false });
        this.wrapper.addEventListener('dblclick', () => {
            this.enableTransition();
            this.fitToWindow();
        });
    }

    async init() {
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

new SvgViewer().init();