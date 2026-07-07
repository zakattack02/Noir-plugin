/**
 * SpiderNoir Player Overlay
 * 
 * Adds a Noir / Color version switcher overlay to the Jellyfin video player.
 * This script is loaded by the Jellyfin web client when playing SpiderNoir content.
 * 
 * Dependencies: ApiClient (global Jellyfin web object), Dashboard (global)
 * No jQuery required.
 */

(function () {
    'use strict';

    var SPIDER_NOIR_PLUGIN_ID = '3bd33ef7-dd55-485b-9487-8bce0b52bd55';
    var OVERLAY_ID = 'spiderNoirOverlay';
    var STYLE_ID = 'spiderNoirStyles';

    /**
     * Create and inject CSS styles for the overlay.
     */
    function injectStyles() {
        if (document.getElementById(STYLE_ID)) {
            return;
        }

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = [
            '#' + OVERLAY_ID + ' {',
            '  position: fixed;',
            '  bottom: 80px;',
            '  right: 24px;',
            '  z-index: 999999;',
            '  display: none;',
            '  flex-direction: row;',
            '  gap: 8px;',
            '  padding: 8px;',
            '  background: rgba(0, 0, 0, 0.75);',
            '  border-radius: 8px;',
            '  backdrop-filter: blur(4px);',
            '  border: 1px solid rgba(255, 255, 255, 0.15);',
            '  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;',
            '}',
            '#' + OVERLAY_ID + '.visible {',
            '  display: flex;',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-btn {',
            '  padding: 10px 20px;',
            '  border: 2px solid rgba(255, 255, 255, 0.2);',
            '  border-radius: 6px;',
            '  background: rgba(255, 255, 255, 0.05);',
            '  color: #ccc;',
            '  cursor: pointer;',
            '  font-size: 13px;',
            '  font-weight: 600;',
            '  letter-spacing: 0.5px;',
            '  text-transform: uppercase;',
            '  transition: all 0.2s ease;',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-btn:hover {',
            '  background: rgba(255, 255, 255, 0.15);',
            '  color: #fff;',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-btn.active {',
            '  border-color: #00a4dc;',
            '  background: rgba(0, 164, 220, 0.25);',
            '  color: #fff;',
            '  box-shadow: 0 0 12px rgba(0, 164, 220, 0.3);',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-btn.noir-btn.active {',
            '  border-color: #888;',
            '  background: rgba(136, 136, 136, 0.3);',
            '  box-shadow: 0 0 12px rgba(136, 136, 136, 0.3);',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-btn.color-btn.active {',
            '  border-color: #f5a623;',
            '  background: rgba(245, 166, 35, 0.25);',
            '  box-shadow: 0 0 12px rgba(245, 166, 35, 0.3);',
            '}',
            '#' + OVERLAY_ID + ' .spider-noir-label {',
            '  color: rgba(255, 255, 255, 0.5);',
            '  font-size: 10px;',
            '  text-transform: uppercase;',
            '  letter-spacing: 1px;',
            '  display: flex;',
            '  align-items: center;',
            '  padding-right: 8px;',
            '  border-right: 1px solid rgba(255, 255, 255, 0.1);',
            '  margin-right: 4px;',
            '}'
        ].join('\n');
        document.head.appendChild(style);
    }

    /**
     * Create the overlay DOM element.
     */
    function createOverlay() {
        if (document.getElementById(OVERLAY_ID)) {
            return document.getElementById(OVERLAY_ID);
        }

        var overlay = document.createElement('div');
        overlay.id = OVERLAY_ID;

        var label = document.createElement('span');
        label.className = 'spider-noir-label';
        label.textContent = 'Version';

        var noirBtn = document.createElement('button');
        noirBtn.className = 'spider-noir-btn noir-btn';
        noirBtn.textContent = 'Noir';
        noirBtn.dataset.version = 'noir';

        var colorBtn = document.createElement('button');
        colorBtn.className = 'spider-noir-btn color-btn';
        colorBtn.textContent = 'Color';
        colorBtn.dataset.version = 'color';

        overlay.appendChild(label);
        overlay.appendChild(noirBtn);
        overlay.appendChild(colorBtn);

        document.body.appendChild(overlay);
        return overlay;
    }

    /**
     * Check if the current item is SpiderNoir content and fetch version info.
     */
    function checkAndShowOverlay() {
        if (!window.ApiClient || !window.Dashboard) {
            // Jellyfin web globals not ready yet
            return;
        }

        // Try to find the current playback item from the player
        var videoPlayer = document.querySelector('video');
        if (!videoPlayer) {
            return;
        }

        // Find the current item ID from the page URL or a data attribute
        var itemId = getCurrentItemId();
        if (!itemId) {
            return;
        }

        var overlay = createOverlay();

        // Fetch version info from the plugin API
        var apiClient = window.ApiClient;
        apiClient.getJSON(apiClient.getUrl('SpiderNoir/versions/' + itemId)).then(function (result) {
            if (!result.hasVersions) {
                overlay.classList.remove('visible');
                return;
            }

            var noirBtn = overlay.querySelector('.noir-btn');
            var colorBtn = overlay.querySelector('.color-btn');

            noirBtn.classList.toggle('active', result.currentVersion === 'noir');
            colorBtn.classList.toggle('active', result.currentVersion === 'color');

            // Set up click handlers
            noirBtn.onclick = function () {
                switchVersion(itemId, 'noir');
            };

            colorBtn.onclick = function () {
                switchVersion(itemId, 'color');
            };

            overlay.classList.add('visible');
        }).catch(function () {
            overlay.classList.remove('visible');
        });
    }

    /**
     * Extract the current item ID from the DOM or URL.
     */
    function getCurrentItemId() {
        // Try page URL pattern: /video?id=xxx&...
        var params = new URLSearchParams(window.location.search);
        var id = params.get('id');
        if (id) {
            return id;
        }

        // Try data attributes on common Jellyfin player elements
        var playerWrapper = document.querySelector('.videoPlayerContainer');
        if (playerWrapper && playerWrapper.dataset.itemId) {
            return playerWrapper.dataset.itemId;
        }

        // Try the now playing bar
        var nowPlayingItem = document.querySelector('.nowPlayingBar');
        if (nowPlayingItem && nowPlayingItem.dataset.itemId) {
            return nowPlayingItem.dataset.itemId;
        }

        return null;
    }

    /**
     * Call the plugin API to switch to the target version.
     */
    function switchVersion(itemId, targetVersion) {
        var apiClient = window.ApiClient;

        apiClient.ajax({
            type: 'POST',
            url: apiClient.getUrl('SpiderNoir/switch/' + itemId + '/' + targetVersion)
        }).then(function (result) {
            if (result.success && result.targetPath) {
                // Reload the player with the new version
                var currentTime = 0;
                var videoPlayer = document.querySelector('video');
                if (videoPlayer) {
                    currentTime = videoPlayer.currentTime;
                }

                // Navigate to the alternate version
                // Use Jellyfin's internal playback manager if available
                if (window.PlaybackManager) {
                    window.PlaybackManager.play({
                        Path: result.targetPath,
                        ItemId: itemId,
                        StartPositionTicks: Math.round(currentTime * 10000000)
                    });
                } else {
                    // Fallback: reload the current page (simplest approach)
                    window.location.reload();
                }

                Dashboard.showLoadingMsg();
            }
        }).catch(function (err) {
            console.error('SpiderNoir: Failed to switch version', err);
            Dashboard.alert({
                message: 'Failed to switch version. Please try again.',
                title: 'SpiderNoir'
            });
        });
    }

    /**
     * Initialize the overlay when the DOM is ready.
     */
    function init() {
        injectStyles();
        createOverlay();

        // Check periodically for SpiderNoir content
        var checkInterval = setInterval(function () {
            checkAndShowOverlay();
        }, 2000);

        // Stop checking after 60 seconds if nothing found
        setTimeout(function () {
            clearInterval(checkInterval);
        }, 60000);

        // Also check when the page visibility changes (user navigates back)
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                checkAndShowOverlay();
            }
        });
    }

    // Run init when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
