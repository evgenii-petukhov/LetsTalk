import { ImagePreviewDto } from '../api-client/api-client';

export class ImagePreview {
    messageId?: string;
    id?: string;
    accountId?: string;
    width?: number | undefined;
    height?: number | undefined;
    fileStorageTypeId?: number;

    constructor(init?: Partial<ImagePreview | ImagePreviewDto>) {
        Object.assign(this, init);
    }
}
