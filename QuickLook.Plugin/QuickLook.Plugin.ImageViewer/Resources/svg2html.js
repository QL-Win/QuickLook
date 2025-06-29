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
    img.style.transform = "scale(1)";

    let scale = 1;

    function updateTransform() {
        img.style.transform = `scale(${scale})`;
    }

    document.addEventListener("wheel", (e) => {
        e.preventDefault();

        const scaleFactor = 1.2;

        scale *= (e.deltaY < 0 ? scaleFactor : 1 / scaleFactor);
        updateTransform();
    }, { passive: false });

    // Add double-click event to reset zoom
    wrapper.addEventListener("dblclick", () => {
        scale = 1;
        updateTransform();
    });

    wrapper.appendChild(img);
    updateTransform();
})();
