(async function () {
    function encodeSvgToBase64(svg) {
        const utf8Bytes = new TextEncoder().encode(svg);
        let binary = '';
        for (const byte of utf8Bytes) {
            binary += String.fromCharCode(byte);
        }
        return btoa(binary);
    }

    function fixSvgSize(svg) {
        const viewBoxMatch = svg.match(/viewBox\s*=\s*["']?([\d\s\.]+)["']/i);
        if (!viewBoxMatch) return svg;

        const parts = viewBoxMatch[1].trim().split(/\s+/);
        if (parts.length !== 4) return svg;

        const width = parseFloat(parts[2]);
        const height = parseFloat(parts[3]);

        if (/width\s*=/.test(svg) || /height\s*=/.test(svg)) {
            return svg;
        }

        return svg.replace(
            /<svg([^>]*)>/i,
            `<svg width="${width}" height="${height}"$1>`
        );
    }

    const wrapper = document.getElementById("svgWrapper");
    const svgString = await chrome.webview.hostObjects.external.GetSvgContent();
    const base64 = encodeSvgToBase64(fixSvgSize(svgString));
    const img = new Image();
    img.src = `data:image/svg+xml;base64,${base64}`;
    img.draggable = false;

    let scale = 1;
    let offsetX = 0;
    let offsetY = 0;
    let startX = 0;
    let startY = 0;
    let isDragging = false;
    let imgNaturalWidth = 0;
    let imgNaturalHeight = 0;
    let canDragX = false;
    let canDragY = false;

    function clamp(val, min, max) {
        return Math.max(min, Math.min(max, val));
    }

    function updateTransform() {
        // Calculate draggable range
        const wrapperRect = wrapper.getBoundingClientRect();
        const w = imgNaturalWidth * scale;
        const h = imgNaturalHeight * scale;
        canDragX = w > wrapperRect.width;
        canDragY = h > wrapperRect.height;
        const maxOffsetX = canDragX ? (w - wrapperRect.width) / 2 : 0;
        const maxOffsetY = canDragY ? (h - wrapperRect.height) / 2 : 0;
        offsetX = canDragX ? clamp(offsetX, -maxOffsetX, maxOffsetX) : 0;
        offsetY = canDragY ? clamp(offsetY, -maxOffsetY, maxOffsetY) : 0;
        img.style.transform = `translate(${offsetX}px, ${offsetY}px) scale(${scale})`;
    }

    img.onload = function () {
        imgNaturalWidth = img.naturalWidth;
        imgNaturalHeight = img.naturalHeight;
        updateTransform();
    };

    document.addEventListener("wheel", (e) => {
        e.preventDefault();
        const scaleFactor = 1.2;
        const prevScale = scale;
        scale *= (e.deltaY < 0 ? scaleFactor : 1 / scaleFactor);
        // Zoom based on window center, do not adjust offsetX/offsetY
        updateTransform();
    }, { passive: false });

    // Drag events
    img.addEventListener("mousedown", (e) => {
        if (!(canDragX || canDragY)) return;
        isDragging = true;
        startX = e.clientX - offsetX;
        startY = e.clientY - offsetY;
    });
    document.addEventListener("mousemove", (e) => {
        if (!isDragging) return;
        if (canDragX) offsetX = e.clientX - startX; else offsetX = 0;
        if (canDragY) offsetY = e.clientY - startY; else offsetY = 0;
        updateTransform();
    });
    document.addEventListener("mouseup", () => {
        isDragging = false;
    });
    document.addEventListener("mouseleave", () => {
        isDragging = false;
    });

    // Double-click to reset zoom and position
    wrapper.addEventListener("dblclick", () => {
        scale = 1;
        offsetX = 0;
        offsetY = 0;
        updateTransform();
    });

    wrapper.appendChild(img);
})();
