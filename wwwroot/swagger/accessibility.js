document.addEventListener('DOMContentLoaded', () => {
    const swagger = document.getElementById('swagger-ui');
    if (!swagger) return;

    const main = document.createElement('main');
    const title = document.createElement('h1');
    title.className = 'api-documentation-title';
    title.textContent = document.title;
    swagger.before(main);
    main.append(title, swagger);

    function labelServerSelect() {
        const select = swagger.querySelector('select#servers');
        if (select && !select.getAttribute('aria-label')) {
            select.setAttribute('aria-label', 'Servers');
        }
    }

    // Swagger renders and replaces the selector as the OpenAPI document changes.
    new MutationObserver(labelServerSelect).observe(swagger, { childList: true, subtree: true });
    labelServerSelect();
});
