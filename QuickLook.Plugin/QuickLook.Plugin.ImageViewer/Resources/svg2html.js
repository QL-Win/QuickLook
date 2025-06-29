(async function () {
    const wrapper = document.getElementById("svgWrapper");

    const svgString = await chrome.webview.hostObjects.external.GetSvgContent();

    wrapper.innerHTML = svgString;

    let scale = 1;

    function updateTransform() {
        wrapper.style.transform = `scale(${scale})`;
    }

    document.addEventListener("wheel", (e) => {
        e.preventDefault();

        const scaleFactor = 1.1;

        if (e.deltaY < 0) {
            scale *= scaleFactor;
        } else {
            scale /= scaleFactor;
        }

        updateTransform();
    }, { passive: false });

    // Add double-click event to reset zoom
    wrapper.addEventListener("dblclick", () => {
        scale = 1;
        updateTransform();
    });

    updateTransform();
})();
