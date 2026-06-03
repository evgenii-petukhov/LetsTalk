import { Component, Input } from '@angular/core';
import { IImageDto } from '../../../api-client/api-client';
import { ImagePreview } from '../../../models/image-preview';

@Component({
    selector: 'app-image',
    template: '<div></div>',
    standalone: false,
})
export class ImageStubComponent {
    @Input() imagePreview: ImagePreview;
    @Input() imageKey: IImageDto;
    @Input() chatId: string;
}
