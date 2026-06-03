import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImageComponent } from './image.component';
import { StoreService } from '../../../services/store.service';
import { ImagePreview } from '../../../models/image-preview';
import { environment } from '../../../../environments/environment';
import { ErrorService } from '../../../services/error.service';
import { errorMessages } from '../../../constants/errors';

describe('ImageComponent', () => {
    let component: ImageComponent;
    let fixture: ComponentFixture<ImageComponent>;
    let storeService: MockedObject<StoreService>;
    let errorService: MockedObject<ErrorService>;

    const imagePreview = {
        id: 'imagePreview1',
        width: 200,
        height: 200,
    };

    const sizeLimit = {
        width: 100,
        height: 100,
    };

    beforeEach(async () => {
        storeService = {
            getImageContent: vi.fn().mockName('StoreService.getImageContent'),
        } as MockedObject<StoreService>;
        errorService = {
            handleError: vi.fn().mockName('ErrorService.handleError'),
        } as MockedObject<ErrorService>;

        await TestBed.configureTestingModule({
            declarations: [ImageComponent],
            providers: [
                { provide: StoreService, useValue: storeService },
                { provide: ErrorService, useValue: errorService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ImageComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should set size based on imagePreview input', async () => {
        // Arrange
        component.imagePreview = imagePreview;
        component.sizeLimit = sizeLimit;

        // Act
        await component.ngOnInit();

        // Assert
        const expectedScale = Math.min(
            sizeLimit.width / imagePreview.width!,
            sizeLimit.height / imagePreview.height!,
        );
        const expectedWidth = expectedScale * imagePreview.width!;
        const expectedHeight = expectedScale * imagePreview.height!;

        expect(component.width()).toBe(expectedWidth);
        expect(component.height()).toBe(expectedHeight);

        expect(storeService.getImageContent).toHaveBeenCalledTimes(1);

        expect(storeService.getImageContent).toHaveBeenCalledWith(imagePreview);
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should handle unknown image size', async () => {
        // Arrange
        const imagePreviewUnknownSize = {
            id: 'imagePreview1',
        };

        component.imagePreview = imagePreviewUnknownSize;

        // Act
        await component.ngOnInit();

        // Assert
        expect(component.isSizeUnknown()).toBe(true);
        expect(component.width()).toBe(
            environment.imageSettings.limits.picturePreview.width,
        );
        expect(component.height()).toBe(
            environment.imageSettings.limits.picturePreview.height,
        );
        expect(storeService.getImageContent).toHaveBeenCalledTimes(1);
        expect(storeService.getImageContent).toHaveBeenCalledWith(
            imagePreviewUnknownSize,
        );
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should load image content on init if imagePreview is provided', async () => {
        // Arrange
        const imageContent = 'data:image/png;base64,someBase64Data';
        storeService.getImageContent.mockResolvedValue({
            content: imageContent,
            width: 200,
            height: 200,
        });

        component.imagePreview = imagePreview;
        component.imagePreview.width = 100;
        component.chatId = 'chatId';

        // Act
        await component.ngOnInit();
        fixture.detectChanges();

        // Assert
        expect(component.url()).toBe(imageContent);
        expect(component.isLoading()).toBe(false);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imagePreview);
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should handle error if getImageContent failed to download the image', async () => {
        // Arrange
        component.imagePreview = imagePreview;
        component.imagePreview.width = 100;

        const error = new Error('Sample error');
        storeService.getImageContent.mockRejectedValue(error);

        // Act
        await component.ngOnInit();
        fixture.detectChanges();

        // Assert
        expect(component.url()).toBeNull();
        expect(component.isLoading()).toBe(false);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imagePreview);
        expect(errorService.handleError).toHaveBeenCalledTimes(1);
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.downloadImage,
        );
    });

    it('should not load image content if imagePreview is not provided', async () => {
        // Arrange

        // Act
        await component.ngOnInit();

        // Assert
        expect(storeService.getImageContent).not.toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should set scaled size if image dimensions exceed the limit', async () => {
        // Arrange
        component.sizeLimit = sizeLimit;

        component.imagePreview = new ImagePreview({ width: 200, height: 300 });

        // Act
        await component.ngOnInit();

        // Assert
        const expectedScale = Math.min(
            sizeLimit.width / 200,
            sizeLimit.height / 300,
        );

        const expectedWidth = Math.round(expectedScale * 200);
        const expectedHeight = Math.round(expectedScale * 300);

        expect(component.width()).toBe(expectedWidth);
        expect(component.height()).toBe(expectedHeight);
        expect(storeService.getImageContent).not.toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
    });
});
