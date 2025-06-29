(async function () {
    const wrapper = document.getElementById("svgWrapper");

    const svgString = await chrome.webview.hostObjects.external.GetSvgContent();

    wrapper.innerHTML = svgString;

    let scale = 1;
    let translate = { x: 0, y: 0 };
    let isDragging = false;
    let lastMouse = { x: 0, y: 0 };

    function updateTransform() {
        wrapper.style.transform = `translate(${translate.x}px, ${translate.y}px) scale(${scale})`;
    }

    document.addEventListener("wheel", (e) => {
        e.preventDefault();

        const scaleFactor = 1.1;
        const oldScale = scale;

        if (e.deltaY < 0) {
            scale *= scaleFactor;
        } else {
            scale /= scaleFactor;
        }

        const rect = wrapper.getBoundingClientRect();
        const dx = e.clientX - rect.left - rect.width / 2;
        const dy = e.clientY - rect.top - rect.height / 2;

        translate.x -= dx * (1 - scale / oldScale);
        translate.y -= dy * (1 - scale / oldScale);

        updateTransform();
    }, { passive: false });

    wrapper.addEventListener("mousedown", (e) => {
        isDragging = true;
        lastMouse = { x: e.clientX, y: e.clientY };
        wrapper.style.cursor = "grabbing";
    });

    document.addEventListener("mousemove", (e) => {
        if (!isDragging) return;

        const dx = e.clientX - lastMouse.x;
        const dy = e.clientY - lastMouse.y;

        translate.x += dx;
        translate.y += dy;

        lastMouse = { x: e.clientX, y: e.clientY };
        updateTransform();
    });

    document.addEventListener("mouseup", () => {
        isDragging = false;
        wrapper.style.cursor = "grab";
    });

    updateTransform();
})();
