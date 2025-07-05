/**
 * LottieViewer: Provides Lottie animation preview with the following features.
 * 
 * Requirements:
 * - Requires the following HTML structure:
 *   <div id="bm"></div>
 * - Lottie file path is obtained via chrome.webview.hostObjects.external.GetPath()
 * 
 * Features:
 * - Loads and plays Lottie animation files
 * - Uses lottie-web library for parsing and playback
 * - Automatically starts playback after loading
 * - Handles asynchronous loading and mounting of Lottie files
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

        // Because the path is a local file, we need to convert it to a URL format
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
