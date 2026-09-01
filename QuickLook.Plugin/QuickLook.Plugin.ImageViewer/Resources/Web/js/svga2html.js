/**
 * SvgaV2Viewer: SVGA 2.x animation preview (protobuf, svga lite).
 *
 * Requires:
 * - <canvas id="canvas">
 * - svga/index.min.js loaded before this file
 * - File path via chrome.webview.hostObjects.external.GetPath()
 */
class SvgaV2Viewer {
    async play() {
        const path = await chrome.webview.hostObjects.external.GetPath();
        const parser = new SVGA.Parser();

        parser.load('https://' + path).then(svga => {
            const player = new SVGA.Player(document.getElementById('canvas'));
            player.mount(svga).then(() => {
                player.start();
            });
        });
    }
}

new SvgaV2Viewer().play();
