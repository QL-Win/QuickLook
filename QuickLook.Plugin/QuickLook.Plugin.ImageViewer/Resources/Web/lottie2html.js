/**
 * SvgaViewer: Provides SVGA animation preview with the following features.
 * 
 * Requirements:
 * - Requires the following HTML structure:
 *   <canvas id="canvas">
 *   </canvas>
 * - SVGA file path is obtained via chrome.webview.hostObjects.external.GetPath()
 * 
 * Features:
 * - Loads and plays SVGA animation files
 * - Uses SVGA.js library for parsing and playback
 * - Automatically starts playback after loading
 * - Handles asynchronous loading and mounting of SVGA files 
*/
class LottieViewer {
    constructor() {
    }

    /**
     * Play Lottie files.
     * @async
     */
    async play() {
        const path = await chrome.webview.hostObjects.external.GetPath();
        lottie.loadAnimation({
            container: document.getElementById('bm'),
            renderer: 'svg',
            loop: true,
            autoplay: true,
            path: 'https://' + path, 
        });
    }
}

// Create the Lottie viewer and play
new LottieViewer().play();
