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

    sso: {
        google: {
            clientId: '1028030540525-43fv3db9fkprrmb3q4r374ruok828m5r.apps.googleusercontent.com',
            projectId: 'utility-destiny-470214-d3',
            authUri: 'https://accounts.google.com/o/oauth2/auth',
            tokenUri: 'https://oauth2.googleapis.com/token',
            authProviderCertUrl: 'https://www.googleapis.com/oauth2/v1/certs',

            redirectUris: isProduction
                ? ['https://dev-ken-systems.vercel.app/example']
                : ['http://localhost:4200/example'],

            javascriptOrigins: isProduction
                ? ['https://dev-ken-systems.vercel.app']
                : ['http://localhost:4200'],
        },
    },

    pesaPal: {
        consumerKey: isProduction
            ? 'osGQ364R49cXKeOYSpaOnT++rHs='
            : 'osGQ364R49cXKeOYSpaOnT++rHs=',

        consumerSecret: isProduction
            ? 'osGQ364R49cXKeOYSpaOnT++rHs='
            : 'qkio1BGGYAXTu2JOfm7XSXNruoZsrqEW',

        baseUrl: isProduction
            ? 'https://pay.pesapal.com/v3'
            : 'https://cybqa.pesapal.com/pesapalv3',

        ipnUrl: isProduction
            ? 'https://devken-systems.onrender.com/api/pesapal/ipn'
            : 'https://localhost:44383/api/pesapal/ipn',

        callbackUrl: isProduction
            ? 'https://dev-ken-systems.vercel.app/pesapal/callback'
            : 'http://localhost:4200/pesapal/callback',
    }
};