import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AvatarComponent } from './avatar.component';
import { StoreService } from '../../../services/store.service';
import { ErrorService } from '../../../services/error.service';
import { errorMessages } from '../../../constants/errors';
import { IImageDto } from '../../../api-client/api-client';

describe('AvatarComponent', () => {
    let component: AvatarComponent;
    let fixture: ComponentFixture<AvatarComponent>;
    let storeService: MockedObject<StoreService>;
    let errorService: MockedObject<ErrorService>;

    const imageKey: IImageDto = {
        id: 'image-id',
        fileStorageTypeId: 1,
    };
    const url = 'http://example.com/image.jpg';
    const defaultUrl = 'images/empty-avatar.svg';
    const mockImage = {
        content: 'data:image/jpeg;base64,...',
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
            declarations: [AvatarComponent],
            providers: [
                { provide: StoreService, useValue: storeService },
                { provide: ErrorService, useValue: errorService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AvatarComponent);
        component = fixture.componentInstance;
    });

    [
        { value: undefined, text: 'undefined' },
        { value: null, text: 'null' },
        { value: [], text: 'empty array' },
        { value: [''], text: 'array of empty string' },
    ].forEach(({ value, text }) => {
        it(`should display default background image if urlOptions is ${text}`, async () => {
            // Arrange

            // Act
            component.urlOptions = value;
            component.ngOnChanges();
            fixture.detectChanges();

            // Assert
            expect(component.backgroundImage()).toBe(`url('${defaultUrl}')`);
            expect(storeService.getImageContent).not.toHaveBeenCalledTimes(1);
            expect(errorService.handleError).not.toHaveBeenCalledTimes(1);
        });
    });

    it('should display background image from URL', async () => {
        // Arrange

        // Act
        component.urlOptions = [url];
        component.ngOnChanges();
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toBe(
            `url('${url}'), url('${defaultUrl}')`,
        );
        expect(storeService.getImageContent).not.toHaveBeenCalledTimes(1);
        expect(errorService.handleError).not.toHaveBeenCalledTimes(1);
    });

    it('should display background image from image ID', async () => {
        // Arrange
        storeService.getImageContent.mockResolvedValue(mockImage);

        // Act
        component.urlOptions = [imageKey];
        component.ngOnChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toContain(
            `url('${mockImage.content}')`,
        );
        expect(storeService.getImageContent).toHaveBeenCalledTimes(1);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imageKey);
        expect(errorService.handleError).not.toHaveBeenCalledTimes(1);
    });

    it('should display background image from URL and image ID', async () => {
        // Arrange
        storeService.getImageContent.mockResolvedValue(mockImage);

        // Act
        component.urlOptions = [url, imageKey];
        component.ngOnChanges();
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toBe(
            `url('${url}'), url('${defaultUrl}')`,
        );
        expect(storeService.getImageContent).not.toHaveBeenCalledTimes(1);
        expect(errorService.handleError).not.toHaveBeenCalledTimes(1);
    });

    it('should display background image from image ID and URL', async () => {
        // Arrange
        storeService.getImageContent.mockResolvedValue(mockImage);

        // Act
        component.urlOptions = [imageKey, url];
        component.ngOnChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toBe(`url('${mockImage.content}')`);
        expect(storeService.getImageContent).toHaveBeenCalledTimes(1);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imageKey);
        expect(errorService.handleError).not.toHaveBeenCalledTimes(1);
    });

    it('should display default background image if getImageContent fails', async () => {
        // Arrange
        const error = new Error('error');
        storeService.getImageContent.mockImplementation(() => {
            throw error;
        });

        // Act
        component.urlOptions = [imageKey];
        component.ngOnChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toBe(`url('${defaultUrl}')`);
        expect(storeService.getImageContent).toHaveBeenCalledTimes(1);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imageKey);
        expect(errorService.handleError).toHaveBeenCalledTimes(1);
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.downloadImage,
        );
    });
});
