/* eslint-disable @typescript-eslint/no-explicit-any */
import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImageViewerComponent } from './image-viewer.component';
import { StoreService } from '../../services/store.service';
import { ErrorService } from '../../services/error.service';
import { By } from '@angular/platform-browser';
import { errorMessages } from '../../constants/errors';
import { IImageDto } from '../../api-client/api-client';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { Location } from '@angular/common';

describe('ImageViewerComponent', () => {
    let component: ImageViewerComponent;
    let fixture: ComponentFixture<ImageViewerComponent>;
    let storeService: MockedObject<StoreService>;
    let errorService: MockedObject<ErrorService>;
    let location: MockedObject<Location>;
    let paramsSubject: BehaviorSubject<any>;

    const imageKey: IImageDto = {
        id: 'image-id',
        fileStorageTypeId: 1,
    };

    beforeEach(async () => {
        paramsSubject = new BehaviorSubject({});

        storeService = {
            getImageContent: vi.fn().mockName('StoreService.getImageContent'),
        } as MockedObject<StoreService>;
        errorService = {
            handleError: vi.fn().mockName('ErrorService.handleError'),
        } as MockedObject<ErrorService>;
        location = {
            back: vi.fn().mockName('Location.back'),
        } as MockedObject<Location>;

        await TestBed.configureTestingModule({
            declarations: [ImageViewerComponent],
            providers: [
                { provide: StoreService, useValue: storeService },
                { provide: ErrorService, useValue: errorService },
                { provide: Location, useValue: location },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        firstChild: null,
                        params: paramsSubject.asObservable(),
                    },
                },
            ],
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(ImageViewerComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should call getImageContent and display the image when image key is provided', async () => {
        // Arrange
        const imageKeyParam = `${imageKey.id}_${imageKey.fileStorageTypeId}`;
        storeService.getImageContent.mockResolvedValue({
            content: 'image-url',
        });

        // Act
        component.ngOnInit();
        paramsSubject.next({ imageKey: imageKeyParam });

        // Wait for async operations
        await new Promise((resolve) => setTimeout(resolve, 0));
        fixture.detectChanges();

        // Assert
        expect(component.backgroundImage()).toBe('image-url');
        expect(component.isVisible()).toBe(true);

        const imageElement = fixture.debugElement.query(By.css('img'));
        expect(imageElement.nativeElement.src).toContain('image-url');

        expect(storeService.getImageContent).toHaveBeenCalledWith(imageKey);
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should handle error and close when getImageContent fails', async () => {
        // Arrange
        const imageKeyParam = `${imageKey.id}_${imageKey.fileStorageTypeId}`;
        const error = new Error('Sample error');
        storeService.getImageContent.mockRejectedValue(error);

        // Act
        component.ngOnInit();
        paramsSubject.next({ imageKey: imageKeyParam });

        // Wait for async operations
        await new Promise((resolve) => setTimeout(resolve, 0));

        // Assert
        expect(component.isVisible()).toBe(false);
        expect(storeService.getImageContent).toHaveBeenCalledWith(imageKey);
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.downloadImage,
        );
        expect(location.back).toHaveBeenCalled();
    });

    it('should hide the image is undefined', async () => {
        // Arrange

        // Act
        component.ngOnInit();
        paramsSubject.next({});

        // Wait for async operations
        await new Promise((resolve) => setTimeout(resolve, 0));

        // Assert
        expect(component.isVisible()).toBe(false);
        expect(storeService.getImageContent).not.toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
    });

    it('should hide the image when close is called', () => {
        // Arrange

        // Act
        component.close();

        // Assert
        expect(component.isVisible()).toBe(false);
        expect(location.back).toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
    });
});
