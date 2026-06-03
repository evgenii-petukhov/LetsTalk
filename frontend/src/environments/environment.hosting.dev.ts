export const environment = {
    production: false,
    login: {},
    services: {
        api: {
            url: 'https://chatapi.petukhov.fyi',
        },
        notifications: {
            url: 'https://chatnotifications.petukhov.fyi/messagehub',
            connectionInterval: 5000,
        },
        fileStorage: {
            url: 'https://chatfilestorage.petukhov.fyi',
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
