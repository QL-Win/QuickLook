/**
 * SvgaV1Viewer: SVGA 1.x animation preview (ZIP/JSON, svgaplayerweb).
 *
 * Requires:
 * - <canvas id="canvas">
 * - svgaplayerweb + JSZip scripts loaded before this file
 * - File path via chrome.webview.hostObjects.external.GetPath()
 */
class SvgaV1Viewer {
    async play() {
        const path = await chrome.webview.hostObjects.external.GetPath();
        const size = JSON.parse(await chrome.webview.hostObjects.external.GetSize());
        const parser = new SVGA.Parser('#canvas');
        const player = new SVGA.Player('#canvas');
        const canvas = document.getElementById('canvas');

        canvas.width = size.width;
        canvas.height = size.height;

        parser.load('https://' + path, function (videoItem) {
            player.setVideoItem(videoItem);
            player.startAnimation();
        });
    }
}

new SvgaV1Viewer().play();
