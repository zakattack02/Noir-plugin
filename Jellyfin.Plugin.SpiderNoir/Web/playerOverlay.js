/**
 * SpiderNoir Player Overlay
 *
 * Adds a Noir / Color version switcher to the Jellyfin video player.
 * Shows a DVD icon button that opens a dropdown menu with version options.
 *
 * Dependencies: ApiClient (global Jellyfin web object), Dashboard (global)
 * No jQuery required.
 */
(function () {
    'use strict';

    var SPIDER_NOIR_PLUGIN_ID = '3bd33ef7-dd55-485b-9487-8bce0b52bd55';
    var OVERLAY_ID = 'spiderNoirOverlay';
    var STYLE_ID = 'spiderNoirStyles';

    function injectStyles() {
        if (document.getElementById(STYLE_ID)) return;

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = [
            '#' + OVERLAY_ID + ' {',
            '  position: fixed;',
            '  bottom: 80px;',
            '  right: 24px;',
            '  z-index: 999999;',
            '  display: none;',
            '  flex-direction: column;',
            '  align-items: flex-end;',
            '  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;',
            '}',
            '#' + OVERLAY_ID + '.visible { display: flex; }',

            /* Toggle button */
            '#' + OVERLAY_ID + ' .sn-toggle-btn {',
            '  width: 40px; height: 40px;',
            '  padding: 6px;',
            '  border: 2px solid rgba(255,255,255,0.15);',
            '  border-radius: 50%;',
            '  background: rgba(0,0,0,0.65);',
            '  cursor: pointer;',
            '  display: flex; align-items: center; justify-content: center;',
            '  transition: all 0.2s ease;',
            '  backdrop-filter: blur(4px);',
            '}',
            '#' + OVERLAY_ID + ' .sn-toggle-btn:hover {',
            '  border-color: rgba(255,255,255,0.4);',
            '  background: rgba(0,0,0,0.8);',
            '}',
            '#' + OVERLAY_ID + ' .sn-toggle-btn svg {',
            '  width: 22px; height: 22px;',
            '  fill: #ccc;',
            '  transition: fill 0.2s ease;',
            '}',
            '#' + OVERLAY_ID + ' .sn-toggle-btn:hover svg { fill: #fff; }',
            '#' + OVERLAY_ID + ' .sn-toggle-btn.active {',
            '  border-color: #00a4dc;',
            '  background: rgba(0,164,220,0.2);',
            '}',
            '#' + OVERLAY_ID + ' .sn-toggle-btn.active svg { fill: #00a4dc; }',

            /* Dropdown menu */
            '#' + OVERLAY_ID + ' .sn-menu {',
            '  display: none;',
            '  position: absolute;',
            '  bottom: 48px;',
            '  right: 0;',
            '  min-width: 140px;',
            '  background: rgba(20,20,25,0.95);',
            '  border: 1px solid rgba(255,255,255,0.12);',
            '  border-radius: 8px;',
            '  padding: 4px 0;',
            '  backdrop-filter: blur(8px);',
            '  box-shadow: 0 4px 20px rgba(0,0,0,0.5);',
            '  overflow: hidden;',
            '}',
            '#' + OVERLAY_ID + ' .sn-menu.open { display: block; }',

            '#' + OVERLAY_ID + ' .sn-menu-item {',
            '  padding: 10px 14px;',
            '  cursor: pointer;',
            '  color: #ccc;',
            '  font-size: 13px;',
            '  font-weight: 500;',
            '  display: flex; align-items: center; gap: 8px;',
            '  transition: background 0.15s ease;',
            '  white-space: nowrap;',
            '  border: none; background: none; width: 100%; text-align: left;',
            '}',
            '#' + OVERLAY_ID + ' .sn-menu-item:hover { background: rgba(255,255,255,0.08); color: #fff; }',
            '#' + OVERLAY_ID + ' .sn-menu-item .sn-check {',
            '  width: 18px; text-align: center; font-size: 14px;',
            '  color: #00a4dc; flex-shrink: 0;',
            '}',
            '#' + OVERLAY_ID + ' .sn-menu-item .sn-check.hidden { visibility: hidden; }',
            '#' + OVERLAY_ID + ' .sn-menu-item .sn-label { flex: 1; }',
            '#' + OVERLAY_ID + ' .sn-menu-divider {',
            '  height: 1px; background: rgba(255,255,255,0.08); margin: 4px 0;',
            '}',
            '#' + OVERLAY_ID + ' .sn-menu-header {',
            '  padding: 8px 14px 4px;',
            '  font-size: 10px;',
            '  text-transform: uppercase;',
            '  letter-spacing: 1px;',
            '  color: rgba(255,255,255,0.35);',
            '  font-weight: 600;',
            '}'
        ].join('\n');
        document.head.appendChild(style);
    }

    function createOverlay() {
        if (document.getElementById(OVERLAY_ID)) return document.getElementById(OVERLAY_ID);

        var overlay = document.createElement('div');
        overlay.id = OVERLAY_ID;

        /* Menu dropdown */
        var menu = document.createElement('div');
        menu.className = 'sn-menu';
        menu.innerHTML =
            '<div class="sn-menu-header">Version</div>' +
            '<button class="sn-menu-item" data-version="noir">' +
                '<span class="sn-check">&#10003;</span>' +
                '<span class="sn-label">Noir</span>' +
            '</button>' +
            '<button class="sn-menu-item" data-version="color">' +
                '<span class="sn-check">&#10003;</span>' +
                '<span class="sn-label">Color</span>' +
            '</button>';

        /* Toggle button */
        var btn = document.createElement('button');
        btn.className = 'sn-toggle-btn';
        btn.title = 'Switch version';
        btn.innerHTML = '';  // SVG loaded async
        btn.appendChild(document.createTextNode(''));

        // Fetch SVG icon from the API and inject it
        btn.style.backgroundImage = 'url(/SpiderNoir/icon.svg)';
        btn.style.backgroundSize = '22px 22px';
        btn.style.backgroundRepeat = 'no-repeat';
        btn.style.backgroundPosition = 'center';

        // Toggle menu on click
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            menu.classList.toggle('open');
        });

        // Menu item click handling
        menu.querySelectorAll('.sn-menu-item').forEach(function (item) {
            item.addEventListener('click', function () {
                var version = this.dataset.version;
                var itemId = overlay.dataset.currentItemId;
                if (itemId) {
                    switchVersion(itemId, version);
                }
                menu.classList.remove('open');
            });
        });

        overlay.appendChild(menu);
        overlay.appendChild(btn);

        document.body.appendChild(overlay);

        // Close menu on outside click
        document.addEventListener('click', function (e) {
            if (!overlay.contains(e.target)) {
                menu.classList.remove('open');
            }
        });

        return overlay;
    }

    function checkAndShowOverlay() {
        if (!window.ApiClient || !window.Dashboard) return;

        var videoPlayer = document.querySelector('video');
        if (!videoPlayer) return;

        var itemId = getCurrentItemId();
        if (!itemId) return;

        var overlay = createOverlay();
        overlay.dataset.currentItemId = itemId;

        var apiClient = window.ApiClient;
        apiClient.getJSON(apiClient.getUrl('SpiderNoir/versions/' + itemId)).then(function (result) {
            if (!result.hasVersions) {
                overlay.classList.remove('visible');
                return;
            }

            var menu = overlay.querySelector('.sn-menu');
            var btn = overlay.querySelector('.sn-toggle-btn');

            // Check if current version is Noir
            var isNoir = result.currentVersion === 'noir';
            btn.classList.toggle('active', isNoir);

            menu.querySelectorAll('.sn-menu-item').forEach(function (item) {
                var version = item.dataset.version;
                var check = item.querySelector('.sn-check');
                check.classList.toggle('hidden', version !== result.currentVersion);
            });

            overlay.classList.add('visible');
        }).catch(function () {
            overlay.classList.remove('visible');
        });
    }

    function getCurrentItemId() {
        var params = new URLSearchParams(window.location.search);
        var id = params.get('id');
        if (id) return id;

        var playerWrapper = document.querySelector('.videoPlayerContainer');
        if (playerWrapper && playerWrapper.dataset.itemId) return playerWrapper.dataset.itemId;

        var nowPlayingItem = document.querySelector('.nowPlayingBar');
        if (nowPlayingItem && nowPlayingItem.dataset.itemId) return nowPlayingItem.dataset.itemId;

        return null;
    }

    function switchVersion(itemId, targetVersion) {
        var apiClient = window.ApiClient;
        apiClient.ajax({
            type: 'POST',
            url: apiClient.getUrl('SpiderNoir/switch/' + itemId + '/' + targetVersion)
        }).then(function (result) {
            if (result.success && result.targetPath) {
                var currentTime = 0;
                var videoPlayer = document.querySelector('video');
                if (videoPlayer) currentTime = videoPlayer.currentTime;

                if (window.PlaybackManager) {
                    window.PlaybackManager.play({
                        Path: result.targetPath,
                        ItemId: itemId,
                        StartPositionTicks: Math.round(currentTime * 10000000)
                    });
                } else {
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

    function init() {
        injectStyles();
        createOverlay();

        var checkInterval = setInterval(function () {
            checkAndShowOverlay();
        }, 2000);

        setTimeout(function () {
            clearInterval(checkInterval);
        }, 60000);

        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) checkAndShowOverlay();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
