export const environment = {
    production: true,
    login: {},
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
