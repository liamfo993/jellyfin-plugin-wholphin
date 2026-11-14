const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

test('new rows use client defaults and custom options can be reset', async () => {
    class Element {
        constructor(tag) {
            this.tag = tag;
            this.children = [];
            this.listeners = {};
            this.dataset = {};
            this.value = '';
        }
        appendChild(child) {
            this.children.push(child);
            if (this.tag === 'select' && (!this.value || child.selected)) this.value = child.value;
            return child;
        }
        replaceChildren() { this.children = []; }
        setAttribute() {}
        addEventListener(name, callback) { this.listeners[name] = callback; }
        fire(name) { this.listeners[name]({ preventDefault() {} }); }
    }

    const elements = new Map();
    const document = {
        createElement: tag => new Element(tag),
        getElementById(id) {
            if (!elements.has(id)) elements.set(id, new Element('div'));
            return elements.get(id);
        },
    };
    const configuration = { HomeConfig: { HomePageSettings: { Rows: [] } }, SeerrConfig: {} };
    const api = {
        getCurrentUserId: () => null,
        getPluginConfiguration: () => Promise.resolve(configuration),
        getJSON: () => Promise.resolve({}),
        getUrl: path => path,
    };
    const dashboard = { showLoadingMsg() {}, hideLoadingMsg() {}, alert(e) { throw Error(e.message); } };
    const html = fs.readFileSync('Jellyfin.Plugin.Wholphin/Pages/HomeSettings/index.html', 'utf8');
    const script = html.match(/<script>([\s\S]*?)<\/script>/)[1];
    vm.runInNewContext(script, { document, ApiClient: api, Dashboard: dashboard, Promise });
    document.getElementById('wholphinConfigurationPage').fire('pageshow');
    await new Promise(resolve => setImmediate(resolve));

    document.getElementById('addRow').fire('click');
    document.getElementById('addKidsRows').fire('click');
    const rows = configuration.HomeConfig.HomePageSettings.Rows;
    assert.equal(rows.length, 5);
    assert.ok(rows.every(row => row.viewOptions === undefined), 'rendering must not create overrides');

    function find(element, predicate) {
        if (predicate(element)) return element;
        for (const child of element.children) {
            const result = find(child, predicate);
            if (result) return result;
        }
        return null;
    }
    const list = document.getElementById('rowList');
    const height = find(list.children[0], e => e.id === 'heightDp-0');
    height.value = '200';
    height.fire('change');
    assert.equal(rows[0].viewOptions.heightDp, 200);
    assert.equal(rows[0].viewOptions.spacing, 16);
    assert.equal(rows[1].viewOptions, undefined);
    find(list.children[0], e => e.tag === 'button' && e.textContent === 'Use Wholphin defaults').fire('click');
    assert.equal(rows[0].viewOptions, null);

    rows[1].viewOptions = { heightDp: 180, spacing: 10 };
    document.getElementById('resetRowDisplays').fire('click');
    assert.ok(rows.every(row => row.viewOptions === null), 'reset all must clear each override');
});
