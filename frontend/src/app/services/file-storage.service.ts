import { inject, Injectable } from '@angular/core';
import { FileUploadGrpcEndpointClient } from '../protos/file_upload_grpc_web_pb';
import {
    DownloadImageRequest,
    DownloadImageResponse,
    ImageRoles,
    UploadImageRequest,
    UploadImageResponse,
} from '../protos/file_upload_pb';
import { environment } from '../../environments/environment';
import { TokenStorageService } from './token-storage.service';
import { IImageDto } from '../api-client/api-client';

@Injectable({
    providedIn: 'root',
})
export class FileStorageService {
    private tokenStorageService = inject(TokenStorageService);
    private fileUploadService = new FileUploadGrpcEndpointClient(
        environment.services.fileStorage.url,
    );

    async uploadImageAsBlob(
        blob: Blob,
        imageRole: ImageRoles,
    ): Promise<UploadImageResponse> {
        const buffer = await blob.arrayBuffer();
        return await this.upload(new Uint8Array(buffer), imageRole);
    }

    upload(
        content: Uint8Array,
        imageRole: ImageRoles,
    ): Promise<UploadImageResponse> {
        const request = new UploadImageRequest();
        request.setContent(content);
        request.setImageRole(imageRole);
        return new Promise((resolve, reject) => {
            this.fileUploadService.uploadImageAsync(
                request,
                { authorization: this.tokenStorageService.getToken() },
                (err, resp) => {
                    if (err) reject(err);
                    else resolve(resp);
                },
            );
        });
    }

    download(imageKey: IImageDto): Promise<DownloadImageResponse> {
        const request = new DownloadImageRequest();
        request.setImageId(imageKey.id);
        request.setFileStorageTypeId(imageKey.fileStorageTypeId);
        return new Promise((resolve, reject) => {
            this.fileUploadService.downloadImageAsync(
                request,
                { authorization: this.tokenStorageService.getToken() },
                (err, resp) => {
                    if (err) reject(err);
                    else resolve(resp);
                },
            );
        });
    }
}
