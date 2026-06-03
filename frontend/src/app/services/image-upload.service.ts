import { inject, Injectable } from '@angular/core';
import { ImageService } from './image.service';
import { FileStorageService } from './file-storage.service';
import { ImageRoles, UploadImageResponse } from '../protos/file_upload_pb';

@Injectable({
    providedIn: 'root',
})
export class ImageUploadService {
    private readonly imageService = inject(ImageService);
    private readonly fileStorageService = inject(FileStorageService);

    async resizeAndUploadImage(
        photoUrl: string,
        width: number,
        height: number,
        imageRole: ImageRoles,
    ): Promise<UploadImageResponse> {
        const blob = await this.resizeAvatar(photoUrl, width, height);
        return this.fileStorageService.uploadImageAsBlob(blob, imageRole);
    }

    private resizeAvatar(
        photoUrl: string,
        maxWidth: number,
        maxHeight: number,
    ): Promise<Blob> {
        return photoUrl
            ? this.imageService.resizeBase64Image(photoUrl, maxWidth, maxHeight)
            : Promise.resolve(null);
    }
}
