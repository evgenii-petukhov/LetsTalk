import { Component, inject, Input, OnChanges, signal } from '@angular/core';
import { IImageDto } from '../../../api-client/api-client';
import { errorMessages } from '../../../constants/errors';
import { ErrorService } from '../../../services/error.service';
import { StoreService } from '../../../services/store.service';

@Component({
    selector: 'app-avatar',
    templateUrl: './avatar.component.html',
    styleUrls: ['./avatar.component.scss'],
    standalone: false,
})
export class AvatarComponent implements OnChanges {
    @Input() urlOptions: (string | IImageDto)[] | null = null;
    backgroundImage = signal('');

    private readonly defaultPhotoUrl = 'images/empty-avatar.svg';
    private readonly storeService = inject(StoreService);
    private readonly errorService = inject(ErrorService);

    ngOnChanges(): void {
        this.processUrlOptions();
    }

    private async processUrlOptions(): Promise<void> {
        const urlOptions = this.urlOptions?.filter(Boolean) ?? [];

        if (urlOptions.length === 0) {
            this.setBackgroundImage(this.defaultPhotoUrl);
            return;
        }

        const primaryOption = urlOptions[0];

        if (this.isImageDto(primaryOption)) {
            await this.loadImageFromStore(primaryOption);
        } else if (this.isValidUrl(primaryOption)) {
            this.setBackgroundImage(primaryOption, this.defaultPhotoUrl);
        } else {
            this.setBackgroundImage(this.defaultPhotoUrl);
        }
    }

    private async loadImageFromStore(imageKey: IImageDto): Promise<void> {
        try {
            const image = await this.storeService.getImageContent(imageKey);
            this.setBackgroundImage(image.content);
        } catch (error) {
            this.setBackgroundImage(this.defaultPhotoUrl);
            this.errorService.handleError(error, errorMessages.downloadImage);
        }
    }

    private isImageDto(value: string | IImageDto): value is IImageDto {
        return value && typeof value === 'object' && 'id' in value && 'fileStorageTypeId' in value;
    }

    private isValidUrl(value: string): boolean {
        return typeof value === 'string' && (value.startsWith('http') || value.startsWith('blob:https'));
    }

    private setBackgroundImage(...urls: string[]): void {
        this.backgroundImage.set(urls.map((url) => `url('${url}')`).join(', '));
    }
}
