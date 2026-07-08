/**
 * SpiderNoir Player Overlay
 *
 * Integrates a Noir / Color version switcher button into the Jellyfin video
 * player's OSD (On-Screen Display) controls bar. Shows a DVD icon button
 * that opens a dropdown with version options.
 *
 * Uses MutationObserver to detect the player OSD lifecycle and inserts the
 * button into the existing OSD controls bar (.buttons.focuscontainer-x)
 * rather than using a floating fixed-position element.
 *
 * Dependencies: ApiClient (global Jellyfin web object), Dashboard (global)
 * No jQuery required.
 */
(function () {
    'use strict';

    var SPIDER_NOIR_PLUGIN_ID = '3bd33ef7-dd55-485b-9487-8bce0b52bd55';
    var STYLE_ID = 'spiderNoirStyles';
    var BTN_CLASS = 'btnSpiderNoir';
    var MENU_ID = 'spiderNoirMenu';
    var observer = null;
    var currentItemId = null;
    var inserted = false;

    /* ── Styles ─────────────────────────────────────────────────── */

    function injectStyles() {
        if (document.getElementById(STYLE_ID)) return;

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = [
            /* OSD button — matches Jellyfin's paper-icon-button-light style */
            '.' + BTN_CLASS + ' {',
            '  position: relative !important;',
            '  display: inline-flex !important;',
            '  align-items: center;',
            '  justify-content: center;',
            '}',
            '.' + BTN_CLASS + ' .sn-icon {',
            '  width: 22px; height: 22px;',
            '  background-image: url(/SpiderNoir/icon.svg);',
            '  background-size: contain;',
            '  background-repeat: no-repeat;',
            '  background-position: center;',
            '  filter: brightness(0.8);',
            '  transition: filter 0.2s ease;',
            '  pointer-events: none;',
            '}',
            '.' + BTN_CLASS + ':hover .sn-icon { filter: brightness(1); }',
            '.' + BTN_CLASS + '.active .sn-icon { filter: brightness(1) drop-shadow(0 0 4px #00a4dc); }',

            /* Dropdown menu */
            '#' + MENU_ID + ' {',
            '  display: none;',
            '  position: absolute;',
            '  bottom: 44px;',
            '  right: 0;',
            '  min-width: 140px;',
            '  background: rgba(20,20,25,0.95);',
            '  border: 1px solid rgba(255,255,255,0.12);',
            '  border-radius: 8px;',
            '  padding: 4px 0;',
            '  backdrop-filter: blur(8px);',
            '  box-shadow: 0 4px 20px rgba(0,0,0,0.5);',
            '  overflow: hidden;',
            '  z-index: 999999;',
            '}',
            '#' + MENU_ID + '.open { display: block; }',

            '#' + MENU_ID + ' .sn-menu-item {',
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
            '#' + MENU_ID + ' .sn-menu-item:hover { background: rgba(255,255,255,0.08); color: #fff; }',
            '#' + MENU_ID + ' .sn-menu-item .sn-check {',
            '  width: 18px; text-align: center; font-size: 14px;',
            '  color: #00a4dc; flex-shrink: 0;',
            '}',
            '#' + MENU_ID + ' .sn-menu-item .sn-check.hidden { visibility: hidden; }',
            '#' + MENU_ID + ' .sn-menu-item .sn-label { flex: 1; }',
            '#' + MENU_ID + ' .sn-menu-divider {',
            '  height: 1px; background: rgba(255,255,255,0.08); margin: 4px 0;',
            '}',
            '#' + MENU_ID + ' .sn-menu-header {',
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

    /* ── Item ID detection ─────────────────────────────────────── */

    function getCurrentItemId() {
        // 1. PlaybackManager (most reliable when player is active)
        try {
            if (window.PlaybackManager) {
                var item = window.PlaybackManager.currentMediaItem
                    || window.PlaybackManager.currentPlaylistItem;
                if (item && item.Id) return item.Id;
            }
        } catch (_) { /* PlaybackManager may not be fully initialized */ }

        // 2. videoPlayerContainer dataset
        var wrapper = document.querySelector('.videoPlayerContainer');
        if (wrapper && wrapper.dataset.itemId) return wrapper.dataset.itemId;

        // 3. OSD page dataset
        var osdPage = document.querySelector('#videoOsdPage');
        if (osdPage && osdPage.dataset.itemId) return osdPage.dataset.itemId;

        // 4. URL search params
        var params = new URLSearchParams(window.location.search);
        var id = params.get('id');
        if (id) return id;

        // 5. Now playing bar
        var nowPlaying = document.querySelector('.nowPlayingBar');
        if (nowPlaying && nowPlaying.dataset.itemId) return nowPlaying.dataset.itemId;

        return null;
    }

    /* ── Button creation ───────────────────────────────────────── */

    function createButton() {
        var btn = document.createElement('button');
        btn.is = 'paper-icon-button-light';
        btn.className = BTN_CLASS + ' autoSize paper-icon-button-light';
        btn.title = 'Switch version';

        var icon = document.createElement('div');
        icon.className = 'sn-icon';
        btn.appendChild(icon);

        // Click to toggle dropdown
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            var menu = document.getElementById(MENU_ID);
            if (menu) menu.classList.toggle('open');
        });

        return btn;
    }

    function createMenu() {
        var menu = document.createElement('div');
        menu.id = MENU_ID;
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

        // Close menu on outside click
        menu.addEventListener('click', function (e) {
            e.stopPropagation();
        });

        // Menu item click handling
        menu.querySelectorAll('.sn-menu-item').forEach(function (item) {
            item.addEventListener('click', function () {
                var version = this.dataset.version;
                if (currentItemId) {
                    switchVersion(currentItemId, version);
                }
                menu.classList.remove('open');
            });
        });

        return menu;
    }

    /* ── Insert into OSD controls bar ──────────────────────────── */

    function insertIntoOsd() {
        if (inserted) return;
        var buttonsBar = document.querySelector('.buttons.focuscontainer-x');
        if (!buttonsBar) return;

        // Remove old floating overlay if it exists (migration from v1)
        var oldOverlay = document.getElementById('spiderNoirOverlay');
        if (oldOverlay) oldOverlay.parentNode.removeChild(oldOverlay);

        // Don't add if we already have our button
        if (buttonsBar.querySelector('.' + BTN_CLASS)) {
            inserted = true;
            return;
        }

        // Insert our button before the fullscreen button (last in the bar)
        var fullscreenBtn = buttonsBar.querySelector('.btnFullscreen');
        var btn = createButton();
        var menu = createMenu();

        // Wrap both in a container for positioning
        var wrapper = document.createElement('div');
        wrapper.style.cssText = 'position:relative;display:inline-flex;';
        wrapper.appendChild(menu);
        wrapper.appendChild(btn);

        if (fullscreenBtn) {
            buttonsBar.insertBefore(wrapper, fullscreenBtn);
        } else {
            buttonsBar.appendChild(wrapper);
        }

        inserted = true;
        currentItemId = getCurrentItemId();
        checkVersions();

        // Close menu on any outside click
        document.addEventListener('click', function (e) {
            var menu = document.getElementById(MENU_ID);
            if (menu && !wrapper.contains(e.target)) {
                menu.classList.remove('open');
            }
        });
    }

    /* ── Check versions via API ────────────────────────────────── */

    function checkVersions() {
        var itemId = getCurrentItemId();
        if (!itemId || !window.ApiClient) return;
        currentItemId = itemId;

        var apiClient = window.ApiClient;
        apiClient.getJSON(apiClient.getUrl('SpiderNoir/versions/' + itemId)).then(function (result) {
            var btn = document.querySelector('.' + BTN_CLASS);
            if (!btn) return;

            if (!result.hasVersions) {
                btn.style.display = 'none';
                return;
            }

            btn.style.display = '';
            var isNoir = result.currentVersion === 'noir';
            btn.classList.toggle('active', isNoir);

            var menu = document.getElementById(MENU_ID);
            if (menu) {
                menu.querySelectorAll('.sn-menu-item').forEach(function (item) {
                    var version = item.dataset.version;
                    var check = item.querySelector('.sn-check');
                    check.classList.toggle('hidden', version !== result.currentVersion);
                });
            }
        }).catch(function () {
            var btn = document.querySelector('.' + BTN_CLASS);
            if (btn) btn.style.display = 'none';
        });
    }

    /* ── Version switching ─────────────────────────────────────── */

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
                    window.location.href = window.location.href.split('?')[0]
                        + '?id=' + itemId + '&version=' + targetVersion;
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

    /* ── MutationObserver setup ────────────────────────────────── */

    function setupObserver() {
        if (observer) observer.disconnect();

        observer = new MutationObserver(function () {
            var osd = document.querySelector('.videoOsdBottom');
            var buttonsBar = document.querySelector('.buttons.focuscontainer-x');
            var playerPage = document.querySelector('#videoOsdPage');

            if (osd && buttonsBar && playerPage) {
                insertIntoOsd();

                // Re-check when OSD visibility changes
                if (!osd.classList.contains('hide')) {
                    var newId = getCurrentItemId();
                    if (newId && newId !== currentItemId) {
                        currentItemId = newId;
                        checkVersions();
                    }
                }
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'data-itemid']
        });
    }

    /* ── Playback event listeners ──────────────────────────────── */

    function setupPlaybackEvents() {
        if (window.Events && window.ApiClient) {
            window.Events.on(window.ApiClient, 'playbackstart', function () {
                setTimeout(checkVersions, 500);
            });
            window.Events.on(window.ApiClient, 'playbackstop', function () {
                var btn = document.querySelector('.' + BTN_CLASS);
                if (btn) btn.style.display = 'none';
            });
        }
    }

    /* ── Init ──────────────────────────────────────────────────── */

    function init() {
        injectStyles();
        setupObserver();
        setupPlaybackEvents();

        // Also check on visibility change (tab switch)
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden && document.querySelector('#videoOsdPage')) {
                var newId = getCurrentItemId();
                if (newId && newId !== currentItemId) {
                    currentItemId = newId;
                    checkVersions();
                }
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
