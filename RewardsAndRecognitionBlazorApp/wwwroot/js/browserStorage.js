window.browserStorage = {
    getItem: function (key) {
        try {
            return sessionStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },
    setItem: function (key, value) {
        try {
            sessionStorage.setItem(key, value);
        } catch (e) {
            // ignore
        }
    },
    removeItem: function (key) {
        try {
            sessionStorage.removeItem(key);
        } catch (e) {
            // ignore
        }
    },
    clear: function () {
        try {
            sessionStorage.clear();
        } catch (e) {
            // ignore
        }
    }
};
