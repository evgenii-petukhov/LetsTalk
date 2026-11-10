export const environment = {
    production: true,
    login: {},
    session: {
        // The app cannot read the HttpOnly cookie, so it assumes the lifetime of JwtSettings:AccessTokenLifetime
        // to renew early. Keep it equal to that setting; a longer value only delays the renewal until the server
        // answers 401, which the HTTP interceptor and the notifications reconnect handle.
        accessTokenLifetimeInSeconds: 900,
        renewalMarginInSeconds: 60,
    },
    services: {
        api: {
            url: 'https://chatapi{{suffix}}.{{domain}}',
        },
        notifications: {
            url: 'https://chatnotifications{{suffix}}.{{domain}}/messagehub',
            connectionInterval: 5000,
        },
        fileStorage: {
            url: 'https://chatfilestorage{{suffix}}.{{domain}}',
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
