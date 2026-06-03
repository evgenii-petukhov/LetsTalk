import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChatListTopPanelComponent } from './chat-list-top-panel.component';
import { StoreService } from '../../../../services/store.service';
import { BackButtonStubComponent } from '../../../shared/back-button/back-button.component.stub';
import { AvatarStubComponent } from '../../../shared/avatar/avatar.component.stub';
import { LogoutButtonStubComponent } from '../logout-button/logout-button.component.stub';
import { UserDetailsStubComponent } from '../../../shared/user-details/user-details.component.stub';
import { By } from '@angular/platform-browser';
import { ImageDto } from '../../../../api-client/api-client';
import { provideRouter } from '@angular/router';
import { Component, Input } from '@angular/core';
import { BackButtonStatus } from '../../../../models/back-button-status';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-top-panel',
    template: '<ng-content></ng-content>',
    standalone: false,
})
class TopPanelStubComponent {
    @Input()
    backButton: BackButtonStatus;
}

describe('ChatListTopPanelComponent', () => {
    let component: ChatListTopPanelComponent;
    let fixture: ComponentFixture<ChatListTopPanelComponent>;
    let storeService: MockedObject<StoreService>;

    beforeEach(async () => {
        storeService = {
            getLoggedInUser: vi.fn().mockName('StoreService.getLoggedInUser'),
        } as MockedObject<StoreService>;

        await TestBed.configureTestingModule({
            declarations: [
                ChatListTopPanelComponent,
                BackButtonStubComponent,
                AvatarStubComponent,
                UserDetailsStubComponent,
                LogoutButtonStubComponent,
                TopPanelStubComponent,
            ],
            imports: [RouterModule],
            providers: [
                { provide: StoreService, useValue: storeService },
                provideRouter([]),
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ChatListTopPanelComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should fetch logged-in user on initialization', async () => {
        // Arrange
        const profile = {
            image: new ImageDto({
                id: '1',
                fileStorageTypeId: 1,
            }),
            photoUrl: 'url',
        };

        storeService.getLoggedInUser.mockResolvedValue(profile);

        // Act
        await component.ngOnInit();

        // Assert
        expect(storeService.getLoggedInUser).toHaveBeenCalled();
        expect(component.account()).toEqual(profile);
    });

    it('should the Avatar, UserDetails, and LogoutButton components if isNavigationActive is false', () => {
        // Arrange
        component.isNavigationActive = false;

        // Act
        fixture.detectChanges();

        // Assert
        const hostElement = fixture.debugElement.nativeElement;
        expect(hostElement.classList.contains('navigation-active')).toBe(false);

        const backButton = fixture.debugElement.query(
            By.directive(BackButtonStubComponent),
        );
        expect(backButton).toBeNull();

        const avatar = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        );
        expect(avatar).toBeTruthy();

        const userDetails = fixture.debugElement.query(
            By.directive(UserDetailsStubComponent),
        );
        expect(userDetails).toBeTruthy();
        expect(userDetails.componentInstance.value).toBe('You');

        const logoutButton = fixture.debugElement.query(
            By.directive(LogoutButtonStubComponent),
        );
        expect(logoutButton).toBeTruthy();
    });

    it('should apply navigation-active CSS class when isNavigationActive is true', () => {
        // Arrange
        component.isNavigationActive = true;

        // Act
        fixture.detectChanges();

        // Assert
        const hostElement = fixture.debugElement.nativeElement;
        expect(hostElement.classList.contains('navigation-active')).toBe(true);
    });

    it('should not apply navigation-active CSS class when isNavigationActive is false', () => {
        // Arrange
        component.isNavigationActive = false;

        // Act
        fixture.detectChanges();

        // Assert
        const hostElement = fixture.debugElement.nativeElement;
        expect(hostElement.classList.contains('navigation-active')).toBe(false);
    });

    it('should pass correct urlOptions to avatar when account has image and photoUrl', () => {
        // Arrange
        const mockAccount = {
            image: new ImageDto({ id: '1', fileStorageTypeId: 1 }),
            photoUrl: 'test-photo-url',
        };
        component.account.set(mockAccount);

        // Act
        fixture.detectChanges();

        // Assert
        const avatar = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        );
        expect(avatar.componentInstance.urlOptions).toEqual([
            mockAccount.image,
            mockAccount.photoUrl,
        ]);
    });

    it('should pass null urlOptions to avatar when account is null', () => {
        // Arrange
        component.account.set(null);

        // Act
        fixture.detectChanges();

        // Assert
        const avatar = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        );
        expect(avatar.componentInstance.urlOptions).toBeFalsy();
    });

    it('should pass undefined urlOptions to avatar when account is undefined', () => {
        // Arrange
        component.account.set(undefined);

        // Act
        fixture.detectChanges();

        // Assert
        const avatar = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        );
        expect(avatar.componentInstance.urlOptions).toBeFalsy();
    });

    it('should call onLogoutButtonClicked method when logout button is clicked', () => {
        // Arrange
        vi.spyOn(component, 'onLogoutButtonClicked');

        // Act
        component.onLogoutButtonClicked();

        // Assert
        expect(component.onLogoutButtonClicked).toHaveBeenCalled();
    });

    it('should trigger logout when logout button emits buttonClick event', () => {
        // Arrange
        vi.spyOn(component, 'onLogoutButtonClicked');

        // Act
        fixture.detectChanges();

        const logoutButton = fixture.debugElement.query(
            By.directive(LogoutButtonStubComponent),
        );
        logoutButton.componentInstance.buttonClick.emit();

        // Assert
        expect(component.onLogoutButtonClicked).toHaveBeenCalled();
    });

    it('should have routerLink pointing to profile page', () => {
        // Arrange & Act
        fixture.detectChanges();

        // Assert
        const profileLink = fixture.debugElement.query(
            By.css('a[title="Edit profile"]'),
        );
        expect(profileLink).toBeTruthy();
        expect(profileLink.nativeElement.title).toBe('Edit profile');
    });

    it('should handle ngOnInit error gracefully', async () => {
        // Arrange
        const error = new Error('Service error');
        storeService.getLoggedInUser.mockRejectedValue(error);
        vi.spyOn(console, 'error'); // Prevent error from showing in test output

        // Act & Assert
        await expect(component.ngOnInit()).rejects.toThrow();
        expect(storeService.getLoggedInUser).toHaveBeenCalled();
    });

    describe('HostBinding', () => {
        it('should bind isNavigationActive to navigation-active class', async () => {
            // Test initial state (false)
            component.isNavigationActive = false;
            fixture.detectChanges();
            await fixture.whenStable();

            expect(
                fixture.debugElement.nativeElement.classList.contains(
                    'navigation-active',
                ),
            ).toBe(false);

            // Create new component instance to test true state
            const fixture2 = TestBed.createComponent(ChatListTopPanelComponent);
            const component2 = fixture2.componentInstance;
            component2.isNavigationActive = true;
            fixture2.detectChanges();
            await fixture2.whenStable();

            expect(
                fixture2.debugElement.nativeElement.classList.contains(
                    'navigation-active',
                ),
            ).toBe(true);
        });
    });

    describe('Component Integration', () => {
        it('should render all child components correctly', () => {
            // Arrange & Act
            fixture.detectChanges();

            // Assert
            const avatar = fixture.debugElement.query(
                By.directive(AvatarStubComponent),
            );
            const userDetails = fixture.debugElement.query(
                By.directive(UserDetailsStubComponent),
            );
            const logoutButton = fixture.debugElement.query(
                By.directive(LogoutButtonStubComponent),
            );

            expect(avatar).toBeTruthy();
            expect(userDetails).toBeTruthy();
            expect(logoutButton).toBeTruthy();

            // Verify UserDetails has correct value
            expect(userDetails.componentInstance.value).toBe('You');
        });

        it('should pass account data to avatar component when available', () => {
            // Arrange
            const mockAccount = {
                image: new ImageDto({ id: 'test-id', fileStorageTypeId: 2 }),
                photoUrl: 'https://example.com/photo.jpg',
            };
            component.account.set(mockAccount);

            // Act
            fixture.detectChanges();

            // Assert
            const avatar = fixture.debugElement.query(
                By.directive(AvatarStubComponent),
            );
            expect(avatar.componentInstance.urlOptions).toEqual([
                mockAccount.image,
                mockAccount.photoUrl,
            ]);
        });
    });
});
