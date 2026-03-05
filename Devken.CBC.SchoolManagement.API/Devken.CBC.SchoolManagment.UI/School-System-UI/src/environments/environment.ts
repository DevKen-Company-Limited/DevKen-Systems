const isProduction = (() => {
    if (typeof window === 'undefined') return false;

    const host = window.location.hostname;
    return host !== 'localhost' && host !== '127.0.0.1';
})();

export const environment = {
    production: isProduction,
    apiUrl: isProduction
        ? 'https://devken-systems.onrender.com'
        : 'https://localhost:44383',
};