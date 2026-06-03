import { LinkPreviewDto } from '../api-client/api-client';

export class LinkPreview {
    accountId?: string;
    title?: string | undefined;
    imageUrl?: string | undefined;
    url?: string | undefined;

    constructor(init?: Partial<LinkPreview | LinkPreviewDto>) {
        Object.assign(this, init);
    }
}
