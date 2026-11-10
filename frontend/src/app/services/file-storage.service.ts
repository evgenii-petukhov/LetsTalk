import { inject, Injectable } from '@angular/core';
import type { RpcError } from 'grpc-web';
import { FileUploadGrpcEndpointClient } from '../protos/file_upload_grpc_web_pb';
import {
    DownloadImageRequest,
    DownloadImageResponse,
    ImageRoles,
    UploadImageRequest,
    UploadImageResponse,
} from '../protos/file_upload_pb';
import { environment } from '../../environments/environment';
import { IImageDto } from '../api-client/api-client';
import { SessionService } from './session.service';

// gRPC status code UNAUTHENTICATED. grpc-web is a CommonJS module, so its StatusCode enum is not imported here.
const UNAUTHENTICATED = 16;

@Injectable({
    providedIn: 'root',
})
export class FileStorageService {
    private readonly sessionService = inject(SessionService);
    // withCredentials makes the browser send the HttpOnly session cookie with every call
    private fileUploadService = new FileUploadGrpcEndpointClient(
        environment.services.fileStorage.url,
        null,
        { withCredentials: true },
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
        return this.withRenewal((callback) =>
            this.fileUploadService.uploadImageAsync(request, {}, callback),
        );
    }

    download(imageKey: IImageDto): Promise<DownloadImageResponse> {
        const request = new DownloadImageRequest();
        request.setImageId(imageKey.id);
        request.setFileStorageTypeId(imageKey.fileStorageTypeId);
        return this.withRenewal((callback) =>
            this.fileUploadService.downloadImageAsync(request, {}, callback),
        );
    }

    // The access token lives only minutes: when the server answers UNAUTHENTICATED, renew the session and
    // call again, once. A rejected refresh token or a failed second call reaches the caller as it is.
    private async withRenewal<T>(
        call: (callback: (error: RpcError | null, response: T) => void) => unknown,
    ): Promise<T> {
        try {
            return await this.invoke(call);
        } catch (error) {
            if (
                (error as RpcError)?.code !== UNAUTHENTICATED ||
                !(await this.sessionService.renew())
            ) {
                throw error;
            }

            return await this.invoke(call);
        }
    }

    private invoke<T>(
        call: (callback: (error: RpcError | null, response: T) => void) => unknown,
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            call((error, response) => {
                if (error) reject(error);
                else resolve(response);
            });
        });
    }
}
