(function () {
    /**
     * Loads the rebase bundle script from Amazon AWS
     */
    var loadScript = function loadScript(src) {
        var script = document.createElement('script');
        script.src = src;

        document.body.appendChild(script);
    }

    document.addEventListener('DOMContentLoaded', function () {

        loadScript('https://code.jquery.com/jquery-3.5.1.min.js');
        loadScript('https://cdn.jsdelivr.net/npm/handlebars@latest/dist/handlebars.js');
        loadScript('https://unpkg.com/axios/dist/axios.min.js');
        loadScript('https://cdn.jsdelivr.net/npm/lodash@4.17.21/lodash.min.js');

        loadScript('https://appcore.buildwithrebase.online/js/rebase.bundle.js');
    });

})();
