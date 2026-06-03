import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { IImageDto } from '../../../api-client/api-client';
import { errorMessages } from '../../../constants/errors';
import { ImagePreview } from '../../../models/image-preview';
import { ErrorService } from '../../../services/error.service';
import { StoreService } from '../../../services/store.service';
import { environment } from '../../../../environments/environment';

@Component({
    selector: 'app-image',
    templateUrl: './image.component.html',
    styleUrls: ['./image.component.scss'],
    standalone: false,
})
export class ImageComponent implements OnInit {
    @Input() imagePreview: ImagePreview;
    @Input() imageKey: IImageDto;
    @Input() chatId: string;
    url = signal<string | null>(null);
    isLoading = signal(true);
    isSizeUnknown = signal(false);
    width = signal(environment.imageSettings.limits.picturePreview.width);
    height = signal(environment.imageSettings.limits.picturePreview.height);
    sizeLimit = environment.imageSettings.limits.picturePreview;
    imageKeyParam = signal<string | null>(null);

    private readonly storeService = inject(StoreService);
    private readonly errorService = inject(ErrorService);

    async ngOnInit(): Promise<void> {
        const imageKeyParam = this.imageKey
            ? `${this.imageKey.id}_${this.imageKey.fileStorageTypeId}`
            : null;

        this.imageKeyParam.set(imageKeyParam);

        try {
            if (!this.imagePreview) {
                return;
            }

            this.setSize(this.imagePreview.width, this.imagePreview.height);

            if (this.imagePreview.id) {
                const image = await this.storeService.getImageContent(
                    this.imagePreview,
                );
                if (image) {
                    this.url.set(image.content);
                    this.setSize(image.width, image.height);
                    this.isLoading.set(false);
                }
            }
        } catch (e) {
            this.errorService.handleError(e, errorMessages.downloadImage);
            this.url.set(null);
            this.isLoading.set(false);
        }
    }

    private setSize(width: number, height: number): void {
        if (!width || !height) {
            this.isSizeUnknown.set(true);
            return;
        }

        this.isSizeUnknown.set(false);
        const scale = Math.min(
            this.sizeLimit.width / width,
            this.sizeLimit.height / height,
            1,
        );
        this.width.set(Math.round(scale * width));
        this.height.set(Math.round(scale * height));
    }
}
