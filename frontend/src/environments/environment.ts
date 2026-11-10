export const environment = {
    production: false,
    login: {},
    session: {
        // The app cannot read the HttpOnly cookie, so it assumes the lifetime of JwtSettings:AccessTokenLifetime
        // to renew early. Keep it equal to that setting; a longer value only delays the renewal until the server
        // answers 401, which the HTTP interceptor and the notifications reconnect handle.
        // 86400 seconds is one day, the "AccessTokenLifetime": "1.00:00:00" (days.hours:minutes:seconds) in
        // appsettings.Development.json of the Authentication service.
        accessTokenLifetimeInSeconds: 86400,
        renewalMarginInSeconds: 60,
    },
    services: {
        api: {
            url: 'https://127.0.0.1:5000',
        },
        notifications: {
            url: 'https://127.0.0.1:5002/messagehub',
            connectionInterval: 5000,
        },
        fileStorage: {
            url: 'https://127.0.0.1:5003',
        },
    },
    imageSettings: {
        limits: {
            avatar: {
                width: 512,
                height: 512,
            },
            picture: {
                width: 4000,
                height: 4000,
            },
            picturePreview: {
                width: 150,
                height: 150,
            },
        },
    },
};
