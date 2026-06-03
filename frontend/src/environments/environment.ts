export const environment = {
    production: false,
    login: {},
    services: {
        api: {
            url: 'https://localhost:5000',
        },
        notifications: {
            url: 'http://127.0.0.1:5002/messagehub',
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
