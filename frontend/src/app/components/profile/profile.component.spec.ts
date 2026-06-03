import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { StoreService } from '../../services/store.service';
import { ApiService } from '../../services/api.service';
import { ErrorService } from '../../services/error.service';
import { ImageRoles, UploadImageResponse } from '../../protos/file_upload_pb';
import { ProfileDto } from '../../api-client/api-client';
import { provideMockStore } from '@ngrx/store/testing';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { AvatarStubComponent } from '../shared/avatar/avatar.component.stub';
import { ImageUploadService } from '../../services/image-upload.service';
import { errorMessages } from '../../constants/errors';

describe('ProfileComponent', () => {
    let component: ProfileComponent;
    let fixture: ComponentFixture<ProfileComponent>;
    let storeService: MockedObject<StoreService>;
    let apiService: MockedObject<ApiService>;
    let router: MockedObject<Router>;
    let imageUploadService: MockedObject<ImageUploadService>;
    let errorService: MockedObject<ErrorService>;

    beforeEach(async () => {
        storeService = {
            getLoggedInUser: vi.fn().mockName('StoreService.getLoggedInUser'),
            setLoggedInUser: vi.fn().mockName('StoreService.setLoggedInUser'),
        } as MockedObject<StoreService>;
        apiService = {
            saveProfile: vi.fn().mockName('ApiService.saveProfile'),
        } as MockedObject<ApiService>;
        router = {
            navigate: vi.fn().mockName('Router.navigate'),
        } as MockedObject<Router>;
        imageUploadService = {
            resizeAndUploadImage: vi
                .fn()
                .mockName('ImageUploadService.resizeAndUploadImage'),
        } as MockedObject<ImageUploadService>;
        errorService = {
            handleError: vi.fn().mockName('ErrorService.handleError'),
        } as MockedObject<ErrorService>;

        await TestBed.configureTestingModule({
            declarations: [ProfileComponent, AvatarStubComponent],
            imports: [ReactiveFormsModule, FontAwesomeModule],
            providers: [
                FormBuilder,
                provideMockStore({}),
                { provide: StoreService, useValue: storeService },
                { provide: ApiService, useValue: apiService },
                { provide: Router, useValue: router },
                { provide: ImageUploadService, useValue: imageUploadService },
                { provide: ErrorService, useValue: errorService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ProfileComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize the form with the logged-in user data', async () => {
        // Arrange
        const account = {
            firstName: 'John',
            lastName: 'Doe',
            email: 'john.doe@example.com',
        };
        storeService.getLoggedInUser.mockResolvedValue(account);

        // Act
        await component.ngOnInit();

        // Assert
        expect(component.form.value).toEqual({
            firstName: 'John',
            lastName: 'Doe',
            photoUrl: null,
        });
        expect(component.email()).toBe('john.doe@example.com');
    });

    it('should handle avatar selection and set the photoUrl in the form', async () => {
        // Arrange
        const file = new File([''], 'avatar.png', { type: 'image/png' });

        // No need to spy on arrayBuffer since our mock File already implements it
        vi.spyOn(URL, 'createObjectURL').mockReturnValue('mockObjectUrl');

        // Act
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        await component.onAvatarSelected({ target: { files: [file] } } as any);

        // Assert
        expect(component.form.value.photoUrl).toBe('mockObjectUrl');
        expect(component.selectedPhotoUrl()).toBe('mockObjectUrl');
    });

    it('should submit the form and navigate to chats on success', async () => {
        // Arrange
        component.form.setValue({
            firstName: 'John',
            lastName: 'Doe',
            photoUrl: 'mockBase64Url',
        });
        component.selectedPhotoUrl.set('mockBase64Url');

        const uploadResponse = new UploadImageResponse();
        imageUploadService.resizeAndUploadImage.mockResolvedValue(
            uploadResponse,
        );

        const profile = new ProfileDto();
        apiService.saveProfile.mockResolvedValue(profile);

        // Act
        await component.onSubmit();

        // Assert
        expect(imageUploadService.resizeAndUploadImage).toHaveBeenCalledTimes(
            1,
        );

        // Assert
        expect(imageUploadService.resizeAndUploadImage).toHaveBeenCalledWith(
            'mockBase64Url',
            512,
            512,
            ImageRoles.AVATAR,
        );
        expect(errorService.handleError).not.toHaveBeenCalled();
        expect(component.isSending()).toBe(false);
        expect(apiService.saveProfile).toHaveBeenCalledTimes(1);
        expect(apiService.saveProfile).toHaveBeenCalledWith(
            'John',
            'Doe',
            uploadResponse,
        );
        expect(storeService.setLoggedInUser).toHaveBeenCalledWith(profile);
    });

    it('should submit the form without image when no photo is selected', async () => {
        // Arrange
        component.form.setValue({
            firstName: 'John',
            lastName: 'Doe',
            photoUrl: null,
        });
        component.selectedPhotoUrl.set(null);

        const profile = new ProfileDto();
        apiService.saveProfile.mockResolvedValue(profile);

        // Act
        await component.onSubmit();

        // Assert
        expect(imageUploadService.resizeAndUploadImage).not.toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
        expect(component.isSending()).toBe(false);
        expect(apiService.saveProfile).toHaveBeenCalledTimes(1);
        expect(apiService.saveProfile).toHaveBeenCalledWith(
            'John',
            'Doe',
            null,
        );
        expect(storeService.setLoggedInUser).toHaveBeenCalledWith(profile);
    });

    it('should handle errors on image uploading', async () => {
        // Arrange
        component.form.setValue({
            firstName: 'John',
            lastName: 'Doe',
            photoUrl: 'mockBase64Url',
        });
        component.selectedPhotoUrl.set('mockBase64Url');

        const error = new Error('error');
        imageUploadService.resizeAndUploadImage.mockImplementation(() => {
            throw error;
        });

        // Act
        await component.onSubmit();

        // Assert
        expect(component.isSending()).toBe(false);
        expect(errorService.handleError).toHaveBeenCalledTimes(1);
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.uploadImage,
        );
        expect(apiService.saveProfile).not.toHaveBeenCalled();
        expect(storeService.setLoggedInUser).not.toHaveBeenCalled();
    });

    it('should handle errors on form submission', async () => {
        // Arrange
        component.form.setValue({
            firstName: 'John',
            lastName: 'Doe',
            photoUrl: 'mockBase64Url',
        });
        component.selectedPhotoUrl.set('mockBase64Url');

        const uploadResponse = new UploadImageResponse();
        imageUploadService.resizeAndUploadImage.mockResolvedValue(
            uploadResponse,
        );

        const error = new Error('error');
        apiService.saveProfile.mockImplementation(() => {
            throw error;
        });

        // Act
        await component.onSubmit();

        // Assert
        expect(imageUploadService.resizeAndUploadImage).toHaveBeenCalledTimes(
            1,
        );

        // Assert
        expect(imageUploadService.resizeAndUploadImage).toHaveBeenCalledWith(
            'mockBase64Url',
            512,
            512,
            ImageRoles.AVATAR,
        );
        expect(component.isSending()).toBe(false);
        expect(apiService.saveProfile).toHaveBeenCalledTimes(1);
        expect(apiService.saveProfile).toHaveBeenCalledWith(
            'John',
            'Doe',
            uploadResponse,
        );
        expect(errorService.handleError).toHaveBeenCalledTimes(1);
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.saveProfile,
        );
        expect(storeService.setLoggedInUser).not.toHaveBeenCalled();
    });

    it('should revoke object URL on destroy', () => {
        // Arrange
        vi.spyOn(URL, 'revokeObjectURL');
        component.selectedPhotoUrl.set('mockPhotoUrl');

        // Act
        component.ngOnDestroy();

        // Assert
        expect(URL.revokeObjectURL).toHaveBeenCalledWith('mockPhotoUrl');
    });
});
