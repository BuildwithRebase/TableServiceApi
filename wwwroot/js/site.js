// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


(function () {

    const loadScript = (src) => {
        const script = document.createElement('script');
        script.src = src;
        script.async = false;
        script.defer = false;

        document.body.appendChild(script);
    }

    document.addEventListener('DOMContentLoaded', function () {

        if (!window.jQuery) {
            loadScript('https://code.jquery.com/jquery-3.5.1.min.js');
        }
        loadScript('https://cdnjs.cloudflare.com/ajax/libs/uuid/8.1.0/uuidv4.min.js');
        loadScript('https://cdn.jsdelivr.net/npm/handlebars@latest/dist/handlebars.js');

        loadScript('/js/tableservice-api.js');
        loadScript('/js/rebase-bundle.js');

    });

})();
