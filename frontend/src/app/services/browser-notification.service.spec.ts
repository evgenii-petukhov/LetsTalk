import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ToastrService } from 'ngx-toastr';
import { BrowserNotificationService } from './browser-notification.service';

describe('BrowserNotificationService', () => {
    let service: BrowserNotificationService;
    let toastrService: MockedObject<ToastrService>;

    const mockServiceWorkerRegistration = {
        showNotification: vi.fn(),
    };

    beforeEach(async () => {
        toastrService = {
            info: vi.fn().mockName('ToastrService.info'),
        } as MockedObject<ToastrService>;

        Object.defineProperty(navigator, 'serviceWorker', {
            writable: true,
            value: {
                register: vi.fn().mockReturnValue(Promise.resolve()),
                ready: Promise.resolve(mockServiceWorkerRegistration),
            },
        });

        await TestBed.configureTestingModule({
            providers: [
                BrowserNotificationService,
                { provide: ToastrService, useValue: toastrService },
            ],
        }).compileComponents();

        service = TestBed.inject(BrowserNotificationService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should register service worker on init and set isServiceWorkerRegistered to true', async () => {
        // Arrange
        vi.spyOn(console, 'log');

        // Act
        await service.init();

        // Assert
        expect(navigator.serviceWorker.register).toHaveBeenCalledWith(
            'notification_sw.js',
        );
        expect(console.log).toHaveBeenCalledWith('serviceWorker success');
        expect(service['isServiceWorkerRegistered']).toBe(true);
    });

    it('should log error if service worker registration fails', async () => {
        // Arrange
        Object.defineProperty(navigator, 'serviceWorker', {
            writable: true,
            value: {
                register: vi.fn().mockReturnValue(Promise.reject()),
            },
        });

        vi.spyOn(console, 'log');

        // Act
        await service.init();

        // Assert
        expect(navigator.serviceWorker.register).toHaveBeenCalledWith(
            'notification_sw.js',
        );
        expect(console.log).toHaveBeenCalledWith('serviceWorker error');
        expect(service['isServiceWorkerRegistered']).toBe(false);
    });

    it('should show notification if permission is granted and service worker is registered', async () => {
        // Arrange
        vi.spyOn(Notification, 'requestPermission').mockReturnValue(
            Promise.resolve('granted'),
        );
        vi.spyOn(console, 'log');

        // Act
        await service.init();
        await service.showNotification('Test Title', 'Test Message', false);

        // Assert
        expect(navigator.serviceWorker.register).toHaveBeenCalledWith(
            'notification_sw.js',
        );
        expect(console.log).toHaveBeenCalledWith('serviceWorker success');
        expect(service['isServiceWorkerRegistered']).toBe(true);
        expect(
            mockServiceWorkerRegistration.showNotification,
        ).toHaveBeenCalledWith('Test Title', {
            body: 'Test Message',
        });
    });

    it('should fallback to window notification if service worker is not registered', async () => {
        // Arrange
        vi.spyOn(Notification, 'requestPermission').mockReturnValue(
            Promise.resolve('granted'),
        );
        const notificationSpy = vi.spyOn(window, 'Notification');

        // Act
        await service.showNotification('Test Title', 'Test Message', false);

        // Assert
        expect(notificationSpy).toHaveBeenCalledWith('Test Title', {
            body: 'Test Message',
        });
    });

    it('should use Toastr if permission is denied and window is active', async () => {
        // Arrange
        vi.spyOn(Notification, 'requestPermission').mockReturnValue(
            Promise.resolve('denied'),
        );

        // Act
        await service.showNotification('Test Title', 'Test Message', true);

        // Assert
        expect(toastrService.info).toHaveBeenCalledWith(
            'Test Message',
            'Test Title',
        );
    });

    it('should not show anything if permission is denied and window is not active', async () => {
        // Arrange
        vi.spyOn(Notification, 'requestPermission').mockReturnValue(
            Promise.resolve('denied'),
        );

        // Act
        await service.showNotification('Test Title', 'Test Message', false);

        // Assert
        expect(toastrService.info).not.toHaveBeenCalled();
    });
});
