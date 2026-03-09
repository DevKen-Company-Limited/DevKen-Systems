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
            clientId: isProduction
                ? '1028030540525-43fv3db9fkprrmb3q4r374ruok828m5r.apps.googleusercontent.com'
                : '1028030540525-43fv3db9fkprrmb3q4r374ruok828m5r.apps.googleusercontent.com',
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
};