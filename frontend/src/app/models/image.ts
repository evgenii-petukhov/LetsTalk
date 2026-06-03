import { ImageDto } from '../api-client/api-client';

export class Image {
    id?: string;
    fileStorageTypeId?: number;

    constructor(init?: Partial<Image | ImageDto>) {
        Object.assign(this, init);
    }
}
