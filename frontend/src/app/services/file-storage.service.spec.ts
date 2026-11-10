import { beforeEach, describe, expect, it, vi } from "vitest";
/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { StatusCode } from 'grpc-web';
import { FileStorageService } from './file-storage.service';
import { SessionService } from './session.service';
import { environment } from '../../environments/environment';
import { DownloadImageRequest, DownloadImageResponse, ImageRoles, UploadImageRequest, UploadImageResponse, } from '../protos/file_upload_pb';
import { IImageDto } from '../api-client/api-client';

const clientConstructor = vi.hoisted(() => vi.fn());

vi.mock('../protos/file_upload_grpc_web_pb', () => ({
    FileUploadGrpcEndpointClient: class {
        constructor(...args: unknown[]) {
            clientConstructor(...args);
        }
    },
}));

describe('FileStorageService', () => {
    let service: FileStorageService;
    let sessionService: { renew: ReturnType<typeof vi.fn> };

    const mockImageKey: IImageDto = {
        id: 'test-image-id',
        fileStorageTypeId: 1,
    };

    beforeEach(() => {
        clientConstructor.mockClear();

        sessionService = { renew: vi.fn().mockName('SessionService.renew') };

        TestBed.configureTestingModule({
            providers: [
                FileStorageService,
                { provide: SessionService, useValue: sessionService },
            ],
        });

        service = TestBed.inject(FileStorageService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should create the gRPC client with credentials enabled so the browser sends the session cookie', () => {
        expect(clientConstructor).toHaveBeenCalledWith(
            environment.services.fileStorage.url,
            null,
            { withCredentials: true },
        );
    });

    describe('uploadImageAsBlob', () => {
        it('should convert blob to Uint8Array and call upload', async () => {
            const mockBlob = new Blob(['test content'], { type: 'image/png' });
            const mockUploadResponse = new UploadImageResponse();
            const imageRole = ImageRoles.AVATAR;

            vi.spyOn(service, 'upload').mockResolvedValue(mockUploadResponse);
            // No need to spy on arrayBuffer since our mock Blob already implements it

            const result = await service.uploadImageAsBlob(mockBlob, imageRole);

            expect(service.upload).toHaveBeenCalledWith(expect.any(Uint8Array), imageRole);
            expect(result).toBe(mockUploadResponse);
        });
    });

    describe('upload', () => {
        it('should upload image successfully', async () => {
            const mockContent = new Uint8Array([1, 2, 3, 4]);
            const imageRole = ImageRoles.AVATAR;
            const mockResponse = new UploadImageResponse();

            // Mock the private fileUploadService
            const mockFileUploadService = {
                uploadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.uploadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.uploadImageAsync.mockImplementation((request: any, metadata: any, callback: any) => {
                expect(request).toBeInstanceOf(UploadImageRequest);
                expect(request.getContent()).toEqual(mockContent);
                expect(request.getImageRole()).toBe(imageRole);
                expect(metadata).toEqual({});
                callback(null, mockResponse);
            });

            const result = await service.upload(mockContent, imageRole);

            expect(result).toBe(mockResponse);
            expect(mockFileUploadService.uploadImageAsync).toHaveBeenCalled();
        });

        it('should reject on upload error', async () => {
            const mockContent = new Uint8Array([1, 2, 3, 4]);
            const imageRole = ImageRoles.AVATAR;
            const mockError = {
                code: 1,
                message: 'Upload failed',
                metadata: {},
            };

            const mockFileUploadService = {
                uploadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.uploadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.uploadImageAsync.mockImplementation((_request: any, _metadata: any, callback: any) => {
                callback(mockError, null);
            });

            await expect(service.upload(mockContent, imageRole)).rejects.toEqual(mockError);
        });

        it('should create request with correct parameters', async () => {
            const mockContent = new Uint8Array([5, 6, 7, 8]);
            const imageRole = ImageRoles.MESSAGE;
            const mockResponse = new UploadImageResponse();

            const mockFileUploadService = {
                uploadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.uploadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.uploadImageAsync.mockImplementation((request: any, _metadata: any, callback: any) => {
                expect(request.getContent()).toEqual(mockContent);
                expect(request.getImageRole()).toBe(imageRole);
                callback(null, mockResponse);
            });

            await service.upload(mockContent, imageRole);

            expect(mockFileUploadService.uploadImageAsync).toHaveBeenCalled();
        });
    });

    describe('download', () => {
        it('should download image successfully', async () => {
            const mockResponse = new DownloadImageResponse();

            const mockFileUploadService = {
                downloadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.downloadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.downloadImageAsync.mockImplementation((request: any, metadata: any, callback: any) => {
                expect(request).toBeInstanceOf(DownloadImageRequest);
                expect(request.getImageId()).toBe(mockImageKey.id);
                expect(request.getFileStorageTypeId()).toBe(mockImageKey.fileStorageTypeId);
                expect(metadata).toEqual({});
                callback(null, mockResponse);
            });

            const result = await service.download(mockImageKey);

            expect(result).toBe(mockResponse);
            expect(mockFileUploadService.downloadImageAsync).toHaveBeenCalled();
        });

        it('should reject on download error', async () => {
            const mockError = {
                code: 1,
                message: 'Download failed',
                metadata: {},
            };

            const mockFileUploadService = {
                downloadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.downloadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.downloadImageAsync.mockImplementation((_request: any, _metadata: any, callback: any) => {
                callback(mockError, null);
            });

            await expect(service.download(mockImageKey)).rejects.toEqual(mockError);
        });

        it('should create request with correct image key parameters', async () => {
            const customImageKey: IImageDto = {
                id: 'custom-image-id',
                fileStorageTypeId: 2,
            };
            const mockResponse = new DownloadImageResponse();

            const mockFileUploadService = {
                downloadImageAsync: vi.fn().mockName("FileUploadGrpcEndpointClient.downloadImageAsync")
            };
            (service as any).fileUploadService = mockFileUploadService;

            mockFileUploadService.downloadImageAsync.mockImplementation((request: any, metadata: any, callback: any) => {
                expect(request.getImageId()).toBe(customImageKey.id);
                expect(request.getFileStorageTypeId()).toBe(customImageKey.fileStorageTypeId);
                callback(null, mockResponse);
            });

            await service.download(customImageKey);

            expect(mockFileUploadService.downloadImageAsync).toHaveBeenCalled();
        });
    });

    describe('when the access token has expired', () => {
        const unauthenticated = { code: StatusCode.UNAUTHENTICATED, message: 'expired' };
        const response = new UploadImageResponse();
        let fileUploadService: { uploadImageAsync: ReturnType<typeof vi.fn>; downloadImageAsync: ReturnType<typeof vi.fn> };

        beforeEach(() => {
            fileUploadService = {
                uploadImageAsync: vi.fn().mockName('FileUploadGrpcEndpointClient.uploadImageAsync'),
                downloadImageAsync: vi.fn().mockName('FileUploadGrpcEndpointClient.downloadImageAsync'),
            };
            (service as any).fileUploadService = fileUploadService;
        });

        it('upload should renew the session and retry once on UNAUTHENTICATED', async () => {
            sessionService.renew.mockResolvedValue(true);
            fileUploadService.uploadImageAsync
                .mockImplementationOnce((_r: any, _m: any, callback: any) => callback(unauthenticated, null))
                .mockImplementationOnce((_r: any, _m: any, callback: any) => callback(null, response));

            const result = await service.upload(new Uint8Array([1]), ImageRoles.AVATAR);

            expect(result).toBe(response);
            expect(sessionService.renew).toHaveBeenCalledTimes(1);
            expect(fileUploadService.uploadImageAsync).toHaveBeenCalledTimes(2);
        });

        it('download should renew the session and retry once on UNAUTHENTICATED', async () => {
            const downloadResponse = new DownloadImageResponse();
            sessionService.renew.mockResolvedValue(true);
            fileUploadService.downloadImageAsync
                .mockImplementationOnce((_r: any, _m: any, callback: any) => callback(unauthenticated, null))
                .mockImplementationOnce((_r: any, _m: any, callback: any) => callback(null, downloadResponse));

            const result = await service.download(mockImageKey);

            expect(result).toBe(downloadResponse);
            expect(sessionService.renew).toHaveBeenCalledTimes(1);
            expect(fileUploadService.downloadImageAsync).toHaveBeenCalledTimes(2);
        });

        it('should reject with the second error when the retry is rejected as well', async () => {
            sessionService.renew.mockResolvedValue(true);
            fileUploadService.uploadImageAsync
                .mockImplementation((_r: any, _m: any, callback: any) => callback(unauthenticated, null));

            await expect(service.upload(new Uint8Array([1]), ImageRoles.AVATAR)).rejects.toEqual(unauthenticated);

            expect(sessionService.renew).toHaveBeenCalledTimes(1);
            expect(fileUploadService.uploadImageAsync).toHaveBeenCalledTimes(2);
        });

        it('should reject without a retry when the refresh token is rejected', async () => {
            sessionService.renew.mockResolvedValue(false);
            fileUploadService.uploadImageAsync
                .mockImplementation((_r: any, _m: any, callback: any) => callback(unauthenticated, null));

            await expect(service.upload(new Uint8Array([1]), ImageRoles.AVATAR)).rejects.toEqual(unauthenticated);

            expect(fileUploadService.uploadImageAsync).toHaveBeenCalledTimes(1);
        });

        it('should reject with the renewal error when the server cannot answer', async () => {
            const outage = { status: 503 };
            sessionService.renew.mockRejectedValue(outage);
            fileUploadService.uploadImageAsync
                .mockImplementation((_r: any, _m: any, callback: any) => callback(unauthenticated, null));

            await expect(service.upload(new Uint8Array([1]), ImageRoles.AVATAR)).rejects.toEqual(outage);

            expect(fileUploadService.uploadImageAsync).toHaveBeenCalledTimes(1);
        });

        it('should not renew for other errors', async () => {
            const other = { code: StatusCode.INTERNAL, message: 'boom' };
            fileUploadService.downloadImageAsync
                .mockImplementation((_r: any, _m: any, callback: any) => callback(other, null));

            await expect(service.download(mockImageKey)).rejects.toEqual(other);

            expect(sessionService.renew).not.toHaveBeenCalled();
            expect(fileUploadService.downloadImageAsync).toHaveBeenCalledTimes(1);
        });
    });
});
