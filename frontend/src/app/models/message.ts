import { IMessageDto } from '../api-client/api-client';
import { LinkPreview } from './link-preview';
import { isOfType } from '../helpers/type-utils.helper';
import { getLocalDate } from '../helpers/date-utils.helper';
import { Image } from './image';
import { ImagePreview } from './image-preview';

export class Message {
    id?: string;
    text?: string | undefined;
    textHtml?: string | undefined;
    recipientId?: string;
    isMine?: boolean | undefined;
    created?: Date;
    linkPreview?: LinkPreview;
    image?: Image;
    imagePreview?: ImagePreview;
    chatId?: string;
    emojisOnly?: boolean;
    emojiCount?: number;

    constructor(...inits: Partial<Message | IMessageDto>[]) {
        inits
            .filter((init) => init)
            .forEach((init) => {
                const linkPreview = this.linkPreview ?? init?.linkPreview;
                const image = this.image ?? init?.image;
                const imagePreview = this.imagePreview ?? init?.imagePreview;
                const created = this.created ?? init?.created;
                Object.assign(this, init);
                if (linkPreview) {
                    this.linkPreview = isOfType(linkPreview, LinkPreview)
                        ? linkPreview
                        : new LinkPreview(linkPreview);
                }
                if (image) {
                    this.image = isOfType(image, Image)
                        ? image
                        : new Image(image);
                }
                if (imagePreview) {
                    this.imagePreview = isOfType(imagePreview, ImagePreview)
                        ? imagePreview
                        : new ImagePreview(imagePreview);
                }
                if (created) {
                    this.created = isOfType(created, Date)
                        ? (created as Date)
                        : getLocalDate(created as number);
                }
            });
    }
}
