window.fetchWithCredentials = async function (method, url, body) {
    // If url is relative, prepend API base URL if available
    try {
        if (url && url.startsWith("/")) {
            const base = window.apiBaseUrl || '';
            url = base + url;
        } else if (url && !url.startsWith('http')) {
            const base = window.apiBaseUrl || '';
            url = base + (url.startsWith('/') ? url : '/' + url);
        }
    } catch (e) {
        // ignore
    }

    const opts = {
        method: method,
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json'
        }
    };
    if (body) opts.body = JSON.stringify(body);

    const resp = await fetch(url, opts);
    const text = await resp.text();
    return { status: resp.status, ok: resp.ok, text };
};
