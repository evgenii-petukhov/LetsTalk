import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ImageUploadService } from './image-upload.service';
import { ImageService } from './image.service';
import { FileStorageService } from './file-storage.service';
import { ImageRoles, UploadImageResponse } from '../protos/file_upload_pb';

describe('ImageUploadService', () => {
    let service: ImageUploadService;
    let imageService: MockedObject<ImageService>;
    let fileStorageService: MockedObject<FileStorageService>;

    beforeEach(() => {
        imageService = {
            resizeBase64Image: vi
                .fn()
                .mockName('ImageService.resizeBase64Image'),
        };
        fileStorageService = {
            uploadImageAsBlob: vi
                .fn()
                .mockName('FileStorageService.uploadImageAsBlob'),
        } as MockedObject<FileStorageService>;

        TestBed.configureTestingModule({
            providers: [
                ImageUploadService,
                { provide: ImageService, useValue: imageService },
                { provide: FileStorageService, useValue: fileStorageService },
            ],
        });

        service = TestBed.inject(ImageUploadService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('resizeAndUploadImage', () => {
        it('should resize image and upload', async () => {
            // Arrange
            const photoUrl = 'data:image/png;base64,...';
            const width = 100;
            const height = 100;
            const imageRole = ImageRoles.AVATAR;

            const blob = new Blob();
            const uploadResponse = new UploadImageResponse();
            uploadResponse.setId('123');
            uploadResponse.setWidth(width);
            uploadResponse.setHeight(height);
            uploadResponse.setImageFormat(1);
            uploadResponse.setSignature('signature');

            imageService.resizeBase64Image.mockResolvedValue(blob);
            fileStorageService.uploadImageAsBlob.mockResolvedValue(
                uploadResponse,
            );

            // Act
            const result = await service.resizeAndUploadImage(
                photoUrl,
                width,
                height,
                imageRole,
            );

            // Assert
            expect(imageService.resizeBase64Image).toHaveBeenCalledTimes(1);

            // Assert
            expect(imageService.resizeBase64Image).toHaveBeenCalledWith(
                photoUrl,
                width,
                height,
            );
            expect(fileStorageService.uploadImageAsBlob).toHaveBeenCalledTimes(
                1,
            );
            expect(fileStorageService.uploadImageAsBlob).toHaveBeenCalledWith(
                blob,
                imageRole,
            );
            expect(result).toEqual(uploadResponse);
        });

        it('should handle empty photoUrl', async () => {
            // Arrange
            const photoUrl = '';
            const width = 100;
            const height = 100;
            const imageRole = ImageRoles.AVATAR;

            const uploadResponse = new UploadImageResponse();
            uploadResponse.setId('123');
            uploadResponse.setWidth(width);
            uploadResponse.setHeight(height);
            uploadResponse.setImageFormat(1);
            uploadResponse.setSignature('signature');

            fileStorageService.uploadImageAsBlob.mockResolvedValue(
                uploadResponse,
            );

            // Act
            const result = await service.resizeAndUploadImage(
                photoUrl,
                width,
                height,
                imageRole,
            );

            // Assert
            expect(imageService.resizeBase64Image).not.toHaveBeenCalled();
            expect(fileStorageService.uploadImageAsBlob).toHaveBeenCalledTimes(
                1,
            );
            expect(fileStorageService.uploadImageAsBlob).toHaveBeenCalledWith(
                null,
                imageRole,
            );
            expect(result).toEqual(uploadResponse);
        });
    });
});
