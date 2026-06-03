import { ApiClient } from '../api-client/api-client';

export const apiClientProvider = {
    provide: ApiClient,
    useClass: ApiClient,
};
